<#
.SYNOPSIS
  Deploys the Compicasa PoC to Azure as production (D-027, D-028): infrastructure, database access, then the app.
.DESCRIPTION
  Needs the Azure CLI signed in to the subscription, and the .NET 10 SDK. Safe to run again: every step is idempotent.
  -Infra   only the Bicep templates and database access
  -App     only build and deploy the app (to production, or with -Slot test to the test slot, D-060)
  (neither: both)
  -Infra also makes the test slot and its own free database, and gives the slot's identity a user there (D-060).
  The invite code is kept in the web app's settings, never in this repository. A new one is made only on the first run,
  or with -NewInviteCode.
#>
param(
  [string]$Subscription = '81209232-7da9-4f81-b984-7ab0bd3dcfb3',
  [string]$Location = 'uksouth',
  [switch]$Infra,
  [switch]$App,
  [switch]$NewInviteCode,
  [string]$InviteCode,  # sets the code, for example to keep the old one when the web app is rebuilt
  [string]$ExternalIdAuthority,  # with -ExternalIdClientId: the External ID settings, when the web app is rebuilt
  [string]$ExternalIdClientId,
  [string]$WebApp = 'compicasa',  # the web app, and so the address <name>.azurewebsites.net (D-057)
  [string]$Slot,  # with -App: deploy to this slot (for example test) instead of production (D-060)
  [switch]$OwnPlan  # create an App Service plan in rg-compicasa-prod instead of using BuildSyncProd: for another subscription (B-073)
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$rg = 'rg-compicasa-prod'
if (-not $Infra -and -not $App) { $Infra = $true; $App = $true }

function Invoke-Az { $out = & az @args; if ($LASTEXITCODE -ne 0) { throw "az $($args[0..1] -join ' ') failed (arguments not shown: they can include the invite code)" }; return $out }

if ($Infra) {
  Write-Host '== Infrastructure'
  $me = Invoke-Az account get-access-token --resource-type ms-graph --subscription $Subscription --query accessToken -o tsv
  $user = Invoke-RestMethod -Headers @{ Authorization = "Bearer $me" } -Uri 'https://graph.microsoft.com/v1.0/me?$select=id,userPrincipalName'

  # Settings are kept from the web app itself or, the first time under a new name (D-057), from the one it replaces.
  $names = @(az webapp list -g $rg --subscription $Subscription --query '[].name' -o tsv 2>$null)
  $existingWeb = if ($names -contains $WebApp) { $WebApp } else { $names | Select-Object -First 1 }
  # Keep the existing invite code unless asked for a new one.
  $invite = $InviteCode
  if (-not $invite -and -not $NewInviteCode -and $existingWeb) {
    $invite = ((az webapp config appsettings list -g $rg -n $existingWeb --subscription $Subscription -o json 2>$null) | ConvertFrom-Json | Where-Object name -eq 'Auth__InviteCode').value
  }
  # Keep the External ID settings that external-id.ps1 wrote (D-033).
  # Keep any agencies the crawler was told to leave alone (D-035).
  # Keep who may see the release funnel (B-074).
  # Keep the sign-in record removal settings that erase-signins.ps1 wrote (B-039).
  $authority = $ExternalIdAuthority; $clientId = $ExternalIdClientId; $stopped = ''; $admins = ''; $tenantId = ''; $eraseId = ''; $eraseSecret = ''
  if ($existingWeb) {
    $settings = az webapp config appsettings list -g $rg -n $existingWeb --subscription $Subscription -o json 2>$null | ConvertFrom-Json
    if (-not $authority) { $authority = ($settings | Where-Object name -eq 'Auth__External__Authority').value }
    if (-not $clientId) { $clientId = ($settings | Where-Object name -eq 'Auth__External__ClientId').value }
    $stopped = ($settings | Where-Object name -eq 'Homes__Stopped').value
    $admins = ($settings | Where-Object name -eq 'Admin__AccountIds').value
    $tenantId = ($settings | Where-Object name -eq 'Auth__External__TenantId').value
    $eraseId = ($settings | Where-Object name -eq 'Auth__Erase__ClientId').value
    $eraseSecret = ($settings | Where-Object name -eq 'Auth__Erase__ClientSecret').value
  }
  if (-not $invite) {
    $chars = 'abcdefghjkmnpqrstuvwxyz23456789'.ToCharArray()
    $invite = -join (1..12 | ForEach-Object { $chars[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($chars.Length)] })
    Write-Host 'New invite code made.'
  }

  $out = Invoke-Az deployment sub create --subscription $Subscription -l $Location -n compicasa-prod-$Location `
    --template-file (Join-Path $PSScriptRoot 'main.bicep') `
    -p sqlAdminObjectId=$($user.id) sqlAdminLogin=$($user.userPrincipalName) inviteCode=$invite "webAppName=$WebApp" "externalIdAuthority=$authority" "externalIdClientId=$clientId" "homesStopped=$stopped" "adminAccountIds=$admins" "externalIdTenantId=$tenantId" "eraseClientId=$eraseId" "eraseClientSecret=$eraseSecret" $(if ($OwnPlan) { 'planId=' }) `
    --query properties.outputs -o json | ConvertFrom-Json
  $web = $out.webAppName.value; $sqlServer = $out.sqlServer.value; $db = $out.database.value; $testDb = $out.testDatabase.value
  Write-Host "Web app: $web  SQL: $sqlServer/$db  Test slot: $($out.testUrl.value) on $testDb"

  # Give the web app's managed identity its own database user (read, write, create its tables). Needs a
  # temporary firewall opening for this machine, removed straight after.
  $ip = (Invoke-RestMethod 'https://api.ipify.org').Trim()
  $sqlName = $sqlServer.Split('.')[0]
  Invoke-Az sql server firewall-rule create -g $rg -s $sqlName -n deploy-temp --start-ip-address $ip --end-ip-address $ip --subscription $Subscription -o none
  try {
    $token = Invoke-Az account get-access-token --resource https://database.windows.net/ --subscription $Subscription --query accessToken -o tsv
    # Production's identity on the live database; the test slot's identity (named <web app>/slots/test) on its own (D-060).
    $pairs = @(@{ Db = $db; User = $web })
    if ($testDb) { $pairs += @{ Db = $testDb; User = "$web/slots/test" } }
    foreach ($pair in $pairs) {
    $db = $pair.Db; $user = $pair.User
    $sqlText = @"
-- Re-create the user each time: a rebuilt web app has a new identity under the same name.
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$user')
  DROP USER [$user];
CREATE USER [$user] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [$user];
ALTER ROLE db_datawriter ADD MEMBER [$user];
ALTER ROLE db_ddladmin ADD MEMBER [$user];
"@
    # Windows PowerShell's SqlClient accepts an Entra ID access token. The serverless database may be paused: retry while it wakes.
    $script = @"
`$c = New-Object System.Data.SqlClient.SqlConnection('Server=tcp:$sqlServer,1433;Database=$db;Encrypt=True;Connect Timeout=60')
`$c.AccessToken = '$token'
for (`$i = 0; `$i -lt 6; `$i++) { try { `$c.Open(); break } catch { Start-Sleep -Seconds 15 } }
`$cmd = `$c.CreateCommand(); `$cmd.CommandText = @'
$sqlText
'@
[void]`$cmd.ExecuteNonQuery(); `$c.Close(); 'database user ready: $user on $db'
"@
    $enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    powershell.exe -NoProfile -EncodedCommand $enc
    if ($LASTEXITCODE -ne 0) { throw "database user set-up failed ($db)" }
    }
  }
  finally {
    az sql server firewall-rule delete -g $rg -s $sqlName -n deploy-temp --subscription $Subscription -o none
  }
}

