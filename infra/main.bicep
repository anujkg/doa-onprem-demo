// DOA Order Management System — Azure Container Apps Infrastructure
// Replaces: On-prem Kubernetes + SQL Server + LDAP + SMTP + SSRS + Harbor + F5
// Target: Azure Container Apps + Azure SQL + Key Vault + ACR + Front Door + ACS

@description('Environment name (prod, staging, dev)')
param env string

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('ACR SKU')
@allowed(['Basic', 'Standard', 'Premium'])
param acrSku string = 'Basic'

@description('Object ID of the Entra ID group/user to be SQL admin')
param sqlAdminObjectId string

@description('Azure Tenant ID')
param tenantId string

@description('Entra ID App Registration client ID')
param clientId string

// ============================================================
// User-Assigned Managed Identity (shared by all resources)
// ============================================================
resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-doa-${env}'
  location: location
}

// ============================================================
// Azure Container Registry (replacing on-prem Harbor)
// ============================================================
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'acrdoa${env}'
  location: location
  sku: {
    name: acrSku
  }
  properties: {
    adminUserEnabled: false
  }
}

// ============================================================
// Log Analytics Workspace (for Container Apps)
// ============================================================
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-doa-${env}'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// ============================================================
// Azure Communication Services (replacing on-prem SMTP)
// ============================================================
resource acs 'Microsoft.Communication/communicationServices@2023-04-01' = {
  name: 'acs-doa-${env}'
  location: 'global'
  properties: {
    dataLocation: 'United States'
  }
}

// ============================================================
// Key Vault (all secrets stored here — no passwords in code)
// ============================================================
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-doa-${env}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true
    enableSoftDelete: true
    softDeleteRetentionInDays: 90
  }
}

// ============================================================
// Azure SQL Server (Entra ID admin, no SQL auth)
// ============================================================
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: 'sql-doa-${env}'
  location: location
  properties: {
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Group'
      login: 'DBA-Admins'
      sid: sqlAdminObjectId
      tenantId: tenantId
      azureADOnlyAuthentication: true
    }
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  parent: sqlServer
  name: 'DOA_Orders'
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource sqlFirewallAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ============================================================
// Container Apps Environment
// ============================================================
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-doa-${env}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ============================================================
// Container App — Web Application
// ============================================================
resource webContainerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-doa-web-${env}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'doa-webapp'
          image: '${acr.properties.loginServer}/doa/webapp:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'ASPNETCORE_ENVIRONMENT'
              value: 'Production'
            }
            {
              name: 'AzureAd__TenantId'
              value: tenantId
            }
            {
              name: 'AzureAd__ClientId'
              value: clientId
            }
            {
              name: 'KeyVault__Endpoint'
              value: keyVault.properties.vaultUri
            }
            {
              name: 'ConnectionStrings__SqlServer'
              value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=DOA_Orders;Authentication=Active Directory Managed Identity;Encrypt=true'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentity.properties.clientId
            }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              initialDelaySeconds: 15
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 5
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '20'
              }
            }
          }
        ]
      }
    }
  }
}

// ============================================================
// Container Apps Job — Batch Processing (replacing K8s CronJob)
// Runs nightly at 2:00 AM (matching original K8s schedule: "0 2 * * *")
// ============================================================
resource batchJob 'Microsoft.App/jobs@2024-03-01' = {
  name: 'caj-doa-batch-${env}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      triggerType: 'Schedule'
      replicaTimeout: 3600
      replicaRetryLimit: 2
      scheduleTriggerConfig: {
        cronExpression: '0 2 * * *'
        parallelism: 1
        replicaCompletionCount: 1
      }
      secrets: [
        {
          name: 'acs-endpoint'
          value: 'https://${acs.name}.communication.azure.com'
        }
        {
          name: 'acs-from-address'
          value: 'DoNotReply@${acs.name}.azurecomm.net'
        }
      ]
      registries: [
        {
          server: acr.properties.loginServer
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'doa-batch'
          image: '${acr.properties.loginServer}/doa/batch-jobs:latest'
          resources: {
            cpu: json('1.0')
            memory: '2Gi'
          }
          env: [
            {
              name: 'DOA_SQL_CONNSTR'
              value: 'Server=${sqlServer.properties.fullyQualifiedDomainName};Database=DOA_Orders;Authentication=Active Directory Managed Identity;Encrypt=true'
            }
            {
              name: 'DOA_BLOB_ACCOUNT_URL'
              value: 'https://stdoa${env}.blob.core.windows.net'
            }
            {
              name: 'DOA_ACS_ENDPOINT'
              secretRef: 'acs-endpoint'
            }
            {
              name: 'DOA_ACS_FROM_ADDRESS'
              secretRef: 'acs-from-address'
            }
            {
              name: 'AZURE_CLIENT_ID'
              value: managedIdentity.properties.clientId
            }
          ]
        }
      ]
    }
  }
}

