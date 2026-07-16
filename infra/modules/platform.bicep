@description('Azure region for regional resources.')
param location string

@description('Azure region used by Azure Static Web Apps.')
param staticWebAppLocation string

@description('Lowercase resource-name prefix.')
param namePrefix string

@description('Microsoft Entra object ID of the human who deploys and bootstraps Key Vault secrets.')
param deployerObjectId string

param postgresAdministratorLogin string

@secure()
param postgresAdministratorPassword string

var uniqueSuffix = toLower(uniqueString(subscription().id, resourceGroup().id, namePrefix))
var storageAccountName = take('st${namePrefix}${uniqueSuffix}', 24)
var registryName = take('acr${namePrefix}${uniqueSuffix}', 50)
var keyVaultName = take('kv-${namePrefix}-${uniqueSuffix}', 24)
var postgresServerName = take('psql-${namePrefix}-${uniqueSuffix}', 63)
var aksName = 'aks-${namePrefix}-prod'
var staticWebAppName = take('swa-${namePrefix}-${uniqueSuffix}', 60)
var serviceBusNamespaceName = take('sb-${namePrefix}-${uniqueSuffix}', 50)
var emailFunctionAppName = take('func-${namePrefix}-${uniqueSuffix}', 60)
var functionPlanName = 'plan-${namePrefix}-functions'
var apiIdentityName = 'id-${namePrefix}-api'
var functionIdentityName = 'id-${namePrefix}-functions'
var emailQueueName = 'email-dispatch'
var apiNamespace = 'speedclaim'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${namePrefix}-prod'
  location: location
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 180
    features: {
      searchVersion: 1
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${namePrefix}-functions'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    DisableLocalAuth: false
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true // Contributor-only deployment bootstrap uses connection strings.
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storageAccount
  name: 'default'
}

resource uploadsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: 'speedclaim-uploads'
  properties: {
    publicAccess: 'None'
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: registryName
  location: location
  sku: {
    name: 'Basic'
  }
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    adminUserEnabled: true // Temporary Contributor-only image-pull and GitHub CI workaround.
    publicNetworkAccess: 'Enabled'
    policies: {
      quarantinePolicy: {
        status: 'disabled'
      }
    }
  }
}

resource apiWorkloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: apiIdentityName
  location: location
}

resource functionWorkloadIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: functionIdentityName
  location: location
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: false // Access policies are compatible with Contributor-only access.
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
    publicNetworkAccess: 'Enabled'
    accessPolicies: [
      {
        tenantId: tenant().tenantId
        objectId: deployerObjectId
        permissions: {
          secrets: [
            'get'
            'list'
            'set'
            'delete'
            'recover'
            'backup'
            'restore'
          ]
        }
      }
      {
        tenantId: tenant().tenantId
        objectId: apiWorkloadIdentity.properties.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
      {
        tenantId: tenant().tenantId
        objectId: functionWorkloadIdentity.properties.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

resource postgresAdministratorPasswordSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'Postgres--AdministratorPassword'
  properties: {
    value: postgresAdministratorPassword
  }
}

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2024-08-01' = {
  name: postgresServerName
  location: location
  sku: {
    name: 'Standard_B2s'
    tier: 'Burstable'
  }
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    administratorLogin: postgresAdministratorLogin
    administratorLoginPassword: postgresAdministratorPassword
    version: '17'
    createMode: 'Create'
    storage: {
      storageSizeGB: 32
    }
    backup: {
      backupRetentionDays: 15
      geoRedundantBackup: 'Disabled'
    }
    highAvailability: {
      mode: 'Disabled'
    }
    network: {
      publicNetworkAccess: 'Enabled'
    }
  }
}

// Needed for AKS and Functions during the public-network, Contributor-only demo phase.
// Replace with private networking before treating this as a production security boundary.
resource postgresAllowAzureServices 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2024-08-01' = {
  parent: postgresServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource businessDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgresServer
  name: 'speedclaim'
  properties: {}
}

resource aiDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2024-08-01' = {
  parent: postgresServer
  name: 'speedclaim_ai'
  properties: {}
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: serviceBusNamespaceName
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false // Contributor-only deployment uses scoped SAS policies.
    minimumTlsVersion: '1.2'
  }
}

resource emailQueue 'Microsoft.ServiceBus/namespaces/queues@2024-01-01' = {
  parent: serviceBusNamespace
  name: emailQueueName
  properties: {
    lockDuration: 'PT1M'
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
    defaultMessageTimeToLive: 'P14D'
    enableBatchedOperations: true
  }
}

resource apiEmailSenderRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' = {
  parent: serviceBusNamespace
  name: 'api-email-sender'
  properties: {
    rights: [
      'Send'
    ]
  }
}

resource functionEmailListenerRule 'Microsoft.ServiceBus/namespaces/authorizationRules@2024-01-01' = {
  parent: serviceBusNamespace
  name: 'function-email-listener'
  properties: {
    rights: [
      'Listen'
    ]
  }
}

resource aksCluster 'Microsoft.ContainerService/managedClusters@2024-09-01' = {
  name: aksName
  location: location
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Base'
    tier: 'Free'
  }
  properties: {
    dnsPrefix: take('${namePrefix}-${uniqueSuffix}', 54)
    enableRBAC: true
    agentPoolProfiles: [
      {
        name: 'system'
        count: 1
        vmSize: 'Standard_B2s_v2'
        osType: 'Linux'
        mode: 'System'
        type: 'VirtualMachineScaleSets'
        osDiskSizeGB: 64
      }
    ]
    oidcIssuerProfile: {
      enabled: true
    }
    securityProfile: {
      workloadIdentity: {
        enabled: true
      }
    }
    networkProfile: {
      networkPlugin: 'azure'
      networkPolicy: 'azure'
      loadBalancerSku: 'standard'
    }
  }
}

resource apiFederatedCredential 'Microsoft.ManagedIdentity/userAssignedIdentities/federatedIdentityCredentials@2023-01-31' = {
  parent: apiWorkloadIdentity
  name: 'aks-speedclaim-api'
  properties: {
    audiences: [
      'api://AzureADTokenExchange'
    ]
    issuer: aksCluster.properties.oidcIssuerProfile.issuerURL
    subject: 'system:serviceaccount:${apiNamespace}:speedclaim-api'
  }
}

// Elastic Premium avoids the Linux Consumption/.NET 10 restriction and needs no Azure RBAC role assignments.
resource functionPlan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: functionPlanName
  location: location
  kind: 'elastic'
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource emailFunctionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: emailFunctionAppName
  location: location
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
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'ServiceBusConnection'
          value: functionEmailListenerRule.listKeys().primaryConnectionString
        }
        {
          name: 'EmailDelivery__QueueName'
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

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: staticWebAppName
  location: staticWebAppLocation
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  tags: {
    application: 'SpeedClaim'
    environment: 'production'
    managedBy: 'Bicep'
  }
  properties: {
    provider: 'GitHub'
  }
}

output staticWebAppName string = staticWebApp.name
output staticWebAppHostname string = staticWebApp.properties.defaultHostname
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
output aksName string = aksCluster.name
output aksFqdn string = aksCluster.properties.fqdn
output postgresServerName string = postgresServer.name
output postgresHost string = postgresServer.properties.fullyQualifiedDomainName
output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output blobStorageAccountName string = storageAccount.name
output serviceBusNamespaceName string = serviceBusNamespace.name
output emailQueueName string = emailQueue.name
output emailFunctionAppName string = emailFunctionApp.name
