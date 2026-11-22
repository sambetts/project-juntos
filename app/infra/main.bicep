// Compicasa PoC, hosted as production (D-027, D-028). One environment, one resource group, Spain Central.
// Deploy with app/infra/deploy.ps1. No passwords exist: the app reaches Azure SQL with its managed identity,
// and the database accepts Entra ID sign-in only.
targetScope = 'subscription'

@description('Azure region: Spain Central keeps the data in Spain (EU).')
param location string = 'spaincentral'

@description('Short name used in resource names.')
param name string = 'compicasa'

@description('Entra ID object id of the database administrator (a person, for set-up and support only).')
param sqlAdminObjectId string

@description('Sign-in name of the database administrator.')
param sqlAdminLogin string

@secure()
@description('Invite code needed to create an account (the PoC is closed, D-027).')
param inviteCode string

@description('Monthly budget alert for the resource group, in the billing currency.')
param monthlyBudget int = 30

@description('First day of the budget period (first of a month, YYYY-MM-01).')
param budgetStart string = '2026-11-01'

var tags = {
  project: 'compicasa'
  environment: 'prod'
  data: 'made-up only'
  decision: 'D-027'
}

resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${name}-prod'
  location: location
  tags: tags
}

module app 'resources.bicep' = {
  name: 'compicasa-resources'
  scope: rg
  params: {
    location: location
    name: name
    tags: tags
    sqlAdminObjectId: sqlAdminObjectId
    sqlAdminLogin: sqlAdminLogin
    inviteCode: inviteCode
    monthlyBudget: monthlyBudget
    budgetStart: budgetStart
  }
}

output resourceGroup string = rg.name
output webAppName string = app.outputs.webAppName
output url string = app.outputs.url
output sqlServer string = app.outputs.sqlServer
output database string = app.outputs.database
