// Log Analytics + Application Insights (pedido: "Application Insights + Log Analytics (OpenTelemetry)").
// Serilog already writes structured logs in the app (see docs/architecture/overview.md); Application
// Insights is where they — and traces/metrics — land in Azure.
param projectName string
param environmentName string
param location string = resourceGroup().location
param tags object = {}

var logAnalyticsName = '${projectName}-${environmentName}-log'
var appInsightsName = '${projectName}-${environmentName}-appi'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    // Conservative default retention — see docs/privacy-retention.md for the app's own data retention
    // policy; this is infrastructure/technical log retention, a separate concern.
    retentionInDays: environmentName == 'prod' ? 90 : 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
  }
}

output connectionString string = appInsights.properties.ConnectionString
output logAnalyticsWorkspaceId string = logAnalytics.id
