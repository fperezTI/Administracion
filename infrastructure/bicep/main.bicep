// Entry point (resource-group scope) — see docs/deployment.md for the full deployment runbook and
// docs/architecture/decisions/0015-azure-cicd.md for the reasoning behind every choice here.
//
// Parameterized on purpose so this never blocks on real Azure/Entra values (docs/deployment.md, since
// F0: "La infraestructura como código (Bicep) se construye parametrizada para no bloquear el desarrollo
// del código de aplicación"). Deploy with one of infrastructure/bicep/parameters/{dev,test,prod}.bicepparam.
targetScope = 'resourceGroup'

@allowed(['dev', 'test', 'prod'])
param environmentName string

param location string = resourceGroup().location

@description('Short, lowercase, alphanumeric-and-hyphen prefix used to derive every resource name.')
param projectName string = 'assetmgmt'

@description('Object id (sid) of the Entra ID user or group that becomes the SQL Server Entra admin — see the runbook in docs/deployment.md.')
param sqlAdminEntraObjectId string

@description('Display name/UPN of the principal above.')
param sqlAdminEntraLogin string

@description('Entra ID tenant id — same value as ENTRA_TENANT_ID in .env.example, see docs/security/entra-id-setup.md.')
param entraTenantId string

@description('Client id of the API App Registration — same as ENTRA_API_CLIENT_ID.')
param entraApiClientId string

@description('Client id of the Web App Registration — same as ENTRA_WEB_CLIENT_ID.')
param entraWebClientId string

@description('Tag of the asset-management-api image already pushed to the registry this deployment creates/reuses — set by the CD workflow to the commit SHA.')
param apiContainerImageTag string = 'latest'

@description('Tag of the asset-management-web image — set by the CD workflow to the commit SHA.')
param webContainerImageTag string = 'latest'

// prod runs on Basic tiers (B1 / SQL Basic) for cost reasons — a deliberate downgrade from the
// original P1v3 / GP_S_Gen5_2 sizing, made after the first real deployment. Revisit if concurrent
// usage grows: SQL Basic caps at 5 DTU and 2GB, and B1 has no autoscale or deployment slots.
param appServicePlanSkuName string = 'B1'
param sqlSkuName string = environmentName == 'prod' ? 'Basic' : 'GP_S_Gen5_1'
param acrSkuName string = 'Basic'

var tags = {
  project: projectName
  environment: environmentName
  'managed-by': 'bicep'
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    projectName: projectName
    environmentName: environmentName
    location: location
    tags: tags
  }
}

module acr 'modules/containerRegistry.bicep' = {
  name: 'acr'
  params: {
    projectName: projectName
    environmentName: environmentName
    location: location
    tags: tags
    sku: acrSkuName
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    projectName: projectName
    environmentName: environmentName
    location: location
    tags: tags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    projectName: projectName
    environmentName: environmentName
    location: location
    tags: tags
    keyVaultName: keyVault.outputs.name
  }
}

module sql 'modules/sqlDatabase.bicep' = {
  name: 'sql'
  params: {
    projectName: projectName
    environmentName: environmentName
    location: location
    tags: tags
    sqlSkuName: sqlSkuName
    sqlAdminEntraObjectId: sqlAdminEntraObjectId
    sqlAdminEntraLogin: sqlAdminEntraLogin
  }
}

module appServicePlan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    projectName: projectName
    environmentName: environmentName
    location: location
    tags: tags
    skuName: appServicePlanSkuName
  }
}

var blobStorageKeyVaultReference = '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=blob-storage-connection-string)'

// App Service's default hostname is deterministic (<name>.azurewebsites.net) — computed here as plain
// strings, never from the other module's output, specifically so the API and Web modules can reference
// each other's URL (CORS, API_INTERNAL_URL) without an actual circular dependency between them.
var apiAppServiceName = '${projectName}-${environmentName}-api'
var webAppServiceName = '${projectName}-${environmentName}-web'
var apiHostName = '${apiAppServiceName}.azurewebsites.net'
var webHostName = '${webAppServiceName}.azurewebsites.net'

