// Compicasa PoC, hosted as production (D-027, D-028), in UK South (D-036): the web app runs on the existing
// App Service plan BuildSyncProd (Windows, P1v2, its own resource group), with the daily homes crawl as a WebJob;
// the database on Azure SQL Basic (D-058; the free offer ran out). One environment, one resource group (recreated in UK South, 2026-11-22).
// Deploy with app/infra/deploy.ps1. No passwords exist: the app reaches Azure SQL with its managed identity,
// and the database accepts Entra ID sign-in only.
targetScope = 'subscription'

@description('Region for every resource: where the BuildSyncProd plan is (D-036).')
param location string = 'uksouth'

@description('Region for the database; the same as the web app unless a tier is refused there.')
param sqlLocation string = location

@description('Region of the resource group record (metadata only).')
param groupLocation string = location

@description('The App Service plan to run on (D-036). Empty: create a plan of its own in rg-compicasa-prod (planSku), for a subscription without BuildSyncProd (B-073, D-049).')
param planId string = '/subscriptions/${subscription().subscriptionId}/resourceGroups/BuildSyncProd/providers/Microsoft.Web/serverfarms/BuildSyncProd'

@description('Size of the plan created when planId is empty. B1 is the smallest with Always On, which the daily WebJob needs.')
param planSku string = 'B1'

@description('Agencies the homes crawler leaves alone (D-035), kept across deploys by deploy.ps1.')
param homesStopped string = ''

@description('Account ids that may see the release funnel (B-074), comma-separated; kept across deploys by deploy.ps1. Ids only, no personal data.')
param adminAccountIds string = ''

@description('Short name used in resource names.')
param name string = 'compicasa'

@description('The web app, and so the address <name>.azurewebsites.net (D-057).')
param webAppName string = name

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
param monthlyBudget int = 10 // D-058: the Basic database is about EUR 5.49 a month

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
    planId: planId
    planSku: planSku
    homesStopped: homesStopped
    adminAccountIds: adminAccountIds
    name: name
    webAppName: webAppName
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
