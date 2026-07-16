using '../main.bicep'

param location = 'southindia'
param staticWebAppLocation = 'eastasia'
param resourceGroupName = 'rg-speedclaim-prod'
param namePrefix = 'speedclaim'

// Supply deployerObjectId and postgresAdministratorPassword at deployment time.