module apiAppService 'modules/appService.bicep' = {
  name: 'apiAppService'
  params: {
    name: apiAppServiceName
    location: location
    tags: tags
    appServicePlanId: appServicePlan.outputs.id
    containerImage: '${acr.outputs.loginServer}/asset-management-api:${apiContainerImageTag}'
    acrLoginServer: acr.outputs.loginServer
    appInsightsConnectionString: monitoring.outputs.connectionString
    appSettings: [
      { name: 'ASPNETCORE_ENVIRONMENT', value: environmentName == 'prod' ? 'Production' : 'Development' }
      { name: 'ASPNETCORE_URLS', value: 'http://+:8080' }
      { name: 'WEBSITES_PORT', value: '8080' }
      // Entra ID-only auth (ADR 0015 decision 2) — no SQL user/password anywhere. The API App Service's
      // own managed identity must still be created as a database user once, out of band — see the
      // runbook in docs/deployment.md.
      {
        name: 'ConnectionStrings__AssetManagementDb'
        value: 'Server=tcp:${sql.outputs.fullyQualifiedDomainName},1433;Database=${sql.outputs.databaseName};Authentication=Active Directory Managed Identity;'
      }
      { name: 'Cors__AllowedOrigins__0', value: 'https://${webHostName}' }
      { name: 'EntraId__Instance', value: environment().authentication.loginEndpoint }
      { name: 'EntraId__TenantId', value: entraTenantId }
      { name: 'EntraId__ClientId', value: entraApiClientId }
      { name: 'BlobStorage__ConnectionString', value: blobStorageKeyVaultReference }
      { name: 'ImportQueue__AzureStorageQueue__ConnectionString', value: blobStorageKeyVaultReference }
    ]
  }
}

module webAppService 'modules/appService.bicep' = {
  name: 'webAppService'
  params: {
    name: webAppServiceName
    location: location
    tags: tags
    appServicePlanId: appServicePlan.outputs.id
    containerImage: '${acr.outputs.loginServer}/asset-management-web:${webContainerImageTag}'
    acrLoginServer: acr.outputs.loginServer
    appInsightsConnectionString: monitoring.outputs.connectionString
    appSettings: [
      { name: 'NODE_ENV', value: 'production' }
      { name: 'PORT', value: '3000' }
      { name: 'WEBSITES_PORT', value: '3000' }
      { name: 'API_INTERNAL_URL', value: 'https://${apiHostName}' }
      { name: 'AUTH_SECRET', value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=auth-secret)' }
      { name: 'AUTH_TRUST_HOST', value: 'true' }
      { name: 'AUTH_URL', value: 'https://${webHostName}' }
      { name: 'ENTRA_TENANT_ID', value: entraTenantId }
      { name: 'ENTRA_CLIENT_ID', value: entraWebClientId }
      { name: 'ENTRA_CLIENT_SECRET', value: '@Microsoft.KeyVault(VaultName=${keyVault.outputs.name};SecretName=entra-web-client-secret)' }
      { name: 'ENTRA_API_CLIENT_ID', value: entraApiClientId }
    ]
  }
}

// Least-privilege role assignments — resolved against "existing" references to the resources created in
// the acr/keyVault modules above, since a role assignment must be scoped to the target resource itself,
// not the whole resource group. The name/scope of a roleAssignment must be computable "at the start of
// the deployment", which rules out referencing a module *output* here (even though it's deterministic) —
// so the same naming formula the acr/keyVault modules use internally is repeated as plain variables
// below, purely so this role-assignment block never depends on those modules' outputs.
var uniqueSuffix = uniqueString(resourceGroup().id)
var acrName = take(toLower('${replace(projectName, '-', '')}${environmentName}acr${uniqueSuffix}'), 50)
var keyVaultName = take(toLower('${replace(projectName, '-', '')}${environmentName}kv${uniqueSuffix}'), 24)

resource acrExisting 'Microsoft.ContainerRegistry/registries@2023-11-01-preview' existing = {
  name: acrName
}

resource keyVaultExisting 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

var acrPullRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var keyVaultSecretsUserRoleId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')

resource apiAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, acrName, apiAppServiceName, 'AcrPull')
  scope: acrExisting
  properties: {
    principalId: apiAppService.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource webAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, acrName, webAppServiceName, 'AcrPull')
  scope: acrExisting
  properties: {
    principalId: webAppService.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: acrPullRoleId
  }
}

resource apiKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, keyVaultName, apiAppServiceName, 'KeyVaultSecretsUser')
  scope: keyVaultExisting
  properties: {
    principalId: apiAppService.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleId
  }
}

resource webKeyVaultSecretsUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(resourceGroup().id, keyVaultName, webAppServiceName, 'KeyVaultSecretsUser')
  scope: keyVaultExisting
  properties: {
    principalId: webAppService.outputs.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleId
  }
}

output apiUrl string = 'https://${apiAppService.outputs.defaultHostName}'
output webUrl string = 'https://${webAppService.outputs.defaultHostName}'
output containerRegistryLoginServer string = acr.outputs.loginServer
output sqlServerFullyQualifiedDomainName string = sql.outputs.fullyQualifiedDomainName
output apiAppServicePrincipalId string = apiAppService.outputs.principalId
output keyVaultName string = keyVault.outputs.name
