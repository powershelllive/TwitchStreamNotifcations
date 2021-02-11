param systemName string {
  metadata: {
      description: 'Name that will be common for all resources created.'
  }
}

param storageAccountSku string {
  default: 'Standard_LRS'
  allowed: [
      'Standard_LRS'
      'Standard_GRS'
      'Standard_RAGRS'
      'Standard_ZRS'
      'Premium_LRS'
      'Premium_ZRS'
      'Standard_GZRS'
      'Standard_RAGZRS'
  ]
  metadata: {
      description: 'Storage account SKU'
  }
}

param runtime string {
  default: 'powershell'
  allowed: [
      'powershell'
      'dotnet'
  ]
}

param appSettings object {
  default: {}
  metadata: {
      description: 'Key-Value pairs representing custom app settings'
  }
}

param location string {
  default: resourceGroup().location
  metadata: {
      description: 'Location for all resources created'
  }
}

var functionAppName             = systemName 
var hostingPlanName             = '${systemName}-plan'
var logAnalyticsName            = '${systemName}-log'
var applicationInsightsName     = '${systemName}-appin'
var systemNameNoDash            = replace(systemName,'-','')
var uniqueStringRg              = uniqueString(resourceGroup().id)
var storageAccountName          = toLower('${take(systemNameNoDash,17)}${take(uniqueStringRg,5)}sa')
var keyVaultName                = '${take(systemNameNoDash,17)}${take(uniqueStringRg,5)}kv'
var storageConnectionStringName = '${systemName}-connectionstring'
var runtimeSettingsTable        = {
  powershell: {
      FUNCTIONS_WORKER_RUNTIME: 'powershell'
      FUNCTIONS_WORKER_RUNTIME_VERSION: '~7'
      FUNCTIONS_WORKER_PROCESS_COUNT: 10
      PSWorkerInProcConcurrencyUpperBound: 1
  }
  dotnet: {
      FUNCTIONS_WORKER_RUNTIME: 'dotnet'
  }
}
var runtimeSettings = runtime == 'powershell' ? runtimeSettingsTable.powershell : runtime == 'dotnet' ? runtimeSettingsTable.dotnet : {}

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2020-03-01-preview' = {
  name: logAnalyticsName
  location: location
  properties: {
      sku: {
          name: 'PerGB2018'
      }
      retentionInDays: 90
  }
}

resource appInsights 'Microsoft.insights/components@2020-02-02-preview' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  tags: {
      'hidden-link:${resourceId('Microsoft.Web/sites',functionAppName)}': 'Resource'
  }
  properties: {
      Application_Type: 'web'
      WorkspaceResourceId: logAnalytics.id
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: storageAccountName
  location: location
  sku: {
      name: storageAccountSku
  }
  kind: 'StorageV2'
  properties: {
      supportsHttpsTrafficOnly: true
      encryption: {
          services: {
              file: {
                  keyType: 'Account'
                  enabled: true
              }
              blob: {
                  keyType: 'Account'
                  enabled: true
              }
          }
          keySource: 'Microsoft.Storage'
      }
  }
}

resource storageAccountBlobService 'Microsoft.Storage/storageAccounts/blobServices@2019-06-01' = {
  name: '${storageAccount.name}/default'
  properties: {
      deleteRetentionPolicy: {
          enabled: false
      }
  }
}

resource storageAccountFileService 'Microsoft.Storage/storageAccounts/fileServices@2019-06-01' = {
  name: '${storageAccount.name}/default'
  properties: {
      shareDeleteRetentionPolicy: {
          enabled: false
      }
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: hostingPlanName
  location: location
  sku: {
      name: 'Y1'
  }
}

resource functionApp 'Microsoft.Web/sites@2020-06-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  identity: {
      type: 'SystemAssigned'
  }
  properties: {
      enabled: true
      httpsOnly: true
      serverFarmId: hostingPlan.id
      siteConfig: {
          ftpsState: 'Disabled'
          minTlsVersion: '1.2'
          powerShellVersion: '~7'
          scmType: 'None'
      }
      containerSize: 1536 // not used any more, but portal complains without it
  }
}

