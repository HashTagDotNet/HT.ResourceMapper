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
USE [ResourceMapper]
GO

CREATE OR ALTER PROCEDURE #Seed_ResourceType(
	@ResourceTypeId INT,
	@ResourceTypeUid VARCHAR(40),
    @TypeName NVARCHAR(250),
    @AllowCustomTags BIT = 0
)
AS
BEGIN
	UPDATE TOP(1)
		[HTResourceMapper].ResourceType
	SET
		ResourceTypeUid = @ResourceTypeUid,
		TypeName = @TypeName,
		AllowCustomTags = @AllowCustomTags,
		UpdatedOn = SYSUTCDATETIME()
	WHERE
		ResourceTypeId = @ResourceTypeId

	
	IF @@ROWCOUNT = 0
	BEGIN
        SET IDENTITY_INSERT [HTResourceMapper].ResourceType ON
		INSERT INTO [HTResourceMapper].ResourceType (
			ResourceTypeId,
			ResourceTypeUid,
			TypeName,
			AllowCustomTags
		) VALUES (
			@ResourceTypeId,
			@ResourceTypeUid,
			@TypeName,
			@AllowCustomTags
		)
        SET IDENTITY_INSERT [HTResourceMapper].ResourceType OFF
	END
	
END
GO

CREATE OR ALTER PROC #Seed_TagType(
    @TagContentTypeId INT
    ,@TagCode VARCHAR(50)
)
AS
BEGIN
    UPDATE TOP(1)
        [HTResourceMapper].TagContentType
    SET
        TagCode = @TagCode
    WHERE
        TagContentTypeId = @TagContentTypeId
    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [HTResourceMapper].TagContentType (
            TagContentTypeId,
            TagCode
        ) VALUES (
            @TagContentTypeId
            ,@TagCode
        )
    END
END
GO

CREATE OR ALTER PROCEDURE #Seed_Resource(
	@ResourceId INT,
	@ResourceUid VARCHAR(40),
	@ResourceKey NVARCHAR(250),
	@ResourceTypeUid VARCHAR(40),
	@ResourceName NVARCHAR(250),
	@Description NVARCHAR(2000)
)
AS
BEGIN

	UPDATE
		[HTResourceMapper].[Resource]
	SET
			ResourceUid = @ResourceUid,
			ResourceKey = @ResourceKey,
			ResourceTypeId = (SELECT ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE ResourceTypeUid = @ResourceTypeUid),
			ResourceName = @ResourceName,
			Description = @Description,
			UpdatedOn = SYSUTCDATETIME()
	WHERE
			ResourceId = @ResourceId
	IF @@ROWCOUNT = 0
	BEGIN
		SET IDENTITY_INSERT [HTResourceMapper].[Resource] ON
		DECLARE @ResourceTypeId INT
		SELECT @ResourceTypeId= ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE ResourceTypeUid = @ResourceTypeUid
		INSERT INTO [HTResourceMapper].[Resource] (
			ResourceId,
			ResourceUid,
			ResourceKey,
			ResourceTypeId,
			ResourceName,
			Description
		) VALUES (
			@ResourceId,
			@ResourceUid,
			@ResourceKey,
			@ResourceTypeId,
			@ResourceName,
			@Description
		)
		SET IDENTITY_INSERT [HTResourceMapper].[Resource] OFF
	END
	
END
GO

BEGIN TRANSACTION
delete [HTResourceMapper].[Resource]
-- Create demo resource types
DECLARE @ResourceType_Application VARCHAR(40) =  'DEMO-AppUid'
, @ResourceType_AppServices VARCHAR(40) =  'DEMO-AppServiceUid'
, @ResourceType_AzureRedis VARCHAR(40) =  'DEMO-AzureUid'
, @ResourceType_AppInsights VARCHAR(40) =  'DEMO-AppInsightsUid'
, @ResourceType_ResourceGroup VARCHAR(40) =  'DEMO-ResourceGroupUid'
, @ResourceType_AppConfiguration VARCHAR(40) =  'DEMO-AppConfigUid'
, @ResourceType_ServiceBus VARCHAR(40) =  'DEMO-ServicebusUid'
, @ResourceType_ServiceFabric VARCHAR(40) =  'DEMO-ServiceFabric'

