--/*
--=============================================
--Demo Data Insert Script for Resource Grid Solution (IDEMPOTENT)
--=============================================
--This script creates comprehensive demo data to showcase:
--1. Search functionality (OR-based across multiple fields)
--2. Tag-based filtering with priority
--3. Sorting capabilities
--4. Pagination functionality
--5. Resource type categorization
--6. Link and Text content types

--IDEMPOTENT: Safe to run multiple times - will not create duplicates
--=============================================
--*/

--BEGIN TRANSACTION;

--BEGIN TRY

--    -- =============================================
--    -- 1. INSERT TAG CONTENT TYPES (IDEMPOTENT)
--    -- =============================================
    
--    MERGE [HTResourceMapper].[TagContentType] AS target
--    USING (VALUES 
--        (1, 'Text'),
--        (2, 'Link')
--    ) AS source (TagContentTypeId, TagCode)
--    ON target.TagContentTypeId = source.TagContentTypeId
--    WHEN NOT MATCHED THEN
--        INSERT (TagContentTypeId, TagCode)
--        VALUES (source.TagContentTypeId, source.TagCode);

--    -- =============================================
--    -- 2. INSERT TAG VALUE TYPES (IDEMPOTENT)
--    -- =============================================
    
--    MERGE [HTResourceMapper].[TagValueType] AS target
--    USING (VALUES 
--        (1, 'Required', 1),
--        (2, 'Optional', 0)
--    ) AS source (TagValueTypeId, Code, IsRequired)
--    ON target.TagValueTypeId = source.TagValueTypeId
--    WHEN NOT MATCHED THEN
--        INSERT (TagValueTypeId, Code, IsRequired)
--        VALUES (source.TagValueTypeId, source.Code, source.IsRequired);

--    -- =============================================
--    -- 3. INSERT RESOURCE TYPES (IDEMPOTENT)
--    -- =============================================
    
--    MERGE [HTResourceMapper].[ResourceType] AS target
--    USING (VALUES 
--        ('RT-DATABASE-001', 'Database', 1, GETUTCDATE()),
--        ('RT-WEBSERVICE-001', 'WebService', 1, GETUTCDATE()),
--        ('RT-GATEWAY-001', 'Gateway', 1, GETUTCDATE()),
--        ('RT-STORAGE-001', 'Storage', 1, GETUTCDATE()),
--        ('RT-CACHE-001', 'Cache', 1, GETUTCDATE())
--    ) AS source (ResourceTypeUid, TypeName, AllowCustomTags, CreatedOn)
--    ON target.ResourceTypeUid = source.ResourceTypeUid
--    WHEN NOT MATCHED THEN
--        INSERT (ResourceTypeUid, TypeName, AllowCustomTags, CreatedOn)
--        VALUES (source.ResourceTypeUid, source.TypeName, source.AllowCustomTags, source.CreatedOn);

--    -- =============================================
--    -- 4. INSERT TAG DEFINITIONS (IDEMPOTENT)
--    -- =============================================
    
