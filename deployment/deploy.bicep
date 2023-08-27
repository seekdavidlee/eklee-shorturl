param prefix string = ''
param appInsightsName string = ''
param appPlanName string = ''
param appName string = ''
param appId string = ''
param appClientId string = ''
param appStorageName string = ''
param appDatabaseName string = ''
param location string = resourceGroup().location
@secure()
param apiKey string
param allowedIPList string
param sharedKeyVaultName string
param appStorageConn string

var appInsightsNameStr = empty(appInsightsName) ? '${prefix}${uniqueString(resourceGroup().name)}' : appInsightsName
var appPlanNameStr = empty(appPlanName) ? '${prefix}${uniqueString(resourceGroup().name)}' : appPlanName
var appNameStr = empty(appName) ? '${prefix}${uniqueString(resourceGroup().name)}' : appName

var appStorageNameStr = empty(appStorageName) ? '${prefix}${uniqueString(resourceGroup().name)}' : appStorageName
var appDatabaseNameStr = empty(appDatabaseName) ? '${prefix}${uniqueString(resourceGroup().name)}' : appDatabaseName

resource appinsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsNameStr
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    ImmediatePurgeDataOn30Days: true
    IngestionMode: 'ApplicationInsights'
  }
}

resource str 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: appStorageNameStr
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    supportsHttpsTrafficOnly: true
  }
}

resource strfile 'Microsoft.Storage/storageAccounts/fileServices@2023-01-01' = {
  name: 'default'
  parent: str
}

resource strfilecontent 'Microsoft.Storage/storageAccounts/fileServices/shares@2023-01-01' = {
  name: 'azurefileshare'
  parent: strfile
}

resource funcappplan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appPlanNameStr
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource funcapp 'Microsoft.Web/sites@2022-09-01' = {
  name: appNameStr
  location: location
  kind: 'functionapp'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${appId}': {}
    }
  }
  properties: {
    keyVaultReferenceIdentity: appId
    httpsOnly: true
    serverFarmId: funcappplan.id
    clientAffinityEnabled: true
    siteConfig: {
      functionAppScaleLimit: 2 // prevent unexpected cost, DoS attack
      webSocketsEnabled: true
      appSettings: [
        {
          name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
          value: appinsights.properties.InstrumentationKey
        }
        {
          name: 'AzureWebJobsStorage__accountName'
          value: str.name
        }
        {
          name: 'AzureWebJobsStorage__credential'
          value: 'managedidentity'
        }
        {
          name: 'AzureWebJobsStorage__clientId'
          value: appClientId
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: empty(appName) ? 'DefaultEndpointsProtocol=https;AccountName=${str.name};AccountKey=${str.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}' : '@Microsoft.KeyVault(VaultName=${sharedKeyVaultName};SecretName=${appStorageConn})'
        }
        {
          name: 'WEBSITE_SKIP_CONTENTSHARE_VALIDATION'
          value: '1'
        }
        {
          name: 'WEBSITE_CONTENTSHARE'
          value: 'azurefileshare'
        }
        {
          name: 'UrlStorageConnection__tableServiceUri'
          value: 'https://${appDatabaseNameStr}.table.${environment().suffixes.storage}/'
        }
        {
          name: 'UrlStorageConnection__credential'
          value: 'managedidentity'
        }
        {
          name: 'UrlStorageConnection__clientId'
          value: appClientId
        }
        {
          name: 'API_KEY'
          value: apiKey
        }
        {
          name: 'ALLOWED_IP_LIST'
          value: allowedIPList
        }
        {
          name: 'SMOKE_TEST'
          value: 'false'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'default'
        }
      ]
    }
  }
}

output funcName string = funcapp.name
output canDeployCode bool = !empty(appName)