if ($App) {
  Write-Host '== App'
  $web = $WebApp
  $slotArgs = if ($Slot) { @('--slot', $Slot) } else { @() }
  # Production signs people in through External ID only (D-034): local password accounts are for developers.
  # Read the settings as JSON: a JMESPath filter here came back empty from PowerShell on 2026-11-22.
  $mode = ((Invoke-Az webapp config appsettings list -g $rg -n $web @slotArgs --subscription $Subscription -o json) | ConvertFrom-Json | Where-Object name -eq 'Auth__Mode').value
  if ($mode -ne 'External') { throw 'The web app is not set up for External ID sign-in yet: run external-id.ps1 first (D-034).' }
  $publish = Join-Path $root 'publish'
  $zip = Join-Path $root 'publish.zip'
  Remove-Item $publish, $zip -Recurse -Force -ErrorAction SilentlyContinue
  dotnet test (Join-Path $root 'Compicasa.slnx') -c Release --nologo -v q
  if ($LASTEXITCODE -ne 0) { throw 'tests failed: not deploying' }
  dotnet publish (Join-Path $root 'src/Compicasa.Api/Compicasa.Api.csproj') -c Release -o $publish --nologo -v q
  if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
  Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip
  Invoke-Az webapp deploy -g $rg -n $web @slotArgs --subscription $Subscription --src-path $zip --type zip --restart true --track-status false -o none
  $url = "https://$(Invoke-Az webapp show -g $rg -n $web @slotArgs --subscription $Subscription --query defaultHostName -o tsv)"
  for ($i = 0; $i -lt 30; $i++) {
    try { if ((Invoke-WebRequest "$url/healthz/ready" -UseBasicParsing -TimeoutSec 30).Content -eq 'ready') { Write-Host "Live and ready: $url"; break } } catch { }
    Start-Sleep -Seconds 10
  }
  Remove-Item $publish, $zip -Recurse -Force -ErrorAction SilentlyContinue
}
