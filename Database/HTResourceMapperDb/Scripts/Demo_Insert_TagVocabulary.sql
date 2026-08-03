--/*
--=============================================
--Demo Data Insert Script for the tag vocabulary (IDEMPOTENT)
--=============================================
--Tagging is the product's headline feature, but the demo catalog only ever had two
--TagDefinitions ('Domain' + 'DemoExplorerPrimaryUrl') and ZERO ResourceTypeTag rows.
--That left multi-valued tags, controlled vocabularies, Link-vs-Text content types,
--primary-tag selection and the whole per-type entry-point template feature with no
--data to demonstrate or test against.
--
--This script seeds:
--  1. Ten demo-owned TagDefinitions covering every capability combination:
--       - Link and Text content types (resolved by TagCode, never hardcoded ids)
--       - single-valued and multi-valued
--       - free-text and controlled vocabulary (AllowCustomValue = 0 + AllowedValues)
--       - Error / Suggested / Optional requirement levels
--  2. A ResourceTypeTag entry-point template for EVERY resource type, each with
--     exactly one IsDefaultPrimary = 1 row pointing at a Link tag.
--  3. Applied ResourceTag values across the whole demo catalog, including
--     Resource.PrimaryTagDefinitionId wiring and several resources carrying two or
--     more values of a multi-valued tag.
--
--NOT touched (owned elsewhere):
--  - 'Domain'                 -- the single system IsDomainTag = 1 row (Script.PostDeployment1.sql).
--                                Resolved by lookup here, never created or modified.
--  - 'DemoExplorerPrimaryUrl' -- owned by Demo_Insert_ExplorerGraph.sql. Resources that
--                                already carry it keep it as their primary entry point.
--  - TagContentType rows      -- resolved by TagCode ('Text' / 'Link').
--
--IDEMPOTENT: safe to run multiple times -- will not create duplicate rows.
--WARNING: [HTResourceMapper].[ResourceTag] has NO unique constraint on
--(ResourceId, TagDefinitionId) (punchlist PL-A5), so nothing at the DB level stops a
--naive re-run from doubling single-valued tags. This script guards explicitly: it
--de-duplicates first, then UPDATEs in place, then INSERTs only what is missing.
--
--Paired with Demo_Purge_TagVocabulary.sql, which removes exactly what this adds.
--=============================================
--*/
USE [ResourceMapper]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- =========================================================================================
-- Helper: upsert a demo-owned TagDefinition, keyed on the natural key TagDefinitionKey.
-- Content type is resolved from TagContentType.TagCode ('Text' | 'Link'), never hardcoded.
-- =========================================================================================
CREATE OR ALTER PROCEDURE #SeedVocab_TagDefinition(
    @TagDefinitionUid VARCHAR(40),
    @TagDefinitionKey NVARCHAR(50),
    @DisplayName      NVARCHAR(100),
    @ContentType      VARCHAR(20),          -- 'Text' | 'Link'
    @IsMultiValued    BIT,
    @AllowCustomValue BIT,
    @AllowedValues    NVARCHAR(2000),       -- JSON array string, or NULL
    @RequirementLevel NVARCHAR(20),
    @DisplayOrder     INT
)
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @TagContentTypeId INT =
        (SELECT TagContentTypeId FROM [HTResourceMapper].TagContentType WHERE TagCode = @ContentType)

    IF @TagContentTypeId IS NULL
    BEGIN
        RAISERROR('Unknown TagContentType code "%s" -- cannot seed tag "%s".', 16, 1, @ContentType, @TagDefinitionKey)
        RETURN
    END

    UPDATE TOP(1)
        [HTResourceMapper].TagDefinition
    SET
         DisplayName      = @DisplayName
        ,TagContentTypeId = @TagContentTypeId
        ,IsMultiValued    = @IsMultiValued
        ,AllowCustomValue = @AllowCustomValue
        ,AllowedValues    = @AllowedValues
        ,RequirementLevel = @RequirementLevel
        ,IsDomainTag      = 0
        ,IsSystemTag      = 0
        ,DisplayOrder     = @DisplayOrder
        ,UpdatedOn        = SYSUTCDATETIME()
    WHERE
        TagDefinitionKey = @TagDefinitionKey

    IF @@ROWCOUNT = 0
    BEGIN
        INSERT INTO [HTResourceMapper].TagDefinition (
             TagDefinitionUid
            ,TagDefinitionKey
            ,DisplayName
            ,TagContentTypeId
            ,IsMultiValued
            ,AllowCustomValue
            ,AllowedValues
            ,RequirementLevel
            ,IsDomainTag
            ,IsSystemTag
            ,DisplayOrder
        ) VALUES (
             @TagDefinitionUid
            ,@TagDefinitionKey
            ,@DisplayName
            ,@TagContentTypeId
            ,@IsMultiValued
            ,@AllowCustomValue
            ,@AllowedValues
            ,@RequirementLevel
            ,0
            ,0
            ,@DisplayOrder
        )
    END
