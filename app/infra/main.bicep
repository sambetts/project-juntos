// Compicasa PoC, hosted as production (D-027, D-028) on free tiers (D-030): the web app in West Europe, the database
// in France Central until one region offers both free tiers (D-031).
// One environment, one resource group. (The group itself was first made in Spain Central; a group's location is
// only where its metadata lives, and the resources are in West Europe and France Central.)
// Deploy with app/infra/deploy.ps1. No passwords exist: the app reaches Azure SQL with its managed identity,
// and the database accepts Entra ID sign-in only.
targetScope = 'subscription'

@description('Region for every resource. West Europe has App Service F1 quota on this subscription; France Central does not (D-031).')
param location string = 'westeurope'

@description('Region for the database. On 2026-11-22 no EU region offered both App Service F1 and the SQL free offer on this subscription: France Central has the free database but no F1 quota; West Europe has F1 but accepts no new SQL servers; Sweden Central, Italy North and Poland Central fail to create a free database. So the database stays in France Central for now (D-031).')
param sqlLocation string = 'francecentral'

@description('Region of the resource group record (metadata only). It was created in Spain Central and cannot move.')
param groupLocation string = 'spaincentral'

@description('Short name used in resource names.')
param name string = 'compicasa'

@description('Entra ID object id of the database administrator (a person, for set-up and support only).')
param sqlAdminObjectId string

@description('Sign-in name of the database administrator.')
param sqlAdminLogin string

@secure()
@description('Invite code needed to create an account (the PoC is closed, D-027).')
param inviteCode string

@description('Entra External ID sign-in (D-033): the authority URL and the client ID of the app registration, both set by external-id.ps1.')
param externalIdAuthority string = ''
param externalIdClientId string = ''

@description('Monthly budget alert for the resource group, in the billing currency.')
param monthlyBudget int = 5

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
  location: groupLocation
  tags: tags
}

module app 'resources.bicep' = {
  name: 'compicasa-resources'
  scope: rg
  params: {
    location: location
    sqlLocation: sqlLocation
    name: name
    tags: tags
    sqlAdminObjectId: sqlAdminObjectId
    sqlAdminLogin: sqlAdminLogin
    inviteCode: inviteCode
    externalIdAuthority: externalIdAuthority
    externalIdClientId: externalIdClientId
    monthlyBudget: monthlyBudget
    budgetStart: budgetStart
  }
}

output resourceGroup string = rg.name
output webAppName string = app.outputs.webAppName
output url string = app.outputs.url
output sqlServer string = app.outputs.sqlServer
output database string = app.outputs.database
