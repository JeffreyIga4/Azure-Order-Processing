param location string = 'northeurope'
param functionAppName string = 'orderprocessorfunc'
param storageAccountName string = 'orderstoragejiga9876'
param serviceBusNamespace string = 'orderservicebusjiga1234'
param keyVaultName string = 'orderkeyvault123'
param logicAppName string = 'logicapp-scheduled-check'
@secure()
param healthCheckKeySecretId string
param healthCheckUrlBase string



// Adding resources: storage, service bus, function app, key vault, logic app, apim

resource storage 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespace
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
}

resource queue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  name: 'orderqueue'
  parent: serviceBus
  properties: {
    enablePartitioning: false
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }
    tenantId: subscription().tenantId
    accessPolicies: []
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: 'orderplan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storage.properties.primaryEndpoints.blob
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'ServiceBusConnection__fullyQualifiedNamespace'
          value: serviceBus.name
        }
      ]
    }
  }
}

@allowed([
  'true'
  'false'
])
param deployLogicApp string = 'true'

module alerting '../../Azure-HealthCheck/alerting.bicep' = if (deployLogicApp == 'true') {
  name: 'deployScheduledLogicApp'
  params: {
    workflows_logicapp_scheduled_check_name: logicAppName
    healthCheckUrlBase: healthCheckUrlBase
    healthCheckKeySecretId: healthCheckKeySecretId
  }
}
