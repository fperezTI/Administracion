// Stores the exact same images infrastructure/docker/{api,web}.Dockerfile build for local Docker
// Compose — never a separate build artifact for local vs. Azure (see ADR 0015 decision 1).
// adminUserEnabled: false on purpose — App Service pulls images using its own managed identity
// (acrUseManagedIdentityCreds in modules/appService.bicep), never an ACR admin password.
param projectName string
param environmentName string
param location string = resourceGroup().location
param tags object = {}
@allowed(['Basic', 'Standard', 'Premium'])
param sku string = 'Basic'

var uniqueSuffix = uniqueString(resourceGroup().id)
var acrName = take(toLower('${replace(projectName, '-', '')}${environmentName}acr${uniqueSuffix}'), 50)

resource acr 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' = {
  name: acrName
  location: location
  tags: tags
  sku: {
    name: sku
  }
  properties: {
    adminUserEnabled: false
  }
}

output id string = acr.id
output name string = acr.name
output loginServer string = acr.properties.loginServer
