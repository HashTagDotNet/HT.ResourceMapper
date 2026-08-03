--/*
--=============================================
--Demo Data Insert Script for Resource Explorer graph (IDEMPOTENT)
--=============================================
--Seeds a small, realistic cloud dependency graph so the Resource Explorer canvas
--(Resource_GetForExplorer) has real data to render, including:
--  - A connected dependency graph (ResourceRelationship) with a shared dependency
--    (multiple resources depending on the same node) and a small cycle
--    (checkout -> pricing -> inventory -> checkout).
--  - A Domain tag ('prod') on every resource, using the existing system Domain
--    TagDefinition (TagDefinitionKey = 'Domain') -- NOT a new one; there can only
--    ever be one IsDomainTag=1 row.
--  - A primary-URL Link tag on most resources, using the shared 'PortalUrl' Link
--    TagDefinition owned by Demo_Insert_TagVocabulary.sql, with
--    Resource.PrimaryTagDefinitionId pointed at it, so "Open link" has somewhere to
--    go in the explorer. Created here only if absent, never updated, so this script
--    still stands alone. (PL-46 retired the old demo-owned 'DemoExplorerPrimaryUrl',
--    which duplicated the "Portal URL" display name; the migration is inline below.)
--
--Resources use stable, readable, DEMOEXP-prefixed ResourceUids (not random GUIDs) so
--this script is idempotent (keyed by ResourceUid/TagDefinitionKey/ResourceTypeUid,
--not by hardcoded identity values) and the root uid is knowable for demos:
--    DEMOEXP-checkout  (Checkout Service -- has both DependsOn and DependentOn edges)
--
--IDEMPOTENT: safe to run multiple times - will not create duplicate rows.
--Paired with Demo_Purge_ExplorerGraph.sql, which removes exactly what this adds.
--=============================================
--*/
USE [ResourceMapper]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE #SeedExp_ResourceType(
    @ResourceTypeUid VARCHAR(40),
    @TypeName NVARCHAR(250),
    @AllowCustomTags BIT = 1
)
AS
BEGIN
    UPDATE TOP(1)
        [HTResourceMapper].ResourceType
    SET
        TypeName = @TypeName,
        AllowCustomTags = @AllowCustomTags,
        UpdatedOn = SYSUTCDATETIME()
    WHERE
        ResourceTypeUid = @ResourceTypeUid

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [HTResourceMapper].ResourceType (
            ResourceTypeUid,
            TypeName,
            AllowCustomTags
        ) VALUES (
            @ResourceTypeUid,
            @TypeName,
            @AllowCustomTags
        )
    END
END
GO

CREATE OR ALTER PROCEDURE #SeedExp_Resource(
    @ResourceUid VARCHAR(40),
    @ResourceKey NVARCHAR(250),
    @ResourceTypeUid VARCHAR(40),
    @ResourceName NVARCHAR(250),
    @Description NVARCHAR(2000)
)
AS
BEGIN
    DECLARE @ResourceTypeId INT =
        (SELECT ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE ResourceTypeUid = @ResourceTypeUid)

    UPDATE TOP(1)
        [HTResourceMapper].[Resource]
    SET
        ResourceKey = @ResourceKey,
        ResourceTypeId = @ResourceTypeId,
        ResourceName = @ResourceName,
        Description = @Description,
        UpdatedOn = SYSUTCDATETIME()
    WHERE
        ResourceUid = @ResourceUid

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [HTResourceMapper].[Resource] (
            ResourceUid,
            ResourceKey,
            ResourceTypeId,
            ResourceName,
            Description
        ) VALUES (
            @ResourceUid,
            @ResourceKey,
            @ResourceTypeId,
            @ResourceName,
            @Description
        )
    END
END
GO

CREATE OR ALTER PROCEDURE #SeedExp_ResourceTag(
    @ResourceUid VARCHAR(40),
    @TagDefinitionKey NVARCHAR(50),
    @TagValue NVARCHAR(2000)
)
AS
BEGIN
    DECLARE @ResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @ResourceUid)
    DECLARE @TagDefinitionId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = @TagDefinitionKey)

    UPDATE TOP(1)
        [HTResourceMapper].ResourceTag
    SET
        TagValue = @TagValue,
        UpdatedOn = SYSUTCDATETIME()
    WHERE
        ResourceId = @ResourceId AND TagDefinitionId = @TagDefinitionId

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [HTResourceMapper].ResourceTag (
            ResourceId,
            TagDefinitionId,
            TagValue
        ) VALUES (
            @ResourceId,
            @TagDefinitionId,
            @TagValue
        )
    END
