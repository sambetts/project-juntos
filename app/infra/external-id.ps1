<#
.SYNOPSIS
  Sets up sign-in for the Compicasa PoC with Microsoft Entra External ID (D-033), then points the web app at it.
.DESCRIPTION
  Safe to run again: each step looks for what already exists before creating anything.
  1. An external tenant, "Compicasa" (compicasa.onmicrosoft.com, sign-in at compicasa.ciamlogin.com), in its own
     resource group (rg-compicasa-identity) and linked to the subscription for billing. Free for the first 50,000
     monthly active users; no premium add-ons (SMS, ID checks) are used.
  2. You sign in to the new tenant once (a browser opens). Whoever creates the tenant is its administrator.
  3. An app registration for the web app: ID tokens only, no client secret.
  4. A sign-up and sign-in flow: email and password, the email confirmed by a one-time code, and a name to show.
  5. The web app's settings: Auth__Mode=External and the tenant's details.
  Needs the Azure CLI signed in to the subscription, and PowerShell 7.
#>
param(
  [string]$Subscription = '81209232-7da9-4f81-b984-7ab0bd3dcfb3',
  [string]$IdentityGroup = 'rg-compicasa-identity',
  [string]$AppGroup = 'rg-compicasa-prod',
  [string]$Prefix = 'compicasa',          # the tenant's domain prefix
  [string]$DisplayName = 'Compicasa',
  [string]$Country = 'ES'                 # decides where the tenant's data lives: Spain → Europe
)
$ErrorActionPreference = 'Stop'
function Invoke-Az { $out = & az @args; if ($LASTEXITCODE -ne 0) { throw "az $($args -join ' ') failed" }; return $out }

# ---------- 1. the external tenant ----------
Write-Host '== External tenant'
$arm = "https://management.azure.com/subscriptions/$Subscription/resourceGroups/$IdentityGroup/providers/Microsoft.AzureActiveDirectory/ciamDirectories/$($Prefix)?api-version=2023-05-17-preview"
Invoke-Az group create -n $IdentityGroup -l westeurope --subscription $Subscription --tags project=compicasa purpose=external-id decision=D-033 -o none
$dir = az rest --method get --url $arm 2>$null | ConvertFrom-Json
if (-not $dir) {
  $body = @{ location = 'Europe'; sku = @{ name = 'Standard'; tier = 'A0' }; tags = @{ project = 'compicasa' }
             properties = @{ createTenantProperties = @{ displayName = $DisplayName; countryCode = $Country } } } | ConvertTo-Json -Depth 5 -Compress
  $bodyFile = New-TemporaryFile; Set-Content $bodyFile $body
  Invoke-Az rest --method put --url $arm --body "@$bodyFile" -o none
  Remove-Item $bodyFile
  Write-Host 'Creating the tenant (a few minutes)…'
}
for ($i = 0; $i -lt 60; $i++) {
  $dir = Invoke-Az rest --method get --url $arm | ConvertFrom-Json
  if ($dir.properties.provisioningState -eq 'Succeeded') { break }
  if ($dir.properties.provisioningState -eq 'Failed') { throw "Tenant creation failed: $($dir | ConvertTo-Json -Depth 5)" }
  Start-Sleep -Seconds 15
}
$tenantId = $dir.properties.tenantId
if (-not $tenantId) { throw 'The tenant has no ID yet; run the script again in a few minutes.' }
$authority = "https://$Prefix.ciamlogin.com/$tenantId/v2.0"
Write-Host "Tenant $tenantId ($Prefix.onmicrosoft.com)"

# ---------- 2. sign in to the new tenant ----------
Write-Host '== Signing in to the new tenant (a browser opens; use the same account)'
$token = az account get-access-token --tenant $tenantId --resource-type ms-graph --query accessToken -o tsv 2>$null
if (-not $token) {
  Invoke-Az login --tenant $tenantId --allow-no-subscriptions -o none
  $token = Invoke-Az account get-access-token --tenant $tenantId --resource-type ms-graph --query accessToken -o tsv
}
$h = @{ Authorization = "Bearer $token"; 'Content-Type' = 'application/json' }
$graph = 'https://graph.microsoft.com/v1.0'
function Get-Graph($path) { Invoke-RestMethod -Headers $h -Uri "$graph$path" }
function Send-Graph($method, $path, $body) { Invoke-RestMethod -Method $method -Headers $h -Uri "$graph$path" -Body ($body | ConvertTo-Json -Depth 12 -Compress) }

# ---------- 3. the app registration ----------
Write-Host '== App registration'
$host_ = Invoke-Az webapp list -g $AppGroup --subscription $Subscription --query '[0].defaultHostName' -o tsv
$site = "https://$host_"
$web = @{
  redirectUris = @("$site/signin-oidc", 'https://localhost:7005/signin-oidc')
  logoutUrl = "$site/signout-callback-oidc"
  implicitGrantSettings = @{ enableIdTokenIssuance = $true; enableAccessTokenIssuance = $false }
}
# Microsoft Graph's delegated scopes openid, profile and email: enough for an ID token with the email and name.
$graphAppId = '00000003-0000-0000-c000-000000000000'
$scopes = @(
  @{ id = '37f7f235-527c-4136-accd-4a02d197296e'; type = 'Scope' },  # openid
  @{ id = '14dad69e-099b-42c9-810b-d002981feec1'; type = 'Scope' },  # profile
  @{ id = '64a6cdd6-aab1-4aaf-94b8-3cc8405e90d0'; type = 'Scope' })  # email
