using '../main.bicep'

// Every CHANGE_ME below is a real placeholder — nothing here is a secret (Bicep parameter files must
// never contain one; see docs/deployment.md's runbook for where each of these actually comes from).
param environmentName = 'test'
param location = 'mexicocentral' // CHANGE_ME if a different Azure region is preferred
param projectName = 'assetmgmt'

param sqlAdminEntraObjectId = 'CHANGE_ME-object-id-of-the-Entra-admin-user-or-group'
param sqlAdminEntraLogin = 'CHANGE_ME-admin@yourtenant.onmicrosoft.com'

param entraTenantId = 'CHANGE_ME-same-value-as-ENTRA_TENANT_ID'
param entraApiClientId = 'CHANGE_ME-same-value-as-ENTRA_API_CLIENT_ID'
param entraWebClientId = 'CHANGE_ME-same-value-as-ENTRA_WEB_CLIENT_ID'

param apiContainerImageTag = 'latest'
param webContainerImageTag = 'latest'