END
GO

-- =========================================================================================
-- Helper: add a tag to a resource type's entry-point template. Always written with
-- IsDefaultPrimary = 0; the default primary is set afterwards by #SeedVocab_TypeDefaultPrimary
-- so the filtered unique index UX_ResourceTypeTag_DefaultPrimary never sees two 1s at once.
-- Resource types are resolved by ResourceTypeUid (seeded types have NEGATIVE identity values,
-- so nothing here may assume a positive id).
-- =========================================================================================
CREATE OR ALTER PROCEDURE #SeedVocab_TypeTag(
    @ResourceTypeUid  VARCHAR(40),
    @TagDefinitionKey NVARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @ResourceTypeId INT =
        (SELECT ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE ResourceTypeUid = @ResourceTypeUid)
    DECLARE @TagDefinitionId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = @TagDefinitionKey)

    -- A type that does not exist in this database is skipped, not forced.
    IF @ResourceTypeId IS NULL OR @TagDefinitionId IS NULL RETURN

    IF NOT EXISTS (
        SELECT 1 FROM [HTResourceMapper].ResourceTypeTag
        WHERE ResourceTypeId = @ResourceTypeId AND TagDefinitionId = @TagDefinitionId
    )
    BEGIN
        INSERT INTO [HTResourceMapper].ResourceTypeTag (ResourceTypeId, TagDefinitionId, IsDefaultPrimary)
        VALUES (@ResourceTypeId, @TagDefinitionId, 0)
    END
END
GO

-- =========================================================================================
-- Helper: designate the default primary entry point for a resource type.
-- Clear-then-set, in that order: UX_ResourceTypeTag_DefaultPrimary is a filtered UNIQUE
-- index on (ResourceTypeId) WHERE IsDefaultPrimary = 1, so the old winner must be demoted
-- in its own statement before the new one is promoted.
-- =========================================================================================
CREATE OR ALTER PROCEDURE #SeedVocab_TypeDefaultPrimary(
    @ResourceTypeUid  VARCHAR(40),
    @TagDefinitionKey NVARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @ResourceTypeId INT =
        (SELECT ResourceTypeId FROM [HTResourceMapper].ResourceType WHERE ResourceTypeUid = @ResourceTypeUid)
    DECLARE @TagDefinitionId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = @TagDefinitionKey)

    IF @ResourceTypeId IS NULL OR @TagDefinitionId IS NULL RETURN

    -- 1. demote any other current default for this type
    UPDATE [HTResourceMapper].ResourceTypeTag
    SET IsDefaultPrimary = 0
    WHERE ResourceTypeId = @ResourceTypeId
      AND IsDefaultPrimary = 1
      AND TagDefinitionId <> @TagDefinitionId

    -- 2. promote the intended one
    UPDATE [HTResourceMapper].ResourceTypeTag
    SET IsDefaultPrimary = 1
    WHERE ResourceTypeId = @ResourceTypeId
      AND TagDefinitionId = @TagDefinitionId
      AND IsDefaultPrimary = 0
