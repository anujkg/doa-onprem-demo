using './main.bicep'

param env = 'prod'
param location = 'eastus'
param acrSku = 'Basic'
param sqlAdminObjectId = '<entra-group-object-id>'
param tenantId = '<azure-tenant-id>'
param clientId = '<entra-app-client-id>'
