<#
.SYNOPSIS
  Deploys the Compicasa PoC to Azure as production (D-027, D-028): infrastructure, database access, then the app.
.DESCRIPTION
  Needs the Azure CLI signed in to the subscription, and the .NET 10 SDK. Safe to run again: every step is idempotent.
  -Infra   only the Bicep templates and database access
  -App     only build and deploy the app
  (neither: both)
  The invite code is kept in the web app's settings, never in this repository. A new one is made only on the first run,
  or with -NewInviteCode.
#>
param(
  [string]$Subscription = '81209232-7da9-4f81-b984-7ab0bd3dcfb3',
  [string]$Location = 'spaincentral',
  [switch]$Infra,
  [switch]$App,
  [switch]$NewInviteCode,
  [string]$InviteCode  # sets the code, for example to keep the old one when the web app is rebuilt
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$rg = 'rg-compicasa-prod'
if (-not $Infra -and -not $App) { $Infra = $true; $App = $true }

function Invoke-Az { $out = & az @args; if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') failed" }; return $out }

if ($Infra) {
  Write-Host '== Infrastructure'
  $me = Invoke-Az account get-access-token --resource-type ms-graph --subscription $Subscription --query accessToken -o tsv
  $user = Invoke-RestMethod -Headers @{ Authorization = "Bearer $me" } -Uri 'https://graph.microsoft.com/v1.0/me?$select=id,userPrincipalName'

  # Keep the existing invite code unless asked for a new one.
  $invite = $InviteCode
  if (-not $invite -and -not $NewInviteCode) {
    $existing = az webapp list -g $rg --subscription $Subscription --query '[0].name' -o tsv 2>$null
    if ($existing) { $invite = az webapp config appsettings list -g $rg -n $existing --subscription $Subscription --query "[?name=='Auth__InviteCode'].value | [0]" -o tsv 2>$null }
  }
  if (-not $invite) {
    $chars = 'abcdefghjkmnpqrstuvwxyz23456789'.ToCharArray()
    $invite = -join (1..12 | ForEach-Object { $chars[[System.Security.Cryptography.RandomNumberGenerator]::GetInt32($chars.Length)] })
    Write-Host 'New invite code made.'
  }

  $out = Invoke-Az deployment sub create --subscription $Subscription -l $Location -n compicasa-prod `
    --template-file (Join-Path $PSScriptRoot 'main.bicep') `
    -p sqlAdminObjectId=$($user.id) sqlAdminLogin=$($user.userPrincipalName) inviteCode=$invite `
    --query properties.outputs -o json | ConvertFrom-Json
  $web = $out.webAppName.value; $sqlServer = $out.sqlServer.value; $db = $out.database.value
  Write-Host "Web app: $web  SQL: $sqlServer/$db"

  # Give the web app's managed identity its own database user (read, write, create its tables). Needs a
  # temporary firewall opening for this machine, removed straight after.
  $ip = (Invoke-RestMethod 'https://api.ipify.org').Trim()
  $sqlName = $sqlServer.Split('.')[0]
  Invoke-Az sql server firewall-rule create -g $rg -s $sqlName -n deploy-temp --start-ip-address $ip --end-ip-address $ip --subscription $Subscription -o none
  try {
    $token = Invoke-Az account get-access-token --resource https://database.windows.net/ --subscription $Subscription --query accessToken -o tsv
    $sqlText = @"
-- Re-create the user each time: a rebuilt web app has a new identity under the same name.
IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$web')
  DROP USER [$web];
CREATE USER [$web] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [$web];
ALTER ROLE db_datawriter ADD MEMBER [$web];
ALTER ROLE db_ddladmin ADD MEMBER [$web];
"@
    # Windows PowerShell's SqlClient accepts an Entra ID access token. The serverless database may be paused: retry while it wakes.
    $script = @"
`$c = New-Object System.Data.SqlClient.SqlConnection('Server=tcp:$sqlServer,1433;Database=$db;Encrypt=True;Connect Timeout=60')
`$c.AccessToken = '$token'
for (`$i = 0; `$i -lt 6; `$i++) { try { `$c.Open(); break } catch { Start-Sleep -Seconds 15 } }
`$cmd = `$c.CreateCommand(); `$cmd.CommandText = @'
$sqlText
'@
[void]`$cmd.ExecuteNonQuery(); `$c.Close(); 'database user ready'
"@
    $enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
    powershell.exe -NoProfile -EncodedCommand $enc
    if ($LASTEXITCODE -ne 0) { throw 'database user set-up failed' }
  }
  finally {
    az sql server firewall-rule delete -g $rg -s $sqlName -n deploy-temp --subscription $Subscription -o none
  }
}

if ($App) {
  Write-Host '== App'
  $web = Invoke-Az webapp list -g $rg --subscription $Subscription --query '[0].name' -o tsv
  $publish = Join-Path $root 'publish'
  $zip = Join-Path $root 'publish.zip'
  Remove-Item $publish, $zip -Recurse -Force -ErrorAction SilentlyContinue
  dotnet test (Join-Path $root 'Compicasa.slnx') -c Release --nologo -v q
  if ($LASTEXITCODE -ne 0) { throw 'tests failed: not deploying' }
  dotnet publish (Join-Path $root 'src/Compicasa.Api/Compicasa.Api.csproj') -c Release -o $publish --nologo -v q
  if ($LASTEXITCODE -ne 0) { throw 'publish failed' }
  Compress-Archive -Path (Join-Path $publish '*') -DestinationPath $zip
  Invoke-Az webapp deploy -g $rg -n $web --subscription $Subscription --src-path $zip --type zip --restart true --track-status false -o none
  $url = "https://$(Invoke-Az webapp show -g $rg -n $web --subscription $Subscription --query defaultHostName -o tsv)"
  for ($i = 0; $i -lt 30; $i++) {
    try { if ((Invoke-WebRequest "$url/healthz/ready" -UseBasicParsing -TimeoutSec 30).Content -eq 'ready') { Write-Host "Live and ready: $url"; break } } catch { }
    Start-Sleep -Seconds 10
  }
  Remove-Item $publish, $zip -Recurse -Force -ErrorAction SilentlyContinue
}