END
GO

BEGIN TRANSACTION

-- =========================================================================================
-- 1. Tag definitions
--
--    DisplayOrder leaves 10 (Domain) and 20 (DemoExplorerPrimaryUrl) alone and runs
--    100..190: entry-point links first, then ownership, then classification, then the
--    multi-valued documentation tags last.
-- =========================================================================================
EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-portalurl'
    ,@TagDefinitionKey = N'PortalUrl'
    ,@DisplayName      = N'Portal URL'
    ,@ContentType      = 'Link'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Suggested'
    ,@DisplayOrder     = 100

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-repository'
    ,@TagDefinitionKey = N'Repository'
    ,@DisplayName      = N'Source Repository'
    ,@ContentType      = 'Link'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Suggested'
    ,@DisplayOrder     = 110

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-owner'
    ,@TagDefinitionKey = N'Owner'
    ,@DisplayName      = N'Owning Team'
    ,@ContentType      = 'Text'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 0
    ,@AllowedValues    = N'["Visibility","Integrations","Platform","Data"]'
    ,@RequirementLevel = N'Error'
    ,@DisplayOrder     = 120

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-oncall'
    ,@TagDefinitionKey = N'OnCall'
    ,@DisplayName      = N'On-Call Rotation'
    ,@ContentType      = 'Text'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Optional'
    ,@DisplayOrder     = 130

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-tier'
    ,@TagDefinitionKey = N'Tier'
    ,@DisplayName      = N'Service Tier'
    ,@ContentType      = 'Text'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 0
    ,@AllowedValues    = N'["tier-1","tier-2","tier-3"]'
    ,@RequirementLevel = N'Suggested'
    ,@DisplayOrder     = 140

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-lifecycle'
    ,@TagDefinitionKey = N'Lifecycle'
    ,@DisplayName      = N'Lifecycle'
    ,@ContentType      = 'Text'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 0
    ,@AllowedValues    = N'["active","deprecated","retiring","planned"]'
    ,@RequirementLevel = N'Suggested'
    ,@DisplayOrder     = 150

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-costcenter'
    ,@TagDefinitionKey = N'CostCenter'
    ,@DisplayName      = N'Cost Center'
    ,@ContentType      = 'Text'
    ,@IsMultiValued    = 0
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Optional'
    ,@DisplayOrder     = 160

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-component'
    ,@TagDefinitionKey = N'Component'
    ,@DisplayName      = N'Component'
    ,@ContentType      = 'Text'
    ,@IsMultiValued    = 1
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Optional'
    ,@DisplayOrder     = 170

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-runbook'
    ,@TagDefinitionKey = N'Runbook'
    ,@DisplayName      = N'Runbook'
    ,@ContentType      = 'Link'
    ,@IsMultiValued    = 1
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Optional'
    ,@DisplayOrder     = 180

EXEC #SeedVocab_TagDefinition
     @TagDefinitionUid = 'DEMOVOCAB-tagdef-documentation'
    ,@TagDefinitionKey = N'Documentation'
    ,@DisplayName      = N'Documentation'
    ,@ContentType      = 'Link'
    ,@IsMultiValued    = 1
    ,@AllowCustomValue = 1
    ,@AllowedValues    = NULL
    ,@RequirementLevel = N'Optional'
    ,@DisplayOrder     = 190