END
GO

CREATE OR ALTER PROCEDURE #SeedExp_PrimaryUrl(
    @ResourceUid VARCHAR(40),
    @TagDefinitionKey NVARCHAR(50)
)
AS
BEGIN
    DECLARE @TagDefinitionId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = @TagDefinitionKey)

    UPDATE [HTResourceMapper].[Resource]
    SET PrimaryTagDefinitionId = @TagDefinitionId
    WHERE ResourceUid = @ResourceUid
END
GO

CREATE OR ALTER PROCEDURE #SeedExp_Relationship(
    @FromResourceUid VARCHAR(40),
    @ToResourceUid VARCHAR(40)
)
AS
BEGIN
    DECLARE @FromResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @FromResourceUid)
    DECLARE @ToResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @ToResourceUid)

    IF NOT EXISTS (
        SELECT 1 FROM [HTResourceMapper].ResourceRelationship
        WHERE FromResourceId = @FromResourceId AND ToResourceId = @ToResourceId
    )
    BEGIN
        INSERT INTO [HTResourceMapper].ResourceRelationship (
            FromResourceId,
            ToResourceId
        ) VALUES (
            @FromResourceId,
            @ToResourceId
        )
    END
END
GO

BEGIN TRANSACTION

-- Demo-owned resource types (kept separate from other Demo_* datasets' types so this
-- script's purge never has to touch types it doesn't own).
DECLARE @Type_App VARCHAR(40) = 'DEMOEXP-type-app'
, @Type_Service VARCHAR(40) = 'DEMOEXP-type-service'
, @Type_Database VARCHAR(40) = 'DEMOEXP-type-database'
, @Type_Queue VARCHAR(40) = 'DEMOEXP-type-queue'

EXEC #SeedExp_ResourceType @ResourceTypeUid = @Type_App, @TypeName = 'App'
EXEC #SeedExp_ResourceType @ResourceTypeUid = @Type_Service, @TypeName = 'Service'
EXEC #SeedExp_ResourceType @ResourceTypeUid = @Type_Database, @TypeName = 'Database'
EXEC #SeedExp_ResourceType @ResourceTypeUid = @Type_Queue, @TypeName = 'Queue'

-- Primary-URL Link tag. NOTE: the Domain tag is NOT created here -- it already exists as the
-- one system-wide IsDomainTag=1 row (TagDefinitionKey = 'Domain', seeded by
-- Script.PostDeployment1.sql) and is reused below by key.
--
-- PL-46: this script used to own its own 'DemoExplorerPrimaryUrl' definition whose DisplayName
-- was also 'Portal URL', colliding with the vocabulary seed's 'PortalUrl'. Distinct keys, so
-- the DB was satisfied, but the editor labelled both rows identically and a resource carrying
-- both showed two indistinguishable "Portal URL" rows. The duplicate is retired: this script
-- now uses the shared 'PortalUrl' definition owned by Demo_Insert_TagVocabulary.sql.
DECLARE @UrlTagKey NVARCHAR(50) = 'PortalUrl'

-- ------------------------------------------------------------------------------------------
-- PL-46 migration: fold any pre-existing 'DemoExplorerPrimaryUrl' data into 'PortalUrl'.
-- A no-op once the retired definition is gone, so re-running this script stays idempotent.
-- ------------------------------------------------------------------------------------------
DECLARE @RetiredUrlTagId INT =
    (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = 'DemoExplorerPrimaryUrl')
DECLARE @PortalUrlTagId INT =
    (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = @UrlTagKey)

IF @RetiredUrlTagId IS NOT NULL AND @PortalUrlTagId IS NOT NULL
BEGIN
    -- ResourceTag has no unique constraint on (ResourceId, TagDefinitionId) (PL-A5), so a blind
    -- repoint could double up a single-valued tag. Where a resource already holds PortalUrl,
    -- drop that row and keep the explorer's value -- it is the one wired as the resource's
    -- PrimaryTagDefinitionId, and this script re-asserts that value below regardless.
    DELETE p
    FROM [HTResourceMapper].ResourceTag p
    WHERE p.TagDefinitionId = @PortalUrlTagId
      AND EXISTS (SELECT 1 FROM [HTResourceMapper].ResourceTag e
                  WHERE e.ResourceId = p.ResourceId AND e.TagDefinitionId = @RetiredUrlTagId)

    UPDATE [HTResourceMapper].ResourceTag
    SET TagDefinitionId = @PortalUrlTagId, UpdatedOn = SYSUTCDATETIME()
    WHERE TagDefinitionId = @RetiredUrlTagId

    UPDATE [HTResourceMapper].[Resource]
    SET PrimaryTagDefinitionId = @PortalUrlTagId
    WHERE PrimaryTagDefinitionId = @RetiredUrlTagId

    DELETE FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionId = @RetiredUrlTagId
