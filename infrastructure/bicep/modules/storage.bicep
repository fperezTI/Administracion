// Blob container for documents (F8, Document.BlobPath) + the "import-batches" queue F9's
// AzureStorageQueueImportQueue already expects by that exact name (ADR 0003/0011) — same Storage Account,
// same connection-string-per-environment strategy the app has used since local Azurite.
//
// ADR 0015 decision 3: the connection string is written into Key Vault as a secret (App Service
// references it via @Microsoft.KeyVault(...)), not consumed via managed identity — switching
// AzureBlobFileStorage/AzureStorageQueueImportQueue to DefaultAzureCredential is real application code
// that can't be verified against a real Azure subscription in this environment; a Key Vault-referenced
// connection string is just as free of secrets in the repository/App Settings without touching that code.
param projectName string
param environmentName string
param location string = resourceGroup().location
param tags object = {}
param keyVaultName string

var uniqueSuffix = uniqueString(resourceGroup().id)
var storageAccountName = take(toLower('${replace(projectName, '-', '')}${environmentName}st${uniqueSuffix}'), 24)

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  // Standard_GRS requires a paired region for geo-replication; mexicocentral has none. Started on
  // Standard_ZRS (zone redundancy), then downgraded to Standard_LRS for cost during this initial,
  // low-traffic stage — revisit if data durability requirements grow.
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource documentsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: blobService
  name: 'documents'
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource importBatchesQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-01-01' = {
  parent: queueService
  name: 'import-batches'
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource storageConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'blob-storage-connection-string'
  properties: {
    value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
  }
}

output accountName string = storageAccount.name
output id string = storageAccount.id