-- =========================================================================================
-- 2. Entry-point templates (ResourceTypeTag), one per resource type.
--    Note "Azure ServcieFabric" is a genuine misspelling in the seeded type data
--    (punchlist PL-A2) -- matched as-is here rather than silently renamed.
-- =========================================================================================
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppServiceUid',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppServiceUid',   @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppServiceUid',   @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppServiceUid',   @TagDefinitionKey = N'Tier'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppServiceUid',   @TagDefinitionKey = N'Lifecycle'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppUid',          @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppUid',          @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppUid',          @TagDefinitionKey = N'Component'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppUid',          @TagDefinitionKey = N'Lifecycle'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-app',     @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-app',     @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-app',     @TagDefinitionKey = N'Component'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-app',     @TagDefinitionKey = N'Lifecycle'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-service', @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-service', @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-service', @TagDefinitionKey = N'OnCall'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-service', @TagDefinitionKey = N'Tier'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-service', @TagDefinitionKey = N'Lifecycle'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-database',@TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-database',@TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-database',@TagDefinitionKey = N'Runbook'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-database',@TagDefinitionKey = N'Tier'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AzureUid',        @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AzureUid',        @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AzureUid',        @TagDefinitionKey = N'Tier'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ServicebusUid',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ServicebusUid',   @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ServicebusUid',   @TagDefinitionKey = N'Runbook'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-queue',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-queue',   @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMOEXP-type-queue',   @TagDefinitionKey = N'Runbook'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppInsightsUid',  @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppInsightsUid',  @TagDefinitionKey = N'Owner'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppConfigUid',    @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-AppConfigUid',    @TagDefinitionKey = N'Owner'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ResourceGroupUid',@TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ResourceGroupUid',@TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ResourceGroupUid',@TagDefinitionKey = N'CostCenter'

EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ServiceFabric',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ServiceFabric',   @TagDefinitionKey = N'Owner'
EXEC #SeedVocab_TypeTag @ResourceTypeUid = 'DEMO-ServiceFabric',   @TagDefinitionKey = N'Runbook'

-- Safety net: any resource type present in this database but not named above still gets a
-- minimal template (Portal URL + Owner), so "every type has an entry-point template" holds
-- even if the type list drifts.
DECLARE @UnmappedTypeUid VARCHAR(40)
DECLARE unmapped_types CURSOR LOCAL FAST_FORWARD FOR
    SELECT rt.ResourceTypeUid
    FROM [HTResourceMapper].ResourceType rt
    WHERE NOT EXISTS (SELECT 1 FROM [HTResourceMapper].ResourceTypeTag t WHERE t.ResourceTypeId = rt.ResourceTypeId)

OPEN unmapped_types
FETCH NEXT FROM unmapped_types INTO @UnmappedTypeUid
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC #SeedVocab_TypeTag @ResourceTypeUid = @UnmappedTypeUid, @TagDefinitionKey = N'PortalUrl'
    EXEC #SeedVocab_TypeTag @ResourceTypeUid = @UnmappedTypeUid, @TagDefinitionKey = N'Owner'
    EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = @UnmappedTypeUid, @TagDefinitionKey = N'PortalUrl'
    FETCH NEXT FROM unmapped_types INTO @UnmappedTypeUid
END
CLOSE unmapped_types
DEALLOCATE unmapped_types

-- Default primary entry point -- exactly one per type, always a Link tag.
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-AppServiceUid',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-AppUid',          @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMOEXP-type-app',     @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMOEXP-type-service', @TagDefinitionKey = N'Repository'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMOEXP-type-database',@TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-AzureUid',        @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-ServicebusUid',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMOEXP-type-queue',   @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-AppInsightsUid',  @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-AppConfigUid',    @TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-ResourceGroupUid',@TagDefinitionKey = N'PortalUrl'
EXEC #SeedVocab_TypeDefaultPrimary @ResourceTypeUid = 'DEMO-ServiceFabric',   @TagDefinitionKey = N'PortalUrl'

-- =========================================================================================
-- 3. Applied tags.
--
--    Everything below is set-based and deterministic (driven off ABS(ResourceId) % n), so a
--    re-run produces byte-identical values -- no NEWID(), no RAND(), no getdate-derived data.
-- =========================================================================================
DECLARE @TagId_PortalUrl     INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'PortalUrl')
DECLARE @TagId_Repository    INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Repository')
DECLARE @TagId_Owner         INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Owner')
DECLARE @TagId_OnCall        INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'OnCall')
DECLARE @TagId_Tier          INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Tier')
DECLARE @TagId_Lifecycle     INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Lifecycle')
DECLARE @TagId_CostCenter    INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'CostCenter')
DECLARE @TagId_Component     INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Component')
DECLARE @TagId_Runbook       INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Runbook')
DECLARE @TagId_Documentation INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'Documentation')

