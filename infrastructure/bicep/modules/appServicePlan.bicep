// One Linux plan shared by both App Services (API, Web) in an environment.
param projectName string
param environmentName string
param location string = resourceGroup().location
param tags object = {}
param skuName string = 'B1'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${projectName}-${environmentName}-plan'
  location: location
  tags: tags
  kind: 'linux'
  properties: {
    reserved: true
  }
  sku: {
    name: skuName
  }
}

output id string = appServicePlan.id
