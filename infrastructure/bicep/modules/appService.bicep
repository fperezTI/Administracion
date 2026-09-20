// Reused twice from main.bicep (API, Web) — the difference between the two is entirely in the params
// passed in (name, container image, App Settings), never in this module's own logic. Pulls its image
// with the App Service's own system-assigned identity (acrUseManagedIdentityCreds) — main.bicep grants
// that identity "AcrPull" on the registry, so no registry password ever exists.
param name string
param location string = resourceGroup().location
param tags object = {}
param appServicePlanId string
@description('Full image reference, e.g. myacr.azurecr.io/asset-management-api:<tag>.')
param containerImage string
param acrLoginServer string
param appInsightsConnectionString string
@description('Extra App Settings beyond the baseline (Application Insights, ACR, Docker) this module always sets.')
param appSettings array = []

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerImage}'
      acrUseManagedIdentityCreds: true
      alwaysOn: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: union(
        [
          {
            name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
            value: 'false'
          }
          {
            name: 'DOCKER_REGISTRY_SERVER_URL'
            value: 'https://${acrLoginServer}'
          }
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: appInsightsConnectionString
          }
        ],
        appSettings
      )
    }
  }
}

output principalId string = appService.identity.principalId
output name string = appService.name
output defaultHostName string = appService.properties.defaultHostName
