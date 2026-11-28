<#
.SYNOPSIS
  Lets the app remove a person's sign-in record at Microsoft Entra External ID when they delete their account (B-039).
.DESCRIPTION
  Safe to run again. Needs you, as the External ID tenant's administrator, to sign in (a browser opens, with MFA).
  1. An app registration in the External ID tenant, "Compicasa: erase sign-ins", single tenant, no sign-in of its own.
  2. The Microsoft Graph application permission User.ReadWrite.All, granted by you as administrator (needed to delete
     users with app-only access). The app uses it for DELETE /users/{id} only (Erasure.cs).
  3. A client secret valid for 12 months, written straight into the web app's settings (production and the test
     slot share the tenant): Auth__External__TenantId, Auth__Erase__ClientId, Auth__Erase__ClientSecret. The secret is
     never printed or written to disk. Run the script again before it expires to replace it.
  Until this has run, deleted accounts' sign-in records wait in the SignInErasures table, and #reports says so.
  Needs the Azure CLI and PowerShell 7.
#>
param(
  [string]$Subscription = '81209232-7da9-4f81-b984-7ab0bd3dcfb3',
  [string]$AppGroup = 'rg-compicasa-prod',
  [string]$WebApp = 'compicasa',
  [string]$Name = 'Compicasa: erase sign-ins'
)
$ErrorActionPreference = 'Stop'
function Invoke-Az { $out = & az @args; if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') failed" }; return $out }

# The External ID tenant, from the web app's own settings (https://<prefix>.ciamlogin.com/<tenant id>/v2.0).
Invoke-Az account set --subscription $Subscription
$authority = Invoke-Az webapp config appsettings list -g $AppGroup -n $WebApp --query "[?name=='Auth__External__Authority'].value | [0]" -o tsv
if ($authority -notmatch '/([0-9a-f-]{36})/') { throw "Couldn't read the External ID tenant from Auth__External__Authority." }
$tenantId = $Matches[1]
Write-Host "External ID tenant $tenantId"

Write-Host '== Signing in to the External ID tenant (a browser opens; use its administrator account)'
$token = az account get-access-token --tenant $tenantId --resource-type ms-graph --query accessToken -o tsv 2>$null
if (-not $token) {
  Invoke-Az login --tenant $tenantId --scope 'https://graph.microsoft.com//.default' --allow-no-subscriptions -o none
  $token = Invoke-Az account get-access-token --tenant $tenantId --resource-type ms-graph --query accessToken -o tsv
}
$h = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }
$g = 'https://graph.microsoft.com/v1.0'

try {
  # ---------- 1. the app registration ----------
  $app = (Invoke-RestMethod -Headers $h "$g/applications?`$filter=displayName eq '$Name'").value | Select-Object -First 1
  if (-not $app) {
    $app = Invoke-RestMethod -Method Post -Headers $h "$g/applications" -Body (@{ displayName = $Name; signInAudience = 'AzureADMyOrg' } | ConvertTo-Json)
    Write-Host "Created app $($app.appId)"
  }
  $sp = (Invoke-RestMethod -Headers $h "$g/servicePrincipals?`$filter=appId eq '$($app.appId)'").value | Select-Object -First 1
  if (-not $sp) { $sp = Invoke-RestMethod -Method Post -Headers $h "$g/servicePrincipals" -Body (@{ appId = $app.appId } | ConvertTo-Json) }

  # ---------- 2. User.ReadWrite.All, granted ----------
  $graphSp = (Invoke-RestMethod -Headers $h "$g/servicePrincipals?`$filter=appId eq '00000003-0000-0000-c000-000000000000'").value[0]
  $role = $graphSp.appRoles | Where-Object { $_.value -eq 'User.ReadWrite.All' -and $_.allowedMemberTypes -contains 'Application' }
  $have = (Invoke-RestMethod -Headers $h "$g/servicePrincipals/$($sp.id)/appRoleAssignments").value | Where-Object { $_.appRoleId -eq $role.id }
  if (-not $have) {
    Invoke-RestMethod -Method Post -Headers $h "$g/servicePrincipals/$($sp.id)/appRoleAssignments" `
      -Body (@{ principalId = $sp.id; resourceId = $graphSp.id; appRoleId = $role.id } | ConvertTo-Json) | Out-Null
    Write-Host 'Granted User.ReadWrite.All'
  }

  # ---------- 3. a 12-month secret, straight into the web app ----------
  $end = (Get-Date).ToUniversalTime().AddMonths(12).ToString('o')
  $secret = Invoke-RestMethod -Method Post -Headers $h "$g/applications/$($app.id)/addPassword" `
    -Body (@{ passwordCredential = @{ displayName = "web app $(Get-Date -Format yyyy-MM-dd)"; endDateTime = $end } } | ConvertTo-Json)
} finally {
  # Back to the subscription for the web app settings.
  Invoke-Az account set --subscription $Subscription
}
$settings = @("Auth__External__TenantId=$tenantId", "Auth__Erase__ClientId=$($app.appId)", "Auth__Erase__ClientSecret=$($secret.secretText)")
Invoke-Az webapp config appsettings set -g $AppGroup -n $WebApp --settings @settings -o none
Invoke-Az webapp config appsettings set -g $AppGroup -n $WebApp --slot test --settings @settings -o none
Write-Host "Done. The secret expires $end; run this script again before then. Older secrets on '$Name' can be removed in the tenant."