--    MERGE [HTResourceMapper].[TagDefinition] AS target
--    USING (VALUES 
--        -- Environment tags
--        ('TD-ENV-001', 'environment', 1, 0, 0, 'dev,test,staging,prod', 1, GETUTCDATE()),
--        ('TD-REGION-001', 'region', 1, 0, 0, 'us-east,us-west,eu-west,asia-pacific', 1, GETUTCDATE()),
--        ('TD-OWNER-001', 'owner', 1, 1, 0, NULL, 0, GETUTCDATE()),
--        ('TD-COST-001', 'cost-center', 1, 1, 0, NULL, 0, GETUTCDATE()),
        
--        -- Technical tags
--        ('TD-VERSION-001', 'version', 1, 1, 0, NULL, 0, GETUTCDATE()),
--        ('TD-RUNTIME-001', 'runtime', 1, 0, 0, 'dotnet,java,python,nodejs', 0, GETUTCDATE()),
--        ('TD-PROTOCOL-001', 'protocol', 1, 0, 0, 'https,http,tcp,grpc', 0, GETUTCDATE()),
        
--        -- Link tags  
--        ('TD-PORTAL-001', 'portal', 2, 1, 0, NULL, 0, GETUTCDATE()),
--        ('TD-DOCS-001', 'documentation', 2, 1, 0, NULL, 0, GETUTCDATE()),
--        ('TD-MONITOR-001', 'monitoring', 2, 1, 0, NULL, 0, GETUTCDATE()),
        
--        -- Business tags
--        ('TD-PROJECT-001', 'project', 1, 1, 0, NULL, 0, GETUTCDATE()),
--        ('TD-CRITICALITY-001', 'criticality', 1, 0, 0, 'low,medium,high,critical', 0, GETUTCDATE())
--    ) AS source (TagDefinitionUid, TagDefinitionKey, TagContentTypeId, AllowCustomValue, IsMultiValued, AllowedValues, IsSystemTag, CreatedOn)
--    ON target.TagDefinitionUid = source.TagDefinitionUid
--    WHEN NOT MATCHED THEN
--        INSERT (TagDefinitionUid, TagDefinitionKey, TagContentTypeId, AllowCustomValue, IsMultiValued, AllowedValues, IsSystemTag, CreatedOn)
--        VALUES (source.TagDefinitionUid, source.TagDefinitionKey, source.TagContentTypeId, source.AllowCustomValue, source.IsMultiValued, source.AllowedValues, source.IsSystemTag, source.CreatedOn);

--    -- =============================================
--    -- 5. INSERT DEMO RESOURCES (IDEMPOTENT)
--    -- =============================================
    
--    DECLARE @DatabaseTypeId INT = (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = 'Database');
--    DECLARE @WebServiceTypeId INT = (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = 'WebService');
--    DECLARE @GatewayTypeId INT = (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = 'Gateway');
--    DECLARE @StorageTypeId INT = (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = 'Storage');
--    DECLARE @CacheTypeId INT = (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = 'Cache');