EXEC #Seed_ResourceType @ResourceTypeId=-1, @ResourceTypeUid=@ResourceType_Application, @TypeName='Application', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-2, @ResourceTypeUid=@ResourceType_AppServices, @TypeName='Azure App Service', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-3, @ResourceTypeUid=@ResourceType_AzureRedis, @TypeName='Azure Redis', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-4, @ResourceTypeUid=@ResourceType_AppInsights, @TypeName='Azure App Insights', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-5, @ResourceTypeUid=@ResourceType_AppConfiguration, @TypeName='Azure App Config', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-6, @ResourceTypeUid=@ResourceType_ServiceBus, @TypeName='Azure ServiceBus', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-7, @ResourceTypeUid=@ResourceType_ServiceFabric, @TypeName='Azure ServcieFabric', @AllowCustomTags=0
EXEC #Seed_ResourceType @ResourceTypeId=-8, @ResourceTypeUid=@ResourceType_ResourceGroup, @TypeName='Azure ResourceGroup', @AllowCustomTags=0

DECLARE @TagType_Text INT = 0,
@TagType_Link INT = 1,
@TagType_Number INT = 2,
@TagType_Date INT = 3

--EXEC #Seed_TagType @TagContentTypeId=@TagType_Text, @TagCode='Text'
--EXEC #Seed_TagType @TagContentTypeId=@TagType_Link, @TagCode='Link'
--EXEC #Seed_TagType @TagContentTypeId=@TagType_Number, @TagCode='Number'
--EXEC #Seed_TagType @TagContentTypeId=@TagType_Date, @TagCode='Date'

-- Create demo resources
DECLARE @ResourceUid VARCHAR(40)