resource funcitonAppDiagLogAnalytics 'Microsoft.Web/sites/providers/diagnosticSettings@2017-05-01-preview' = {
  name: '${functionApp.name}/Microsoft.Insights/logAnalyticsAudit'
  location: location
  properties: {
      workspaceId: logAnalytics.id
      logs: [
          {
              category: 'FunctionAppLogs'
              enabled: true
              retentionPolicy: {
                  enabled: true
                  days: 90
              }
          }
      ]
  }
}

resource keyvault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: keyVaultName
  location: location
  properties: {
      enabledForDeployment: false
      enabledForTemplateDeployment: false
      enabledForDiskEncryption: false
      tenantId: subscription().tenantId
      accessPolicies: [
          {
              // delegate secrets access to function app
              // tenantId: reference(functionApp.id,'2020-06-01','Full').identity.tenantId
              tenantId: functionApp.identity.tenantId
              objectId: functionApp.identity.principalId
              permissions: {
                  secrets: [
                      'get'
                      'list'
                      'set'
                  ]
              }
          }
      ]
      sku: {
          name: 'standard'
          family: 'A'
      }
  }
}

resource keyvaultDiagStorage 'Microsoft.KeyVault/vaults/providers/diagnosticSettings@2017-05-01-preview' = {
  name: '${keyvault.name}/Microsoft.Insights/service'
  location: location
  properties: {
      storageAccountId: storageAccount.id
      logs: [
          {
              category: 'AuditEvent'
              enabled: true
              retentionPolicy: {
                  enabled: false
                  days: 0
              }
          }
      ]
  }
}

resource keyvaultDiagLogAnalytics 'Microsoft.KeyVault/vaults/providers/diagnosticSettings@2017-05-01-preview' = {
  name: '${keyvault.name}/Microsoft.Insights/logAnalyticsAudit'
  location: location
  properties: {
      workspaceId: logAnalytics.id
      logs: [
          {
              category: 'AuditEvent'
              enabled: true
              retentionPolicy: {
                  enabled: true
                  days: 90
              }
          }
      ]
  }
}

resource secretStorageConnectionString 'Microsoft.KeyVault/vaults/secrets@2019-09-01' = {
  name: '${keyvault.name}/${storageConnectionStringName}'
  properties: {
      value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id,'2019-06-01').keys[0].value}'
  }
}

resource functionAppSettings 'Microsoft.Web/sites/config@2020-06-01' = {
  name: '${functionApp.name}/appsettings'
  properties: union(runtimeSettings,appSettings,{
      AzureWebJobsStorage: '@Microsoft.KeyVault(SecretUri=${reference(storageConnectionStringName).secretUriWithVersion})'
      WEBSITE_CONTENTSHARE: toLower(functionApp.name)
      WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: '@Microsoft.KeyVault(SecretUri=${reference(storageConnectionStringName).secretUriWithVersion})'
      APPINSIGHTS_INSTRUMENTATIONKEY: reference(appInsights.id,'2020-02-02-preview').InstrumentationKey
      APPLICATIONINSIGHTS_CONNECTION_STRING: reference(appInsights.id,'2020-02-02-preview').ConnectionString
      FUNCTIONS_EXTENSION_VERSION: '~3'
      AzureWebJobsSecretStorageKeyVaultName: keyvault.name
      AzureWebJobsSecretStorageType: 'keyvault'
      AzureWebJobsSecretStorageKeyVaultConnectionString: ''
      AzureWebJobsDisableHomepage: true
      FUNCTIONS_APP_EDIT_MODE: 'readonly'
      WEBSITE_RUN_FROM_PACKAGE: '1'
  })
}

output FunctionAppName string = functionApp.name
output PrincipalIdRef string = reference(functionApp.id,'2020-06-01','Full').identity.principalId
output PrincipalTenantId string = functionApp.identity.tenantId
output PrincipalId string = functionApp.identity.principalId
output StorageAccountName string = storageAccountName
output KeyVaultName string = keyVaultName