-- The explorer seed's own primary-URL tag. Resources that already carry it keep it as their
-- entry point and are skipped for PortalUrl, so the editor never shows two "Portal URL" rows.
DECLARE @TagId_ExplorerUrl   INT = (SELECT TagDefinitionId FROM [HTResourceMapper].TagDefinition WHERE TagDefinitionKey = N'DemoExplorerPrimaryUrl')

-- ---------------------------------------------------------------------------------------
-- Projection of the catalog with the deterministic derivations used below.
-- ---------------------------------------------------------------------------------------
SELECT
     r.ResourceId
    ,r.ResourceUid
    ,r.ResourceKey
    ,rt.ResourceTypeUid
    ,[Slug]   = LOWER(REPLACE(REPLACE(CAST(r.ResourceKey AS NVARCHAR(250)), N' ', N'-'), N'_', N'-'))
    ,[Bucket] = ABS(r.ResourceId) % 4
    ,[Team]   = CHOOSE(ABS(r.ResourceId) % 4 + 1, N'Visibility', N'Integrations', N'Platform', N'Data')
    ,[Provider] = CASE rt.ResourceTypeUid
                      WHEN 'DEMO-AppServiceUid'    THEN N'Microsoft.Web/sites'
                      WHEN 'DEMO-AppUid'           THEN N'Microsoft.Web/sites'
                      WHEN 'DEMO-AzureUid'         THEN N'Microsoft.Cache/Redis'
                      WHEN 'DEMO-AppInsightsUid'   THEN N'Microsoft.Insights/components'
                      WHEN 'DEMO-AppConfigUid'     THEN N'Microsoft.AppConfiguration/configurationStores'
                      WHEN 'DEMO-ServicebusUid'    THEN N'Microsoft.ServiceBus/namespaces'
                      WHEN 'DEMO-ServiceFabric'    THEN N'Microsoft.ServiceFabric/clusters'
                      WHEN 'DEMO-ResourceGroupUid' THEN N'Microsoft.Resources/resourceGroups'
                      WHEN 'DEMOEXP-type-database' THEN N'Microsoft.Sql/servers/demo-sql/databases'
                      WHEN 'DEMOEXP-type-queue'    THEN N'Microsoft.ServiceBus/namespaces/demo-sb/queues'
                      ELSE N'Microsoft.Resources/resources'
                  END
    -- Which Link tag is this resource's entry point, per its type's default-primary template?
    ,[LinkTagId] = CASE WHEN rt.ResourceTypeUid IN ('DEMO-AppUid', 'DEMOEXP-type-app', 'DEMOEXP-type-service')
                        THEN @TagId_Repository ELSE @TagId_PortalUrl END
INTO #VocabRes
FROM [HTResourceMapper].[Resource] r
INNER JOIN [HTResourceMapper].ResourceType rt ON rt.ResourceTypeId = r.ResourceTypeId

-- ---------------------------------------------------------------------------------------
-- Desired assignments. IsMultiValued drives which reconciliation rule applies below.
-- ---------------------------------------------------------------------------------------
CREATE TABLE #VocabAssign (
     ResourceId      INT NOT NULL
    ,TagDefinitionId INT NOT NULL
    ,TagValue        NVARCHAR(2000) NOT NULL
    ,IsMultiValued   BIT NOT NULL
)

-- Owner -- EVERY resource. RequirementLevel = 'Error', so a resource without it fails
-- editor validation. Values are drawn from the definition's AllowedValues.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT v.ResourceId, @TagId_Owner, v.Team, 0
FROM #VocabRes v

