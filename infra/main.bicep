targetScope = 'subscription'

@description('Azure region for the resource group and regional resources.')
param location string = 'southindia'

@description('Azure region used by Azure Static Web Apps. It can differ from the application resource group region.')
param staticWebAppLocation string = 'eastasia'

@description('The single production resource group for the internship deployment.')
param resourceGroupName string = 'rg-speedclaim-prod'

@description('A lowercase suffix used in globally unique resource names.')
@minLength(3)
@maxLength(12)
param namePrefix string = 'speedclaim'

@description('Microsoft Entra object ID of the human deploying infrastructure. This principal receives Key Vault secret administration through access policies.')
param deployerObjectId string

@description('PostgreSQL administrator login name. Do not use an email address.')
param postgresAdministratorLogin string = 'speedclaimadmin'

@secure()
@description('PostgreSQL administrator password. Pass it at deployment time; never commit it in a parameter file.')
param postgresAdministratorPassword string

resource productionResourceGroup 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: resourceGroupName
  location: location
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
    purpose: 'internship-demo'
  }
}

module platform 'modules/platform.bicep' = {
  name: 'speedclaim-platform'
  scope: productionResourceGroup
  params: {
    location: location
    staticWebAppLocation: staticWebAppLocation
    namePrefix: namePrefix
    deployerObjectId: deployerObjectId
    postgresAdministratorLogin: postgresAdministratorLogin
    postgresAdministratorPassword: postgresAdministratorPassword
  }
}

output resourceGroupId string = productionResourceGroup.id
output staticWebAppName string = platform.outputs.staticWebAppName
output staticWebAppHostname string = platform.outputs.staticWebAppHostname
output containerRegistryLoginServer string = platform.outputs.containerRegistryLoginServer
output aksName string = platform.outputs.aksName
output aksFqdn string = platform.outputs.aksFqdn
output postgresServerName string = platform.outputs.postgresServerName
output postgresHost string = platform.outputs.postgresHost
output keyVaultName string = platform.outputs.keyVaultName
output keyVaultUri string = platform.outputs.keyVaultUri
output blobStorageAccountName string = platform.outputs.blobStorageAccountName
output serviceBusNamespaceName string = platform.outputs.serviceBusNamespaceName
output emailQueueName string = platform.outputs.emailQueueName
output emailFunctionAppName string = platform.outputs.emailFunctionAppName
