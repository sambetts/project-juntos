// Resources inside rg-compicasa-prod. See main.bicep.
param location string
@description('Region for the database: the SQL free offer is not available in Spain Central (D-030).')
param sqlLocation string
param name string
param tags object
param sqlAdminObjectId string
param sqlAdminLogin string
@secure()
param inviteCode string
param monthlyBudget int
param budgetStart string

var suffix = substring(uniqueString(resourceGroup().id), 0, 5)
var dbName = 'sqldb-${name}'

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
  name: 'sql-${name}-eu-${suffix}'
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
  // it pauses until next month instead of billing. Refused in Spain Central on 2026-11-22, hence sqlLocation.
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

// ---------- web app: ASP.NET Core API + PWA, Linux, F1 (free, D-030) ----------
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-${name}-prod'
  location: location
  tags: tags
  kind: 'linux'
  sku: { name: 'F1', tier: 'Free' }
  properties: { reserved: true }
}

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-${name}-prod-${suffix}'
  location: location
  tags: tags
  kind: 'app,linux'
  identity: { type: 'SystemAssigned' }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      alwaysOn: false // not available on F1: the app sleeps when idle and refreshes the made-up people when it wakes (D-029)
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
        { name: 'Database__Provider', value: 'SqlServer' }
        { name: 'ConnectionStrings__Db', value: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${dbName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connect Timeout=90' }
        { name: 'Auth__InviteCode', value: inviteCode }
        { name: 'Demo__RefreshDaily', value: 'true' }
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
      { category: 'AppServiceConsoleLogs', enabled: true }
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