--    MERGE [HTResourceMapper].[Resource] AS target
--    USING (VALUES 
--        -- Production Databases
--        ('RES-DB-PROD-001', 'prod-customer-db', @DatabaseTypeId, 'Production Customer Database', 'Main production database containing customer data and transactions', DATEADD(day, -90, GETUTCDATE()), DATEADD(hour, -2, GETUTCDATE())),
--        ('RES-DB-PROD-002', 'prod-inventory-db', @DatabaseTypeId, 'Production Inventory Database', 'Production database managing product inventory and warehouse data', DATEADD(day, -75, GETUTCDATE()), DATEADD(hour, -6, GETUTCDATE())),
--        ('RES-DB-PROD-003', 'prod-analytics-db', @DatabaseTypeId, 'Production Analytics Database', 'Data warehouse for business intelligence and reporting', DATEADD(day, -60, GETUTCDATE()), DATEADD(day, -1, GETUTCDATE())),
        
--        -- Staging/Test Databases  
--        ('RES-DB-STAGE-001', 'staging-customer-db', @DatabaseTypeId, 'Staging Customer Database', 'Staging environment database for customer data testing', DATEADD(day, -45, GETUTCDATE()), DATEADD(hour, -12, GETUTCDATE())),
--        ('RES-DB-TEST-001', 'test-integration-db', @DatabaseTypeId, 'Test Integration Database', 'Integration testing database for automated test suites', DATEADD(day, -30, GETUTCDATE()), DATEADD(hour, -4, GETUTCDATE())),
        
--        -- Web Services
--        ('RES-WS-PROD-001', 'customer-api-prod', @WebServiceTypeId, 'Customer API Service', 'REST API for customer management operations', DATEADD(day, -80, GETUTCDATE()), DATEADD(hour, -1, GETUTCDATE())),
--        ('RES-WS-PROD-002', 'payment-service-prod', @WebServiceTypeId, 'Payment Processing Service', 'Microservice handling payment transactions and validation', DATEADD(day, -70, GETUTCDATE()), DATEADD(hour, -3, GETUTCDATE())),
--        ('RES-WS-PROD-003', 'notification-service', @WebServiceTypeId, 'Notification Service', 'Service managing email and SMS notifications', DATEADD(day, -55, GETUTCDATE()), DATEADD(hour, -8, GETUTCDATE())),
--        ('RES-WS-STAGE-001', 'customer-api-staging', @WebServiceTypeId, 'Customer API Staging', 'Staging environment for customer API testing', DATEADD(day, -40, GETUTCDATE()), DATEADD(hour, -5, GETUTCDATE())),
        
--        -- Gateways
--        ('RES-GW-PROD-001', 'api-gateway-prod', @GatewayTypeId, 'Production API Gateway', 'Main API gateway routing external requests', DATEADD(day, -85, GETUTCDATE()), DATEADD(hour, -2, GETUTCDATE())),
--        ('RES-GW-PROD-002', 'internal-gateway-prod', @GatewayTypeId, 'Internal API Gateway', 'Gateway for internal service-to-service communication', DATEADD(day, -65, GETUTCDATE()), DATEADD(hour, -7, GETUTCDATE())),
        
--        -- Storage
--        ('RES-ST-PROD-001', 'blob-storage-prod', @StorageTypeId, 'Production Blob Storage', 'Main blob storage for application files and media', DATEADD(day, -95, GETUTCDATE()), DATEADD(day, -2, GETUTCDATE())),
--        ('RES-ST-PROD-002', 'backup-storage-prod', @StorageTypeId, 'Production Backup Storage', 'Long-term backup storage for disaster recovery', DATEADD(day, -100, GETUTCDATE()), DATEADD(day, -7, GETUTCDATE())),
        
--        -- Cache
--        ('RES-CACHE-PROD-001', 'redis-cache-prod', @CacheTypeId, 'Production Redis Cache', 'Main Redis cache cluster for session and data caching', DATEADD(day, -50, GETUTCDATE()), DATEADD(hour, -4, GETUTCDATE())),
--        ('RES-CACHE-STAGE-001', 'redis-cache-staging', @CacheTypeId, 'Staging Redis Cache', 'Staging environment Redis cache for testing', DATEADD(day, -35, GETUTCDATE()), DATEADD(hour, -10, GETUTCDATE()))
--    ) AS source (ResourceUid, ResourceKey, ResourceTypeId, ResourceName, Description, CreatedOn, UpdatedOn)
--    ON target.ResourceUid = source.ResourceUid
--    WHEN NOT MATCHED THEN
--        INSERT (ResourceUid, ResourceKey, ResourceTypeId, ResourceName, Description, CreatedOn, UpdatedOn)
--        VALUES (source.ResourceUid, source.ResourceKey, source.ResourceTypeId, source.ResourceName, source.Description, source.CreatedOn, source.UpdatedOn)
--    WHEN MATCHED THEN
--        UPDATE SET 
--            ResourceName = source.ResourceName,
--            Description = source.Description,
--            UpdatedOn = source.UpdatedOn;

--    -- =============================================
--    -- 6. INSERT RESOURCE TAGS (IDEMPOTENT)
--    -- =============================================
    
--    -- Get Tag Definition IDs
--    DECLARE @EnvTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'environment');
--    DECLARE @RegionTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'region');
--    DECLARE @OwnerTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'owner');
--    DECLARE @CostTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'cost-center');
--    DECLARE @VersionTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'version');
--    DECLARE @RuntimeTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'runtime');
--    DECLARE @ProtocolTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'protocol');
--    DECLARE @PortalTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'portal');
--    DECLARE @DocsTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'documentation');
--    DECLARE @MonitorTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'monitoring');
--    DECLARE @ProjectTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'project');
--    DECLARE @CriticalityTagId INT = (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionKey = 'criticality');