END
ELSE IF @RetiredUrlTagId IS NOT NULL
BEGIN
    -- PortalUrl not seeded yet: rename the retired row in place rather than discarding data.
    -- The ensure-block below then finds it by key and leaves it alone.
    UPDATE [HTResourceMapper].TagDefinition
    SET TagDefinitionKey = @UrlTagKey, UpdatedOn = SYSUTCDATETIME()
    WHERE TagDefinitionId = @RetiredUrlTagId
END

-- ------------------------------------------------------------------------------------------
-- Ensure 'PortalUrl' exists. Demo_Insert_TagVocabulary.sql is its OWNER -- this block only
-- creates it when absent (so the explorer seed still stands alone) and never updates it, so
-- the two scripts cannot fight over RequirementLevel / DisplayOrder whatever the run order.
-- Values deliberately mirror the vocabulary seed's definition.
-- ------------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = @UrlTagKey)
BEGIN
    INSERT INTO [HTResourceMapper].[TagDefinition]
        ([TagDefinitionUid], [TagDefinitionKey], [DisplayName], [TagContentTypeId]
        ,[AllowCustomValue], [IsMultiValued], [RequirementLevel]
        ,[IsDomainTag], [IsSystemTag], [DisplayOrder])
    -- Content type resolved by TagCode, never a hardcoded id.
    SELECT 'DEMOVOCAB-tagdef-portalurl', @UrlTagKey, N'Portal URL', tct.TagContentTypeId
          ,CAST(1 AS BIT), CAST(0 AS BIT), N'Suggested'
          ,CAST(0 AS BIT), CAST(0 AS BIT), 100
    FROM [HTResourceMapper].TagContentType tct
    WHERE tct.TagCode = 'Link'
END

DECLARE @DomainTagKey NVARCHAR(50) =
    (SELECT TagDefinitionKey FROM [HTResourceMapper].TagDefinition WHERE IsDomainTag = 1)

-- ==========================================================================
-- Resources: a small storefront topology -- 2 apps, 4 services, 2 databases,
-- 1 queue, 1 shared cache.
-- ==========================================================================
EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-web', @ResourceKey = 'WEB-STOREFRONT', @ResourceTypeUid = @Type_App,
    @ResourceName = 'Web Storefront', @Description = 'Customer-facing storefront web application.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-mobile-bff', @ResourceKey = 'MOBILE-BFF', @ResourceTypeUid = @Type_App,
    @ResourceName = 'Mobile BFF', @Description = 'Backend-for-frontend API consumed by the mobile apps.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-checkout', @ResourceKey = 'CHECKOUT-SVC', @ResourceTypeUid = @Type_Service,
    @ResourceName = 'Checkout Service', @Description = 'Handles cart checkout, order creation, and payment orchestration.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-catalog', @ResourceKey = 'CATALOG-SVC', @ResourceTypeUid = @Type_Service,
    @ResourceName = 'Catalog Service', @Description = 'Serves product catalog and search results.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-pricing', @ResourceKey = 'PRICING-SVC', @ResourceTypeUid = @Type_Service,
    @ResourceName = 'Pricing Service', @Description = 'Computes prices, discounts, and promotions.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-inventory', @ResourceKey = 'INVENTORY-SVC', @ResourceTypeUid = @Type_Service,
    @ResourceName = 'Inventory Service', @Description = 'Tracks stock levels and reservations.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-orders-queue', @ResourceKey = 'ORDERS-QUEUE', @ResourceTypeUid = @Type_Queue,
    @ResourceName = 'Orders Queue', @Description = 'Message queue for asynchronous order processing.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-orders-db', @ResourceKey = 'ORDERS-DB', @ResourceTypeUid = @Type_Database,
    @ResourceName = 'Orders Database', @Description = 'Primary datastore for order and payment records.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-catalog-db', @ResourceKey = 'CATALOG-DB', @ResourceTypeUid = @Type_Database,
    @ResourceName = 'Catalog Database', @Description = 'Primary datastore for product catalog data.'

EXEC #SeedExp_Resource @ResourceUid = 'DEMOEXP-shared-cache', @ResourceKey = 'SHARED-CACHE', @ResourceTypeUid = @Type_Database,
    @ResourceName = 'Shared Cache', @Description = 'Shared Redis cache for catalog and pricing lookups.'