-- Entry-point link (Portal URL or Source Repository, per the type template).
-- Skipped for ABS(ResourceId) % 17 = 0 so the "no primary entry point" case still exists,
-- and skipped where the explorer seed already owns the resource's portal URL.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT
     v.ResourceId
    ,v.LinkTagId
    ,CASE WHEN v.LinkTagId = @TagId_Repository
          THEN N'https://dev.azure.com/contoso/CloudCatalog/_git/' + v.Slug
          ELSE N'https://portal.azure.com/#@contoso.onmicrosoft.com/resource/subscriptions/'
             + N'2f1c0a64-9d3b-4a17-8e55-0c1d7b93a4e2/resourceGroups/rg-contoso-demo/providers/'
             + v.Provider + N'/' + v.Slug + N'/overview'
     END
    ,0
FROM #VocabRes v
WHERE ABS(v.ResourceId) % 17 <> 0
  AND NOT (v.LinkTagId = @TagId_PortalUrl
           AND EXISTS (SELECT 1 FROM [HTResourceMapper].ResourceTag x
                       WHERE x.ResourceId = v.ResourceId AND x.TagDefinitionId = @TagId_ExplorerUrl))

-- Source Repository as a SECOND link tag on Azure App Service, so primary-tag selection has
-- a real choice to make (two Link tags, one starred).
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT v.ResourceId, @TagId_Repository, N'https://dev.azure.com/contoso/CloudCatalog/_git/' + v.Slug, 0
FROM #VocabRes v
WHERE v.ResourceTypeUid = 'DEMO-AppServiceUid'

-- Service Tier -- controlled vocabulary, on the types whose template lists it.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT v.ResourceId, @TagId_Tier, CHOOSE(ABS(v.ResourceId) % 3 + 1, N'tier-1', N'tier-2', N'tier-3'), 0
FROM #VocabRes v
WHERE v.ResourceTypeUid IN ('DEMO-AppServiceUid', 'DEMO-AzureUid', 'DEMOEXP-type-service', 'DEMOEXP-type-database')

-- Lifecycle -- controlled vocabulary, mostly 'active' with a realistic tail.
-- Gaps left where ABS(ResourceId) % 7 = 0 so an "unset" bucket exists for filtering.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT
     v.ResourceId
    ,@TagId_Lifecycle
    ,CHOOSE(ABS(v.ResourceId) % 8 + 1,
            N'active', N'active', N'active', N'deprecated',
            N'active', N'retiring', N'active', N'planned')
    ,0
FROM #VocabRes v
WHERE v.ResourceTypeUid IN ('DEMO-AppServiceUid', 'DEMO-AppUid', 'DEMOEXP-type-app', 'DEMOEXP-type-service')
  AND ABS(v.ResourceId) % 7 <> 0

-- Cost Center -- free text, on resource groups plus a scattered subset.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT
     v.ResourceId
    ,@TagId_CostCenter
    ,N'CC-' + CAST(1000 + (ABS(v.ResourceId) % 40) AS NVARCHAR(10))
    ,0
FROM #VocabRes v
WHERE v.ResourceTypeUid = 'DEMO-ResourceGroupUid'
   OR ABS(v.ResourceId) % 6 = 0

-- On-Call rotation -- free text, on services plus a scattered subset.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT v.ResourceId, @TagId_OnCall, N'pd-' + LOWER(v.Team) + N'-primary', 0
FROM #VocabRes v
WHERE v.ResourceTypeUid = 'DEMOEXP-type-service'
   OR ABS(v.ResourceId) % 5 = 0

-- ---------------------------------------------------------------------------------------
-- Multi-valued tags -- the case that has never been exercised. Every resource touched here
-- gets 2 or more discrete values of the same tag definition.
-- ---------------------------------------------------------------------------------------

-- Runbook (Link, multi-valued): 2 runbooks each, on the operational types.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT
     v.ResourceId
    ,@TagId_Runbook
    ,N'https://contoso.sharepoint.com/sites/ops/runbooks/' + v.Slug + s.Suffix
    ,1
