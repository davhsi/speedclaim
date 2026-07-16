targetScope = 'resourceGroup'

@description('Lowercase resource-name prefix used by the existing SpeedClaim platform.')
param namePrefix string = 'speedclaim'

var uniqueSuffix = toLower(uniqueString(subscription().id, resourceGroup().id, namePrefix))
var storageAccountName = take('st${namePrefix}${uniqueSuffix}', 24)
var keyVaultName = take('kv-${namePrefix}-${uniqueSuffix}', 24)
var serviceBusNamespaceName = take('sb-${namePrefix}-${uniqueSuffix}', 50)
var functionIdentityName = 'id-${namePrefix}-functions'
var applicationInsightsName = 'appi-${namePrefix}-functions'
var emailQueueName = 'email-dispatch'
var functionPlanName = 'plan-${namePrefix}-functions-flex'
var emailFunctionAppName = take('func-${namePrefix}-flex-${uniqueSuffix}', 60)
var deploymentContainerName = 'function-deployments'
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource functionWorkloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: functionIdentityName
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

resource functionEmailListenerRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' existing = {
  name: '${serviceBusNamespaceName}/function-email-listener'
}

resource deploymentContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: '${storageAccount.name}/default/${deploymentContainerName}'
  properties: {
    publicAccess: 'None'
  }
}

resource functionPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionPlanName
  location: resourceGroup().location
  kind: 'functionapp'
  sku: {
    name: 'FC1'
    tier: 'FlexConsumption'
  }
  properties: {
    reserved: true
  }
}

resource emailFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: emailFunctionAppName
  location: resourceGroup().location
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${functionWorkloadIdentity.id}': {}
    }
  }
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    serverFarmId: functionPlan.id
    httpsOnly: true
    keyVaultReferenceIdentity: functionWorkloadIdentity.id
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${storageAccount.properties.primaryEndpoints.blob}${deploymentContainerName}'
          authentication: {
            type: 'StorageAccountConnectionString'
            storageAccountConnectionStringName: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 10
        instanceMemoryMB: 512
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
    siteConfig: {
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: storageConnectionString
        }
        {
          name: 'DEPLOYMENT_STORAGE_CONNECTION_STRING'
          value: storageConnectionString
        }
        {
          name: 'ServiceBusConnection'
          value: functionEmailListenerRule.listKeys().primaryConnectionString
        }
        {
          name: 'ServiceBusQueueName'
          value: emailQueueName
        }
        {
          name: 'SmtpSettings__Host'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/SmtpSettings--Host/)'
        }
        {
          name: 'SmtpSettings__Port'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/SmtpSettings--Port/)'
        }
        {
          name: 'SmtpSettings__SenderName'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/SmtpSettings--SenderName/)'
        }
        {
          name: 'SmtpSettings__SenderEmail'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/SmtpSettings--SenderEmail/)'
        }
        {
          name: 'SmtpSettings__AppPassword'
          value: '@Microsoft.KeyVault(SecretUri=${keyVault.properties.vaultUri}secrets/SmtpSettings--AppPassword/)'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
      ]
    }
  }
}

output emailFunctionAppName string = emailFunctionApp.name
output functionPlanName string = functionPlan.name