--    -- Get Resource IDs
--    DECLARE @ProdCustomerDbId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'prod-customer-db');
--    DECLARE @ProdInventoryDbId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'prod-inventory-db');
--    DECLARE @ProdAnalyticsDbId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'prod-analytics-db');
--    DECLARE @StagingCustomerDbId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'staging-customer-db');
--    DECLARE @TestIntegrationDbId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'test-integration-db');
--    DECLARE @CustomerApiProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'customer-api-prod');
--    DECLARE @PaymentServiceProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'payment-service-prod');
--    DECLARE @NotificationServiceId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'notification-service');
--    DECLARE @CustomerApiStagingId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'customer-api-staging');
--    DECLARE @ApiGatewayProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'api-gateway-prod');
--    DECLARE @InternalGatewayProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'internal-gateway-prod');
--    DECLARE @BlobStorageProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'blob-storage-prod');
--    DECLARE @BackupStorageProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'backup-storage-prod');
--    DECLARE @RedisCacheProdId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'redis-cache-prod');
--    DECLARE @RedisCacheStagingId INT = (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceKey = 'redis-cache-staging');

--    -- Use MERGE for all resource tags to make them idempotent
--    MERGE [HTResourceMapper].[ResourceTag] AS target
--    USING (VALUES 
--        -- Production Customer Database tags
--        (@ProdCustomerDbId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@ProdCustomerDbId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@ProdCustomerDbId, @OwnerTagId, 'data-team', GETUTCDATE()),
--        (@ProdCustomerDbId, @CostTagId, 'CC-100', GETUTCDATE()),
--        (@ProdCustomerDbId, @ProjectTagId, 'customer-management', GETUTCDATE()),
--        (@ProdCustomerDbId, @CriticalityTagId, 'critical', GETUTCDATE()),
--        (@ProdCustomerDbId, @PortalTagId, 'https://portal.azure.com/resource/customer-db-prod', GETUTCDATE()),
--        (@ProdCustomerDbId, @MonitorTagId, 'https://monitor.company.com/database/customer-prod', GETUTCDATE()),

--        -- Production Inventory Database tags
--        (@ProdInventoryDbId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@ProdInventoryDbId, @RegionTagId, 'us-west', GETUTCDATE()),
--        (@ProdInventoryDbId, @OwnerTagId, 'inventory-team', GETUTCDATE()),
--        (@ProdInventoryDbId, @CostTagId, 'CC-200', GETUTCDATE()),
--        (@ProdInventoryDbId, @ProjectTagId, 'supply-chain', GETUTCDATE()),
--        (@ProdInventoryDbId, @CriticalityTagId, 'high', GETUTCDATE()),
--        (@ProdInventoryDbId, @PortalTagId, 'https://portal.azure.com/resource/inventory-db-prod', GETUTCDATE()),

--        -- Production Analytics Database tags
--        (@ProdAnalyticsDbId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@ProdAnalyticsDbId, @RegionTagId, 'eu-west', GETUTCDATE()),
--        (@ProdAnalyticsDbId, @OwnerTagId, 'analytics-team', GETUTCDATE()),
--        (@ProdAnalyticsDbId, @CostTagId, 'CC-300', GETUTCDATE()),
--        (@ProdAnalyticsDbId, @ProjectTagId, 'business-intelligence', GETUTCDATE()),
--        (@ProdAnalyticsDbId, @CriticalityTagId, 'medium', GETUTCDATE()),
--        (@ProdAnalyticsDbId, @DocsTagId, 'https://docs.company.com/analytics-db', GETUTCDATE()),

--        -- Staging Customer Database tags
--        (@StagingCustomerDbId, @EnvTagId, 'staging', GETUTCDATE()),
--        (@StagingCustomerDbId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@StagingCustomerDbId, @OwnerTagId, 'data-team', GETUTCDATE()),
--        (@StagingCustomerDbId, @CostTagId, 'CC-100', GETUTCDATE()),
--        (@StagingCustomerDbId, @ProjectTagId, 'customer-management', GETUTCDATE()),
--        (@StagingCustomerDbId, @CriticalityTagId, 'medium', GETUTCDATE()),

--        -- Test Integration Database tags
--        (@TestIntegrationDbId, @EnvTagId, 'test', GETUTCDATE()),
--        (@TestIntegrationDbId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@TestIntegrationDbId, @OwnerTagId, 'qa-team', GETUTCDATE()),
--        (@TestIntegrationDbId, @CostTagId, 'CC-400', GETUTCDATE()),
--        (@TestIntegrationDbId, @ProjectTagId, 'integration-testing', GETUTCDATE()),
--        (@TestIntegrationDbId, @CriticalityTagId, 'low', GETUTCDATE()),

--        -- Customer API Production tags
--        (@CustomerApiProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@CustomerApiProdId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@CustomerApiProdId, @OwnerTagId, 'api-team', GETUTCDATE()),
--        (@CustomerApiProdId, @CostTagId, 'CC-100', GETUTCDATE()),
--        (@CustomerApiProdId, @RuntimeTagId, 'dotnet', GETUTCDATE()),
--        (@CustomerApiProdId, @ProtocolTagId, 'https', GETUTCDATE()),
--        (@CustomerApiProdId, @OwnerTagId, 'api-team', GETUTCDATE()),
--        (@CustomerApiProdId, @CostTagId, 'CC-100', GETUTCDATE()),
--        (@CustomerApiProdId, @RuntimeTagId, 'dotnet', GETUTCDATE()),
--        (@CustomerApiProdId, @ProtocolTagId, 'https', GETUTCDATE()),
--        (@CustomerApiProdId, @VersionTagId, '2.1.5', GETUTCDATE()),
--        (@CustomerApiProdId, @ProjectTagId, 'customer-management', GETUTCDATE()),
--        (@CustomerApiProdId, @CriticalityTagId, 'critical', GETUTCDATE()),
--        (@CustomerApiProdId, @PortalTagId, 'https://portal.azure.com/resource/customer-api-prod', GETUTCDATE()),
--        (@CustomerApiProdId, @DocsTagId, 'https://docs.company.com/customer-api', GETUTCDATE()),

--        -- Payment Service Production tags
--        (@PaymentServiceProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@PaymentServiceProdId, @RegionTagId, 'us-west', GETUTCDATE()),
--        (@PaymentServiceProdId, @OwnerTagId, 'payments-team', GETUTCDATE()),
--        (@PaymentServiceProdId, @CostTagId, 'CC-500', GETUTCDATE()),
--        (@PaymentServiceProdId, @RuntimeTagId, 'java', GETUTCDATE()),
--        (@PaymentServiceProdId, @ProtocolTagId, 'https', GETUTCDATE()),
--        (@PaymentServiceProdId, @VersionTagId, '1.8.2', GETUTCDATE()),
--        (@PaymentServiceProdId, @ProjectTagId, 'payment-processing', GETUTCDATE()),
--        (@PaymentServiceProdId, @CriticalityTagId, 'critical', GETUTCDATE()),
--        (@PaymentServiceProdId, @MonitorTagId, 'https://monitor.company.com/payments', GETUTCDATE()),

--        -- Notification Service tags
--        (@NotificationServiceId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@NotificationServiceId, @RegionTagId, 'asia-pacific', GETUTCDATE()),
--        (@NotificationServiceId, @OwnerTagId, 'platform-team', GETUTCDATE()),
--        (@NotificationServiceId, @CostTagId, 'CC-600', GETUTCDATE()),
--        (@NotificationServiceId, @RuntimeTagId, 'nodejs', GETUTCDATE()),
--        (@NotificationServiceId, @ProtocolTagId, 'https', GETUTCDATE()),
--        (@NotificationServiceId, @VersionTagId, '3.2.1', GETUTCDATE()),
--        (@NotificationServiceId, @ProjectTagId, 'notifications', GETUTCDATE()),
--        (@NotificationServiceId, @CriticalityTagId, 'high', GETUTCDATE()),

--        -- Customer API Staging tags
--        (@CustomerApiStagingId, @EnvTagId, 'staging', GETUTCDATE()),
--        (@CustomerApiStagingId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@CustomerApiStagingId, @OwnerTagId, 'api-team', GETUTCDATE()),
--        (@CustomerApiStagingId, @CostTagId, 'CC-100', GETUTCDATE()),
--        (@CustomerApiStagingId, @RuntimeTagId, 'dotnet', GETUTCDATE()),
--        (@CustomerApiStagingId, @ProtocolTagId, 'https', GETUTCDATE()),
--        (@CustomerApiStagingId, @VersionTagId, '2.2.0-beta', GETUTCDATE()),
--        (@CustomerApiStagingId, @ProjectTagId, 'customer-management', GETUTCDATE()),
--        (@CustomerApiStagingId, @CriticalityTagId, 'medium', GETUTCDATE()),

--        -- API Gateway Production tags
--        (@ApiGatewayProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@ApiGatewayProdId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@ApiGatewayProdId, @OwnerTagId, 'platform-team', GETUTCDATE()),
--        (@ApiGatewayProdId, @CostTagId, 'CC-700', GETUTCDATE()),
--        (@ApiGatewayProdId, @ProtocolTagId, 'https', GETUTCDATE()),
--        (@ApiGatewayProdId, @VersionTagId, '4.1.0', GETUTCDATE()),
--        (@ApiGatewayProdId, @ProjectTagId, 'infrastructure', GETUTCDATE()),
--        (@ApiGatewayProdId, @CriticalityTagId, 'critical', GETUTCDATE()),
--        (@ApiGatewayProdId, @MonitorTagId, 'https://monitor.company.com/gateway', GETUTCDATE()),

--        -- Internal Gateway Production tags
--        (@InternalGatewayProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@InternalGatewayProdId, @RegionTagId, 'us-west', GETUTCDATE()),
--        (@InternalGatewayProdId, @OwnerTagId, 'platform-team', GETUTCDATE()),
--        (@InternalGatewayProdId, @CostTagId, 'CC-700', GETUTCDATE()),
--        (@InternalGatewayProdId, @ProtocolTagId, 'grpc', GETUTCDATE()),
--        (@InternalGatewayProdId, @VersionTagId, '3.5.2', GETUTCDATE()),
--        (@InternalGatewayProdId, @ProjectTagId, 'infrastructure', GETUTCDATE()),
--        (@InternalGatewayProdId, @CriticalityTagId, 'high', GETUTCDATE()),

--        -- Blob Storage Production tags
--        (@BlobStorageProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@BlobStorageProdId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@BlobStorageProdId, @OwnerTagId, 'storage-team', GETUTCDATE()),
--        (@BlobStorageProdId, @CostTagId, 'CC-800', GETUTCDATE()),
--        (@BlobStorageProdId, @ProjectTagId, 'file-storage', GETUTCDATE()),
--        (@BlobStorageProdId, @CriticalityTagId, 'high', GETUTCDATE()),
--        (@BlobStorageProdId, @PortalTagId, 'https://portal.azure.com/resource/blob-storage-prod', GETUTCDATE()),

--        -- Backup Storage Production tags
--        (@BackupStorageProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@BackupStorageProdId, @RegionTagId, 'eu-west', GETUTCDATE()),
--        (@BackupStorageProdId, @OwnerTagId, 'backup-team', GETUTCDATE()),
--        (@BackupStorageProdId, @CostTagId, 'CC-900', GETUTCDATE()),
--        (@BackupStorageProdId, @ProjectTagId, 'disaster-recovery', GETUTCDATE()),
--        (@BackupStorageProdId, @CriticalityTagId, 'critical', GETUTCDATE()),

--        -- Redis Cache Production tags
--        (@RedisCacheProdId, @EnvTagId, 'prod', GETUTCDATE()),
--        (@RedisCacheProdId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@RedisCacheProdId, @OwnerTagId, 'platform-team', GETUTCDATE()),
--        (@RedisCacheProdId, @CostTagId, 'CC-700', GETUTCDATE()),
--        (@RedisCacheProdId, @VersionTagId, '6.2.1', GETUTCDATE()),
--        (@RedisCacheProdId, @ProjectTagId, 'caching', GETUTCDATE()),
--        (@RedisCacheProdId, @CriticalityTagId, 'high', GETUTCDATE()),
--        (@RedisCacheProdId, @MonitorTagId, 'https://monitor.company.com/redis', GETUTCDATE()),

--        -- Redis Cache Staging tags
--        (@RedisCacheStagingId, @EnvTagId, 'staging', GETUTCDATE()),
--        (@RedisCacheStagingId, @RegionTagId, 'us-east', GETUTCDATE()),
--        (@RedisCacheStagingId, @OwnerTagId, 'platform-team', GETUTCDATE()),
--        (@RedisCacheStagingId, @CostTagId, 'CC-700', GETUTCDATE()),
--        (@RedisCacheStagingId, @VersionTagId, '6.2.2-beta', GETUTCDATE()),
--        (@RedisCacheStagingId, @ProjectTagId, 'caching', GETUTCDATE()),
--        (@RedisCacheStagingId, @CriticalityTagId, 'medium', GETUTCDATE())
--    ) AS source (ResourceId, TagDefinitionId, TagValue, CreatedOn)
--    ON target.ResourceId = source.ResourceId AND target.TagDefinitionId = source.TagDefinitionId
--    WHEN NOT MATCHED THEN
--        INSERT (ResourceId, TagDefinitionId, TagValue, CreatedOn)
--        VALUES (source.ResourceId, source.TagDefinitionId, source.TagValue, source.CreatedOn)
--    WHEN MATCHED THEN
--        UPDATE SET TagValue = source.TagValue;

