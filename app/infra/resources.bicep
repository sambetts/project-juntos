// Resources inside rg-compicasa-prod. See main.bicep.
param location string
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
    workspaceCapping: { dailyQuotaGb: json('0.2') } // keeps logging costs near zero
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

// ---------- database: Azure SQL, free offer, Entra ID sign-in only ----------
resource sql 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: 'sql-${name}-prod-${suffix}'
  location: location
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
  location: location
  tags: tags
  // Basic (5 DTU, 2 GB): a small flat monthly price and no cold starts. The free serverless offer was refused
  // in Spain Central on 2026-11-22 ("not supported for provided service level objective or region").
  sku: { name: 'Basic', tier: 'Basic', capacity: 5 }
  properties: {
    maxSizeBytes: 2147483648
    requestedBackupStorageRedundancy: 'Local'
    zoneRedundant: false
  }
}

// ---------- web app: ASP.NET Core API + PWA, Linux, B1 ----------
resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-${name}-prod'
  location: location
  tags: tags
  kind: 'linux'
  sku: { name: 'B1', tier: 'Basic' }
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
      alwaysOn: true // keeps the daily refresh of made-up people running (D-029)
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/healthz'
      appSettings: [
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: insights.properties.ConnectionString }
        { name: 'Database__Provider', value: 'SqlServer' }
        { name: 'ConnectionStrings__Db', value: 'Server=tcp:${sql.properties.fullyQualifiedDomainName},1433;Database=${dbName};Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;Connect Timeout=90' }
        { name: 'Auth__InviteCode', value: inviteCode }
        { name: 'Demo__RefreshDaily', value: 'true' }
        { name: 'WEBSITE_HEALTHCHECK_MAXPINGFAILURES', value: '5' }
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
