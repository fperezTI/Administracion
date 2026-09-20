// RBAC authorization (not classic access policies) — App Service identities are granted "Key Vault
// Secrets User" at the resource-group level in main.bicep, once both App Services exist and their
// principal ids are known.
//
// The two placeholder secrets below (Entra web client secret, NextAuth AUTH_SECRET) cannot be generated
// by Bicep — they come from a real Entra App Registration and a random value the operator generates once
// (see docs/deployment.md's runbook). Bicep creates the secret *shell* so the Key Vault reference in
// modules/appService.bicep resolves to something at deploy time; the operator replaces the placeholder
// value with `az keyvault secret set` after the first deployment, before real traffic is expected.
param projectName string
param environmentName string
param location string = resourceGroup().location
param tags object = {}

var uniqueSuffix = uniqueString(resourceGroup().id)
var keyVaultName = take(toLower('${replace(projectName, '-', '')}${environmentName}kv${uniqueSuffix}'), 24)

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    // Purge protection can only ever be turned on, never off once set — left off outside prod so a
    // throwaway dev/test vault can actually be purged and its name reused during iteration.
    enablePurgeProtection: environmentName == 'prod' ? true : false
  }
}

resource entraWebClientSecretPlaceholder 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'entra-web-client-secret'
  properties: {
    value: 'REPLACE_ME_AFTER_DEPLOYMENT'
  }
}

resource authSecretPlaceholder 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'auth-secret'
  properties: {
    value: 'REPLACE_ME_AFTER_DEPLOYMENT'
  }
}

output id string = keyVault.id
output name string = keyVault.name
output uri string = keyVault.properties.vaultUri