--    -- =============================================
--    -- 7. INSERT RESOURCE TYPE TAGS (IDEMPOTENT)
--    -- =============================================
--    PRINT 'Inserting Resource Type Tags...';
    
--    MERGE [HTResourceMapper].[ResourceTypeTag] AS target
--    USING (VALUES 
--        (@DatabaseTypeId, 'environment', 1),
--        (@DatabaseTypeId, 'region', 1),
--        (@DatabaseTypeId, 'owner', 1),
--        (@WebServiceTypeId, 'environment', 1),
--        (@WebServiceTypeId, 'runtime', 1),
--        (@WebServiceTypeId, 'version', 2),
--        (@GatewayTypeId, 'environment', 1),
--        (@GatewayTypeId, 'protocol', 1),
--        (@StorageTypeId, 'environment', 1),
--        (@StorageTypeId, 'region', 1),
--        (@CacheTypeId, 'environment', 1),
--        (@CacheTypeId, 'version', 2)
--    ) AS source (ResourceTypeId, Tag, TagValueTypeId)
--    ON target.ResourceTypeId = source.ResourceTypeId AND target.Tag = source.Tag
--    WHEN NOT MATCHED THEN
--        INSERT (ResourceTypeId, Tag, TagValueTypeId)
--        VALUES (source.ResourceTypeId, source.Tag, source.TagValueTypeId);

--    COMMIT TRANSACTION;
    
--    PRINT 'Demo data inserted successfully!';
--    PRINT '==============================================';
--    PRINT 'Demo Data Summary:';
--    PRINT '- 5 Resource Types';
--    PRINT '- 12 Tag Definitions (Text and Link types)';
--    PRINT '- 15 Resources across different environments';
--    PRINT '- 100+ Resource Tags demonstrating search scenarios';
--    PRINT '==============================================';
--    PRINT 'Test Search Examples:';
--    PRINT '- Search "prod" - should return 11 production resources';
--    PRINT '- Search "database" - should return 5 database resources';
--    PRINT '- Search "customer" - should return 4 customer-related resources';
--    PRINT '- Search "api" - should return 3 API services';
--    PRINT '- Search "dotnet" - should return 2 .NET services';
--    PRINT '==============================================';
--    PRINT 'IDEMPOTENT: This script can be run multiple times safely';
--    PRINT '==============================================';

--END TRY
--BEGIN CATCH
--    ROLLBACK TRANSACTION;
    
--    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
--    DECLARE @ErrorLine INT = ERROR_LINE();
    
--    RAISERROR('Error inserting demo data at line %d: %s', 16, 1, @ErrorLine, @ErrorMessage);
--END CATCH;