$app = (Get-Graph "/applications?`$filter=displayName eq 'Compicasa web'").value | Select-Object -First 1
if (-not $app) {
  $app = Send-Graph Post '/applications' @{ displayName = 'Compicasa web'; signInAudience = 'AzureADMyOrg'; web = $web
    requiredResourceAccess = @(@{ resourceAppId = $graphAppId; resourceAccess = $scopes }) }
} else {
  Send-Graph Patch "/applications/$($app.id)" @{ web = $web } | Out-Null
}
$sp = (Get-Graph "/servicePrincipals?`$filter=appId eq '$($app.appId)'").value | Select-Object -First 1
if (-not $sp) { $sp = Send-Graph Post '/servicePrincipals' @{ appId = $app.appId } }
# People in an external tenant can't consent for themselves, so the administrator consents for everyone, once.
$graphSp = (Get-Graph "/servicePrincipals?`$filter=appId eq '$graphAppId'").value | Select-Object -First 1
$grant = (Get-Graph "/oauth2PermissionGrants?`$filter=clientId eq '$($sp.id)' and resourceId eq '$($graphSp.id)'").value | Select-Object -First 1
if (-not $grant) { Send-Graph Post '/oauth2PermissionGrants' @{ clientId = $sp.id; consentType = 'AllPrincipals'; resourceId = $graphSp.id; scope = 'openid profile email' } | Out-Null }
Write-Host "App $($app.appId)"

# ---------- 4. the sign-up and sign-in flow ----------
Write-Host '== Sign-up and sign-in flow'
$flowName = 'Compicasa: email sign-up and sign-in'
$flows = (Get-Graph '/identity/authenticationEventsFlows').value
$flow = $flows | Where-Object displayName -eq $flowName | Select-Object -First 1
if (-not $flow) {
  $flow = Send-Graph Post '/identity/authenticationEventsFlows' @{
    '@odata.type' = '#microsoft.graph.externalUsersSelfServiceSignUpEventsFlow'
    displayName = $flowName
    onAuthenticationMethodLoadStart = @{ '@odata.type' = '#microsoft.graph.onAuthenticationMethodLoadStartExternalUsersSelfServiceSignUp'
      identityProviders = @(@{ id = 'EmailPassword-OAUTH' }) }
    onInteractiveAuthFlowStart = @{ '@odata.type' = '#microsoft.graph.onInteractiveAuthFlowStartExternalUsersSelfServiceSignUp'; isSignUpAllowed = $true }
    onAttributeCollection = @{ '@odata.type' = '#microsoft.graph.onAttributeCollectionExternalUsersSelfServiceSignUp'
      attributes = @(
        @{ id = 'email'; displayName = 'Email Address'; description = 'Email address of the user'; userFlowAttributeType = 'builtIn'; dataType = 'string' },
        @{ id = 'displayName'; displayName = 'Display Name'; description = 'Display Name of the User.'; userFlowAttributeType = 'builtIn'; dataType = 'string' })
      attributeCollectionPage = @{ views = @(@{ inputs = @(
        @{ attribute = 'email'; label = 'Email Address'; inputType = 'text'; hidden = $true; editable = $false; writeToDirectory = $true; required = $true
           validationRegEx = '^[a-zA-Z0-9.!#$%&''*+/=?^_`{|}~-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)*$' },
        @{ attribute = 'displayName'; label = 'Display Name'; inputType = 'text'; hidden = $false; editable = $true; writeToDirectory = $true; required = $true
           validationRegEx = '^.{1,40}$' }) }) }
    }
  }
}
$linked = (Get-Graph "/identity/authenticationEventsFlows/$($flow.id)/conditions/applications/includeApplications").value | Where-Object appId -eq $app.appId
if (-not $linked) {
  Send-Graph Post "/identity/authenticationEventsFlows/$($flow.id)/conditions/applications/includeApplications" @{
    '@odata.type' = '#microsoft.graph.authenticationConditionApplication'; appId = $app.appId } | Out-Null
}
Write-Host "Flow $($flow.id)"

# ---------- 5. point the web app at it ----------
Write-Host '== Web app settings'
$webName = Invoke-Az webapp list -g $AppGroup --subscription $Subscription --query '[0].name' -o tsv
Invoke-Az webapp config appsettings set -g $AppGroup -n $webName --subscription $Subscription -o none --settings `
  Auth__Mode=External "Auth__External__Authority=$authority" "Auth__External__ClientId=$($app.appId)"
Write-Host "Done. Authority $authority, client $($app.appId). Deploy the app with deploy.ps1 -App."
