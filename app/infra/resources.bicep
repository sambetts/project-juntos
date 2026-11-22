// Resources inside rg-compicasa-prod. See main.bicep.
@description('The App Service plan the web app runs on: the existing BuildSyncProd (Windows, P1v2, UK South; D-036), in its own resource group.')
param planId string
@description('Agencies the homes crawler must leave alone and whose homes are hidden (D-035), comma-separated; kept across deploys.')
param homesStopped string = ''
param location string
@description('Region for the database. Ideally the same as location (D-031); see main.bicep for why it may differ.')
param sqlLocation string = location
param name string
param tags object
param sqlAdminObjectId string
param sqlAdminLogin string
@secure()
param inviteCode string
@description('Sign-in through Entra External ID (D-033), set by external-id.ps1. Empty means local accounts, which is for developers only (D-034).')
param externalIdAuthority string = ''
param externalIdClientId string = ''
param monthlyBudget int
param budgetStart string

var suffix = substring(uniqueString(resourceGroup().id), 0, 5)
var dbName = 'sqldb-${name}'
var sqlServerName = 'sql-${name}-${sqlLocation}-${suffix}'

// ---------- monitoring ----------
resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${name}-prod'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
    workspaceCapping: { dailyQuotaGb: json('0.1') } // at most about 3 GB a month, inside the free 5 GB allowance (D-030)
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${name}-prod'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logs.id
    DisableLocalAuth: false
  }
}

// ---------- database: Azure SQL free offer, Entra ID sign-in only (D-030) ----------
resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: sqlLocation
  tags: tags
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: sqlAdminLogin
      sid: sqlAdminObjectId
      tenantId: subscription().tenantId
      principalType: 'User'
    }
  }
}

// Lets Azure services (the web app) reach the server; sign-in still needs an Entra ID identity with a database user.
resource sqlAllowAzure 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sql
  name: 'AllowAllWindowsAzureIps'
  properties: { startIpAddress: '0.0.0.0', endIpAddress: '0.0.0.0' }
}

resource db 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sql
  name: dbName
  location: sqlLocation
  tags: tags
  // The free offer: serverless, 100,000 vCore-seconds and 32 GB a month at no charge. If the allowance runs out
  // it pauses until next month instead of billing. Refused in Spain Central on 2026-11-22, hence another region.
  sku: { name: 'GP_S_Gen5_2', tier: 'GeneralPurpose', family: 'Gen5', capacity: 2 }
  properties: {
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause'
    autoPauseDelay: 60
    minCapacity: json('0.5')
    maxSizeBytes: 34359738368
    requestedBackupStorageRedundancy: 'Local'
    zoneRedundant: false
  }
}

// ---------- web app: ASP.NET Core API + PWA, Windows, on the shared BuildSyncProd plan (D-036) ----------
resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-${name}-prod-${suffix}'
  location: location
  tags: tags
  kind: 'app'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: planId
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      netFrameworkVersion: 'v10.0'
      metadata: [ { name: 'CURRENT_STACK', value: 'dotnet' } ]
      use32BitWorkerProcess: false
      alwaysOn: true // needed for the scheduled WebJob (the daily homes crawl, D-036)
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
        { name: 'Database__Provider', value: 'SqlServer' }
        { name: 'ConnectionStrings__Db', value: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${dbName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connect Timeout=90' }
        { name: 'Auth__InviteCode', value: inviteCode }
        { name: 'Auth__Mode', value: empty(externalIdClientId) ? 'Local' : 'External' }
        { name: 'Auth__External__Authority', value: externalIdAuthority }
        { name: 'Auth__External__ClientId', value: externalIdClientId }
        { name: 'Demo__RefreshDaily', value: 'true' }
        { name: 'Homes__Stopped', value: homesStopped }
        // No made-up homes on the live site: the crawler fills the index (F-019).
        { name: 'Homes__Demo', value: 'false' }
        // The WebJob's schedule (App_Data/jobs/triggered/homes-sync/settings.job) is in Madrid time.
        { name: 'WEBSITE_TIME_ZONE', value: 'Romance Standard Time' }
        // The first crawl reads every advert (about two hours); don't kill it for being quiet.
        { name: 'WEBJOBS_IDLE_TIMEOUT', value: '3600' }
      ]
    }
  }
}

// No FTP. Deployments go through the Azure CLI with Entra ID.
resource ftp 'Microsoft.Web/sites/basicPublishingCredentialsPolicies@2023-12-01' = {
  parent: web
  name: 'ftp'
  properties: { allow: false }
}

resource logsToWorkspace 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name: 'to-log-analytics'
  scope: web
  properties: {
    workspaceId: logs.id
    logs: [
      { category: 'AppServiceHTTPLogs', enabled: true }
      { category: 'AppServiceAppLogs', enabled: true }
    ]
  }
}

// ---------- cost guard ----------
resource budget 'Microsoft.Consumption/budgets@2023-11-01' = {
  name: 'budget-${name}-prod'
  properties: {
    category: 'Cost'
    amount: monthlyBudget
    timeGrain: 'Monthly'
    timePeriod: { startDate: budgetStart }
    notifications: {
      actual80: { enabled: true, operator: 'GreaterThanOrEqualTo', threshold: 80, thresholdType: 'Actual', contactRoles: [ 'Owner' ], contactEmails: [] }
      forecast100: { enabled: true, operator: 'GreaterThanOrEqualTo', threshold: 100, thresholdType: 'Forecasted', contactRoles: [ 'Owner' ], contactEmails: [] }
    }
  }
}

output webAppName string = web.name
output url string = 'https://${web.properties.defaultHostName}'
output sqlServer string = sql.properties.fullyQualifiedDomainName
output database string = dbName