FROM #VocabRes v
CROSS JOIN (VALUES (N'-failover'), (N'-restore')) AS s(Suffix)
WHERE v.ResourceTypeUid IN ('DEMO-ServicebusUid', 'DEMO-ServiceFabric', 'DEMOEXP-type-database', 'DEMOEXP-type-queue')

-- Documentation (Link, multi-valued): 2 documents each, on a subset of apps and app services.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT
     v.ResourceId
    ,@TagId_Documentation
    ,N'https://contoso.sharepoint.com/sites/engineering/docs/' + v.Slug + s.Suffix
    ,1
FROM #VocabRes v
CROSS JOIN (VALUES (N'-architecture'), (N'-onboarding')) AS s(Suffix)
WHERE (v.ResourceTypeUid = 'DEMO-AppUid'        AND ABS(v.ResourceId) % 3 = 0)
   OR (v.ResourceTypeUid = 'DEMO-AppServiceUid' AND ABS(v.ResourceId) % 3 = 1)

-- Component (Text, multi-valued): 2-3 components each, on the Application type.
INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT v.ResourceId, @TagId_Component, s.ComponentName, 1
FROM #VocabRes v
CROSS JOIN (VALUES (N'api'), (N'worker')) AS s(ComponentName)
WHERE v.ResourceTypeUid IN ('DEMO-AppUid', 'DEMOEXP-type-app')

INSERT INTO #VocabAssign (ResourceId, TagDefinitionId, TagValue, IsMultiValued)
SELECT v.ResourceId, @TagId_Component, N'scheduler', 1
FROM #VocabRes v
WHERE v.ResourceTypeUid IN ('DEMO-AppUid', 'DEMOEXP-type-app')
  AND ABS(v.ResourceId) % 2 = 0

-- ---------------------------------------------------------------------------------------
-- Defensive: collapse the desired-state set itself, so a single-valued tag can never be
-- asked for twice on the same resource and a multi-valued tag can never repeat a value.
-- ---------------------------------------------------------------------------------------
;WITH assign_dupes AS (
    SELECT rn = ROW_NUMBER() OVER (
                    PARTITION BY ResourceId, TagDefinitionId,
                                 CASE WHEN IsMultiValued = 1 THEN TagValue ELSE N'' END
                    ORDER BY TagValue)
    FROM #VocabAssign
)
DELETE FROM assign_dupes WHERE rn > 1

-- ---------------------------------------------------------------------------------------
-- Reconcile #VocabAssign into ResourceTag.
--
-- Step A guards against PL-A5: with no unique constraint on (ResourceId, TagDefinitionId),
-- a pre-existing duplicate would survive an UPDATE-then-INSERT and be reported as a
-- single-valued tag with two rows. Collapse first, then update, then insert what is missing.
-- ---------------------------------------------------------------------------------------

-- A. Collapse any duplicate rows for the single-valued tags this script owns.
-- (single base table only -- a CTE over a join is not updatable)
;WITH dupes AS (
    SELECT rt.ResourceTagId,
           rn = ROW_NUMBER() OVER (PARTITION BY rt.ResourceId, rt.TagDefinitionId ORDER BY rt.ResourceTagId)
    FROM [HTResourceMapper].ResourceTag rt
    WHERE rt.TagDefinitionId IN (@TagId_PortalUrl, @TagId_Repository, @TagId_Owner, @TagId_OnCall,
                                 @TagId_Tier, @TagId_CostCenter, @TagId_Lifecycle)
)
DELETE FROM dupes WHERE rn > 1

-- B. Update single-valued rows already present but holding a different value.
UPDATE rt
SET  rt.TagValue  = a.TagValue
    ,rt.UpdatedOn = SYSUTCDATETIME()
FROM [HTResourceMapper].ResourceTag rt
INNER JOIN #VocabAssign a
    ON a.ResourceId = rt.ResourceId AND a.TagDefinitionId = rt.TagDefinitionId
