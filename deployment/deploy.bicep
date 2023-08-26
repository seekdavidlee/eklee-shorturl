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
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${appId}': {}
    }
  }
  properties: {
    httpsOnly: true
    serverFarmId: funcappplan.id
    clientAffinityEnabled: true
    siteConfig: {
      functionAppScaleLimit: 2 // prevent unexpected cost
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
