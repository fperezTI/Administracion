// ADR 0015 decision 2: Microsoft Entra ID-only authentication — no SQL login/password exists anywhere,
// consistent with Entra ID already being "la única identidad" for the app itself (pedido §5). App
// Service's managed identity still needs to be created as a database user and granted roles — that one
// step is real T-SQL that Bicep alone can't run reliably against a database it just created in the same
// deployment; it's a documented manual/scripted step in docs/deployment.md's runbook, not automated here.
param projectName string
param environmentName string
param location string = resourceGroup().location
param tags object = {}
@allowed(['GP_S_Gen5_1', 'GP_S_Gen5_2', 'GP_Gen5_2', 'S0', 'S1', 'Basic'])
param sqlSkuName string = 'GP_S_Gen5_1'
@description('Object id (sid) of the Entra ID user or group that becomes the SQL Server Entra admin.')
param sqlAdminEntraObjectId string
@description('Display name/UPN of the Entra ID principal above, as SQL Server records it.')
param sqlAdminEntraLogin string

var uniqueSuffix = uniqueString(resourceGroup().id)
var sqlServerName = take(toLower('${replace(projectName, '-', '')}-${environmentName}-sql-${uniqueSuffix}'), 63)
var sqlDatabaseName = 'AssetManagement'

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'User'
      login: sqlAdminEntraLogin
      sid: sqlAdminEntraObjectId
      tenantId: subscription().tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: sqlSkuName
  }
}

// Serverless/Basic tiers in this SKU family only accept connections from Azure services by default when
// this rule is present — App Service reaches SQL over the public endpoint (no VNet/Private Endpoint in
// V1, see ADR 0015 decision 5 and docs/deployment.md).
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output serverName string = sqlServer.name
output fullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output databaseName string = sqlDatabase.name