// ============================================================
// Azure Storage Account — for batch data feeds (replacing NFS)
// ============================================================
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-04-01' = {
  name: 'stdoa${env}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
  }
}

resource dataFeedsContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-04-01' = {
  name: '${storageAccount.name}/default/data-feeds'
  properties: {
    publicAccess: 'None'
  }
}

// ============================================================
// Azure Front Door (replacing F5 load balancer)
// ============================================================
resource frontDoor 'Microsoft.Cdn/profiles@2023-05-01' = {
  name: 'fd-doa-${env}'
  location: 'global'
  sku: {
    name: 'Standard_AzureFrontDoor'
  }
}

resource frontDoorEndpoint 'Microsoft.Cdn/profiles/afdEndpoints@2023-05-01' = {
  parent: frontDoor
  name: 'doa-${env}'
  location: 'global'
  properties: {
    enabledState: 'Enabled'
  }
}

resource frontDoorOriginGroup 'Microsoft.Cdn/profiles/originGroups@2023-05-01' = {
  parent: frontDoor
  name: 'doa-web-origin-group'
  properties: {
    loadBalancingSettings: {
      sampleSize: 4
      successfulSamplesRequired: 3
    }
    healthProbeSettings: {
      probePath: '/healthz'
      probeRequestType: 'GET'
      probeProtocol: 'Https'
      probeIntervalInSeconds: 30
    }
  }
}

resource frontDoorOrigin 'Microsoft.Cdn/profiles/originGroups/origins@2023-05-01' = {
  parent: frontDoorOriginGroup
  name: 'doa-web-app'
  properties: {
    hostName: webContainerApp.properties.configuration.ingress.fqdn
    httpPort: 80
    httpsPort: 443
    originHostHeader: webContainerApp.properties.configuration.ingress.fqdn
    priority: 1
    weight: 1000
    enabledState: 'Enabled'
  }
}

resource frontDoorRoute 'Microsoft.Cdn/profiles/afdEndpoints/routes@2023-05-01' = {
  parent: frontDoorEndpoint
  name: 'doa-route'
  properties: {
    originGroup: {
      id: frontDoorOriginGroup.id
    }
    supportedProtocols: ['Https']
    patternsToMatch: ['/*']
    forwardingProtocol: 'HttpsOnly'
    linkToDefaultDomain: 'Enabled'
    httpsRedirect: 'Enabled'
  }
}

// ============================================================
// Role Assignments — Managed Identity permissions
// ============================================================

// AcrPull — allows Container Apps to pull images from ACR
resource acrPullRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, managedIdentity.id, 'AcrPull')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Key Vault Secrets User — allows apps to read secrets
resource kvSecretsUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentity.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Storage Blob Data Contributor — allows batch jobs to read/write blobs
resource storageBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccount.id, managedIdentity.id, 'StorageBlobDataContributor')
  scope: storageAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
    principalId: managedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ============================================================
// Outputs
// ============================================================
output acrLoginServer string = acr.properties.loginServer
output containerAppFqdn string = webContainerApp.properties.configuration.ingress.fqdn
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output keyVaultUri string = keyVault.properties.vaultUri
output managedIdentityClientId string = managedIdentity.properties.clientId
output frontDoorEndpointFqdn string = frontDoorEndpoint.properties.hostName