WHERE a.IsMultiValued = 0
  AND ISNULL(rt.TagValue, N'') <> a.TagValue

-- C. Insert single-valued rows that do not exist yet (exactly one row per resource+tag).
INSERT INTO [HTResourceMapper].ResourceTag (ResourceId, TagDefinitionId, TagValue)
SELECT a.ResourceId, a.TagDefinitionId, a.TagValue
FROM #VocabAssign a
WHERE a.IsMultiValued = 0
  AND NOT EXISTS (
        SELECT 1 FROM [HTResourceMapper].ResourceTag rt
        WHERE rt.ResourceId = a.ResourceId AND rt.TagDefinitionId = a.TagDefinitionId)

-- D. Insert multi-valued rows keyed on (resource, tag, value) -- several rows per resource
--    are expected here; only exact-value duplicates are suppressed.
INSERT INTO [HTResourceMapper].ResourceTag (ResourceId, TagDefinitionId, TagValue)
SELECT a.ResourceId, a.TagDefinitionId, a.TagValue
FROM #VocabAssign a
WHERE a.IsMultiValued = 1
  AND NOT EXISTS (
        SELECT 1 FROM [HTResourceMapper].ResourceTag rt
        WHERE rt.ResourceId = a.ResourceId
          AND rt.TagDefinitionId = a.TagDefinitionId
          AND rt.TagValue = a.TagValue)

-- ---------------------------------------------------------------------------------------
-- E. Point Resource.PrimaryTagDefinitionId at the entry-point link tag, but only where no
--    primary is set yet -- so the explorer seed's choice (and any user choice) is preserved.
-- ---------------------------------------------------------------------------------------
UPDATE r
SET  r.PrimaryTagDefinitionId = v.LinkTagId
    ,r.UpdatedOn = SYSUTCDATETIME()
FROM [HTResourceMapper].[Resource] r
INNER JOIN #VocabRes v ON v.ResourceId = r.ResourceId
WHERE r.PrimaryTagDefinitionId IS NULL
  AND EXISTS (SELECT 1 FROM [HTResourceMapper].ResourceTag rt
              WHERE rt.ResourceId = r.ResourceId AND rt.TagDefinitionId = v.LinkTagId)

-- =========================================================================================
-- Summary
-- =========================================================================================
SELECT TagDefinitionId, TagDefinitionKey, DisplayName, TagContentTypeId, IsMultiValued,
       AllowCustomValue, AllowedValues, RequirementLevel, IsSystemTag, IsDomainTag, DisplayOrder
FROM [HTResourceMapper].TagDefinition
ORDER BY DisplayOrder, TagDefinitionKey

SELECT rt.TypeName, td.TagDefinitionKey, tt.IsDefaultPrimary
FROM [HTResourceMapper].ResourceTypeTag tt
INNER JOIN [HTResourceMapper].ResourceType rt ON rt.ResourceTypeId = tt.ResourceTypeId
INNER JOIN [HTResourceMapper].TagDefinition td ON td.TagDefinitionId = tt.TagDefinitionId
ORDER BY rt.TypeName, tt.IsDefaultPrimary DESC, td.TagDefinitionKey

SELECT td.TagDefinitionKey, [Rows] = COUNT(*), [Resources] = COUNT(DISTINCT rt.ResourceId)
FROM [HTResourceMapper].ResourceTag rt
INNER JOIN [HTResourceMapper].TagDefinition td ON td.TagDefinitionId = rt.TagDefinitionId
GROUP BY td.TagDefinitionKey
ORDER BY td.TagDefinitionKey

DROP TABLE #VocabAssign
DROP TABLE #VocabRes

--ROLLBACK
COMMIT

DROP PROCEDURE #SeedVocab_TagDefinition
DROP PROCEDURE #SeedVocab_TypeTag
DROP PROCEDURE #SeedVocab_TypeDefaultPrimary