-- Application Resources (15 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-1, @ResourceUid=@ResourceUid, @ResourceKey='APP001', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Customer Portal', @Description='Main customer-facing web application for order management and tracking.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-2, @ResourceUid=@ResourceUid, @ResourceKey='APP002', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Admin Dashboard', @Description='Administrative interface for system configuration and user management.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-3, @ResourceUid=@ResourceUid, @ResourceKey='APP003', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Mobile API Gateway', @Description='RESTful API gateway for mobile application integrations.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-4, @ResourceUid=@ResourceUid, @ResourceKey='APP004', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Reporting Engine', @Description='Business intelligence and reporting application for analytics and insights.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-5, @ResourceUid=@ResourceUid, @ResourceKey='APP005', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Inventory Management', @Description='Real-time inventory tracking and warehouse management system.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-6, @ResourceUid=@ResourceUid, @ResourceKey='APP006', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Payment Processing', @Description='Secure payment gateway integration and transaction processing system.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-7, @ResourceUid=@ResourceUid, @ResourceKey='APP007', @ResourceTypeUid=@ResourceType_Application, @ResourceName='User Authentication', @Description='Centralized authentication and authorization service for all applications.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-8, @ResourceUid=@ResourceUid, @ResourceKey='APP008', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Notification Service', @Description='Multi-channel notification system for email, SMS, and push notifications.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-9, @ResourceUid=@ResourceUid, @ResourceKey='APP009', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Data Migration Tool', @Description='ETL application for migrating data between legacy and modern systems.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-10, @ResourceUid=@ResourceUid, @ResourceKey='APP010', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Audit Logging', @Description='Centralized audit logging and compliance tracking application.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-11, @ResourceUid=@ResourceUid, @ResourceKey='APP011', @ResourceTypeUid=@ResourceType_Application, @ResourceName='File Upload Service', @Description='Secure file upload and document management service with virus scanning.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-12, @ResourceUid=@ResourceUid, @ResourceKey='APP012', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Configuration Manager', @Description='Dynamic application configuration management and feature flag system.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-13, @ResourceUid=@ResourceUid, @ResourceKey='APP013', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Search Engine', @Description='Elasticsearch-powered search service for product and content discovery.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-14, @ResourceUid=@ResourceUid, @ResourceKey='APP014', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Backup Scheduler', @Description='Automated backup scheduling and monitoring application for critical data.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-15, @ResourceUid=@ResourceUid, @ResourceKey='APP015', @ResourceTypeUid=@ResourceType_Application, @ResourceName='Load Balancer Config', @Description='Application for managing load balancer configurations and health checks.'

-- Azure App Service Resources (12 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-16, @ResourceUid=@ResourceUid, @ResourceKey='AAS001', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Production Web App', @Description='Primary production web application hosting on Azure App Service.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-17, @ResourceUid=@ResourceUid, @ResourceKey='AAS002', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Staging Environment', @Description='Pre-production staging environment for testing and validation.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-18, @ResourceUid=@ResourceUid, @ResourceKey='AAS003', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Development API', @Description='Development environment API service for testing new features.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-19, @ResourceUid=@ResourceUid, @ResourceKey='AAS004', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='QA Testing Environment', @Description='Quality assurance testing environment for automated and manual testing.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-20, @ResourceUid=@ResourceUid, @ResourceKey='AAS005', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Demo Environment', @Description='Customer demonstration environment with sample data and workflows.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-21, @ResourceUid=@ResourceUid, @ResourceKey='AAS006', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='API Documentation', @Description='Interactive API documentation and testing portal hosted on App Service.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-22, @ResourceUid=@ResourceUid, @ResourceKey='AAS007', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Webhook Processor', @Description='Azure App Service for processing incoming webhooks from external systems.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-23, @ResourceUid=@ResourceUid, @ResourceKey='AAS008', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Background Job Runner', @Description='App Service hosting background job processing and scheduled tasks.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-24, @ResourceUid=@ResourceUid, @ResourceKey='AAS009', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='File Processing Service', @Description='Dedicated App Service for processing uploaded files and documents.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-25, @ResourceUid=@ResourceUid, @ResourceKey='AAS010', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Monitoring Dashboard', @Description='Real-time monitoring and metrics dashboard hosted on App Service.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-26, @ResourceUid=@ResourceUid, @ResourceKey='AAS011', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Integration Hub', @Description='Central integration hub for third-party API connections and data sync.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-27, @ResourceUid=@ResourceUid, @ResourceKey='AAS012', @ResourceTypeUid=@ResourceType_AppServices, @ResourceName='Health Check Endpoint', @Description='Dedicated health check and system status monitoring service.'

-- Azure Redis Resources (8 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-28, @ResourceUid=@ResourceUid, @ResourceKey='REDIS001', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Session Cache', @Description='Primary Redis cache for storing user session data and authentication tokens.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-29, @ResourceUid=@ResourceUid, @ResourceKey='REDIS002', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Application Cache', @Description='High-performance cache for frequently accessed application data and queries.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-30, @ResourceUid=@ResourceUid, @ResourceKey='REDIS003', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Shopping Cart Cache', @Description='Dedicated Redis instance for e-commerce shopping cart data persistence.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-31, @ResourceUid=@ResourceUid, @ResourceKey='REDIS004', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Real-time Analytics', @Description='Redis cache for real-time analytics data and dashboard metrics.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-32, @ResourceUid=@ResourceUid, @ResourceKey='REDIS005', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Rate Limiting Cache', @Description='Redis instance for API rate limiting and throttling management.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-33, @ResourceUid=@ResourceUid, @ResourceKey='REDIS006', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Distributed Lock Manager', @Description='Redis-based distributed locking system for coordinating background processes.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-34, @ResourceUid=@ResourceUid, @ResourceKey='REDIS007', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Pub/Sub Message Queue', @Description='Redis pub/sub system for real-time messaging and notifications.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-35, @ResourceUid=@ResourceUid, @ResourceKey='REDIS008', @ResourceTypeUid=@ResourceType_AzureRedis, @ResourceName='Feature Flag Cache', @Description='Redis cache for dynamic feature flags and configuration settings.'

-- Azure App Insights Resources (10 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-36, @ResourceUid=@ResourceUid, @ResourceKey='AI001', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Production Monitoring', @Description='Application Insights for production environment monitoring and telemetry.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-37, @ResourceUid=@ResourceUid, @ResourceKey='AI002', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='API Performance Tracking', @Description='Dedicated App Insights for API performance monitoring and dependency tracking.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-38, @ResourceUid=@ResourceUid, @ResourceKey='AI003', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='User Experience Analytics', @Description='App Insights focused on user behavior analytics and page view tracking.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-39, @ResourceUid=@ResourceUid, @ResourceKey='AI004', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Error Tracking System', @Description='Centralized error tracking and exception monitoring across all applications.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-40, @ResourceUid=@ResourceUid, @ResourceKey='AI005', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Custom Events Tracker', @Description='App Insights for tracking custom business events and conversion metrics.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-41, @ResourceUid=@ResourceUid, @ResourceKey='AI006', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Performance Counter Monitor', @Description='System performance counter monitoring and resource utilization tracking.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-42, @ResourceUid=@ResourceUid, @ResourceKey='AI007', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Database Query Analytics', @Description='Database performance monitoring and slow query detection system.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-43, @ResourceUid=@ResourceUid, @ResourceKey='AI008', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Mobile App Telemetry', @Description='Application Insights for mobile application usage and crash reporting.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-44, @ResourceUid=@ResourceUid, @ResourceKey='AI009', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Security Event Monitor', @Description='Security-focused monitoring for authentication failures and suspicious activities.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-45, @ResourceUid=@ResourceUid, @ResourceKey='AI010', @ResourceTypeUid=@ResourceType_AppInsights, @ResourceName='Availability Test Suite', @Description='Automated availability testing and uptime monitoring for critical services.'

-- Azure App Config Resources (8 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-46, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG001', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Production Settings', @Description='Production environment configuration store with connection strings and API keys.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-47, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG002', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Feature Flag Manager', @Description='Centralized feature flag configuration for A/B testing and gradual rollouts.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-48, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG003', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Development Config', @Description='Development environment configuration store for testing and debugging.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-49, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG004', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Multi-tenant Settings', @Description='Tenant-specific configuration management for multi-tenant applications.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-50, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG005', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Regional Settings', @Description='Region-specific configuration for global application deployment.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-51, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG006', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Security Policies', @Description='Security policy configuration store for authentication and authorization rules.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-52, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG007', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Integration Endpoints', @Description='Third-party integration endpoint configuration and credentials management.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-53, @ResourceUid=@ResourceUid, @ResourceKey='CONFIG008', @ResourceTypeUid=@ResourceType_AppConfiguration, @ResourceName='Performance Thresholds', @Description='Application performance monitoring thresholds and alerting configuration.'

-- Azure ServiceBus Resources (10 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-54, @ResourceUid=@ResourceUid, @ResourceKey='SB001', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Order Processing Queue', @Description='Service Bus queue for processing customer orders and payment transactions.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-55, @ResourceUid=@ResourceUid, @ResourceKey='SB002', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Notification Topic', @Description='Service Bus topic for distributing notifications across multiple subscriber services.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-56, @ResourceUid=@ResourceUid, @ResourceKey='SB003', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Audit Event Stream', @Description='Event streaming for audit log processing and compliance reporting.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-57, @ResourceUid=@ResourceUid, @ResourceKey='SB004', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='File Upload Queue', @Description='Queue for processing uploaded files and document conversion tasks.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-58, @ResourceUid=@ResourceUid, @ResourceKey='SB005', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Integration Events', @Description='Service Bus for handling integration events between microservices.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-59, @ResourceUid=@ResourceUid, @ResourceKey='SB006', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Error Handling Queue', @Description='Dead letter queue for handling failed message processing and retry logic.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-60, @ResourceUid=@ResourceUid, @ResourceKey='SB007', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Scheduled Jobs Topic', @Description='Topic for distributing scheduled job triggers and background task coordination.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-61, @ResourceUid=@ResourceUid, @ResourceKey='SB008', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Real-time Updates', @Description='Service Bus for real-time updates and live dashboard data synchronization.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-62, @ResourceUid=@ResourceUid, @ResourceKey='SB009', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Webhook Delivery Queue', @Description='Queue for reliable webhook delivery to external systems and partners.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-63, @ResourceUid=@ResourceUid, @ResourceKey='SB010', @ResourceTypeUid=@ResourceType_ServiceBus, @ResourceName='Data Sync Pipeline', @Description='Service Bus pipeline for synchronizing data between different database systems.'

-- Azure Service Fabric Resources (6 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-64, @ResourceUid=@ResourceUid, @ResourceKey='SF001', @ResourceTypeUid=@ResourceType_ServiceFabric, @ResourceName='Microservices Cluster', @Description='Primary Service Fabric cluster hosting microservices architecture.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-65, @ResourceUid=@ResourceUid, @ResourceKey='SF002', @ResourceTypeUid=@ResourceType_ServiceFabric, @ResourceName='Stateful Services', @Description='Service Fabric hosting stateful services with reliable collections.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-66, @ResourceUid=@ResourceUid, @ResourceKey='SF003', @ResourceTypeUid=@ResourceType_ServiceFabric, @ResourceName='Actor Model Services', @Description='Service Fabric cluster running actor-based distributed applications.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-67, @ResourceUid=@ResourceUid, @ResourceKey='SF004', @ResourceTypeUid=@ResourceType_ServiceFabric, @ResourceName='Guest Executables', @Description='Service Fabric hosting legacy applications as guest executables.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-68, @ResourceUid=@ResourceUid, @ResourceKey='SF005', @ResourceTypeUid=@ResourceType_ServiceFabric, @ResourceName='Container Orchestration', @Description='Service Fabric cluster for container orchestration and management.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-69, @ResourceUid=@ResourceUid, @ResourceKey='SF006', @ResourceTypeUid=@ResourceType_ServiceFabric, @ResourceName='High Availability Cluster', @Description='Multi-region Service Fabric cluster for high availability and disaster recovery.'

-- Azure ResourceGroup Resources (6 records)
SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-70, @ResourceUid=@ResourceUid, @ResourceKey='RG001', @ResourceTypeUid=@ResourceType_ResourceGroup, @ResourceName='Production Resources', @Description='Primary resource group containing all production environment resources.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-71, @ResourceUid=@ResourceUid, @ResourceKey='RG002', @ResourceTypeUid=@ResourceType_ResourceGroup, @ResourceName='Development Environment', @Description='Resource group for development and testing environment resources.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-72, @ResourceUid=@ResourceUid, @ResourceKey='RG003', @ResourceTypeUid=@ResourceType_ResourceGroup, @ResourceName='Monitoring and Logging', @Description='Resource group dedicated to monitoring, logging, and observability tools.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-73, @ResourceUid=@ResourceUid, @ResourceKey='RG004', @ResourceTypeUid=@ResourceType_ResourceGroup, @ResourceName='Security Services', @Description='Resource group containing security-related services and Key Vault resources.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-74, @ResourceUid=@ResourceUid, @ResourceKey='RG005', @ResourceTypeUid=@ResourceType_ResourceGroup, @ResourceName='Backup and Recovery', @Description='Resource group for backup storage accounts and disaster recovery services.'

SET @ResourceUid = 'DEMO'+LOWER(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''))
EXEC #Seed_Resource @ResourceId=-75, @ResourceUid=@ResourceUid, @ResourceKey='RG006', @ResourceTypeUid=@ResourceType_ResourceGroup, @ResourceName='Shared Infrastructure', @Description='Resource group for shared infrastructure components like networking and DNS.'

SELECT * FROM [HTResourceMapper].ResourceType
SELECT * FROM [HTResourceMapper].TagContentType
SELECT * FROM [HTResourceMapper].[Resource]

--ROLLBACK
COMMIT

DROP PROC #Seed_ResourceType
DROP PROC #Seed_TagType
DROP PROC #Seed_Resource