-- ==========================================================================
-- Domain tag ('prod') on every resource.
-- ==========================================================================
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-web', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-mobile-bff', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-checkout', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-catalog', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-pricing', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-inventory', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-orders-queue', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-orders-db', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-catalog-db', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-shared-cache', @TagDefinitionKey = @DomainTagKey, @TagValue = 'prod'

-- ==========================================================================
-- Primary URL Link tag on most resources (8 of 10 -- inventory and shared-cache are
-- left without one, to also exercise the "no primary URL" case in the canvas).
-- ==========================================================================
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-web', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Web/sites/web-storefront/appServices'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-mobile-bff', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Web/sites/mobile-bff/appServices'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-checkout', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Web/sites/checkout-service/appServices'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-catalog', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Web/sites/catalog-service/appServices'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-pricing', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Web/sites/pricing-service/appServices'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-orders-queue', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.ServiceBus/namespaces/demo-sb/queues/orders'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-orders-db', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Sql/servers/demo-sql/databases/orders'
EXEC #SeedExp_ResourceTag @ResourceUid = 'DEMOEXP-catalog-db', @TagDefinitionKey = @UrlTagKey,
    @TagValue = 'https://portal.azure.com/#@/resource/subscriptions/demo-sub/resourceGroups/demo-rg/providers/Microsoft.Sql/servers/demo-sql/databases/catalog'

EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-web', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-mobile-bff', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-checkout', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-catalog', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-pricing', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-orders-queue', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-orders-db', @TagDefinitionKey = @UrlTagKey
EXEC #SeedExp_PrimaryUrl @ResourceUid = 'DEMOEXP-catalog-db', @TagDefinitionKey = @UrlTagKey

-- ==========================================================================
-- Dependency graph (From = dependent, To = dependency).
-- Shared dependencies: checkout & catalog are each depended on by both web and
-- mobile-bff; shared-cache is depended on by both catalog and pricing; catalog-db
-- is depended on by both catalog and inventory.
-- Cycle: checkout -> pricing -> inventory -> checkout.
-- ==========================================================================
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-web', @ToResourceUid = 'DEMOEXP-checkout'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-web', @ToResourceUid = 'DEMOEXP-catalog'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-web', @ToResourceUid = 'DEMOEXP-pricing'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-mobile-bff', @ToResourceUid = 'DEMOEXP-checkout'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-mobile-bff', @ToResourceUid = 'DEMOEXP-catalog'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-checkout', @ToResourceUid = 'DEMOEXP-orders-queue'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-checkout', @ToResourceUid = 'DEMOEXP-orders-db'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-checkout', @ToResourceUid = 'DEMOEXP-pricing'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-pricing', @ToResourceUid = 'DEMOEXP-inventory'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-inventory', @ToResourceUid = 'DEMOEXP-checkout'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-catalog', @ToResourceUid = 'DEMOEXP-catalog-db'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-catalog', @ToResourceUid = 'DEMOEXP-shared-cache'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-pricing', @ToResourceUid = 'DEMOEXP-shared-cache'
EXEC #SeedExp_Relationship @FromResourceUid = 'DEMOEXP-inventory', @ToResourceUid = 'DEMOEXP-catalog-db'

SELECT * FROM [HTResourceMapper].ResourceType WHERE ResourceTypeUid LIKE 'DEMOEXP-%'
SELECT * FROM [HTResourceMapper].[Resource] WHERE ResourceUid LIKE 'DEMOEXP-%'
SELECT r.ResourceUid, rt.TagValue FROM [HTResourceMapper].ResourceTag rt
    INNER JOIN [HTResourceMapper].[Resource] r ON r.ResourceId = rt.ResourceId
    WHERE r.ResourceUid LIKE 'DEMOEXP-%'
SELECT fr.ResourceUid AS FromUid, tr.ResourceUid AS ToUid
    FROM [HTResourceMapper].ResourceRelationship rel
    INNER JOIN [HTResourceMapper].[Resource] fr ON fr.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[Resource] tr ON tr.ResourceId = rel.ToResourceId
    WHERE fr.ResourceUid LIKE 'DEMOEXP-%'

--ROLLBACK
COMMIT

DROP PROCEDURE #SeedExp_ResourceType
DROP PROCEDURE #SeedExp_Resource
DROP PROCEDURE #SeedExp_ResourceTag
DROP PROCEDURE #SeedExp_PrimaryUrl
DROP PROCEDURE #SeedExp_Relationship
