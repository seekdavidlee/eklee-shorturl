param location string = resourceGroup().location
param storageName string = 'su${uniqueString(resourceGroup().name)}'
param appId string = ''
var appIdStr = empty(appId) ? 'su${uniqueString(resourceGroup().name)}' : appId

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource tableServices 'Microsoft.Storage/storageAccounts/tableServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource table 'Microsoft.Storage/storageAccounts/tableServices/tables@2023-01-01' = {
  parent: tableServices
  name: 'urls'
}

resource appid 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: appIdStr
  location: location
}
