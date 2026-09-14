/*
=============================================================================================
Live_Seed_Vocabulary.sql  --  the vocabulary the import format cannot carry
=============================================================================================
Step 2 of 4:

    1. Live_Purge_Catalog.sql       clean slate
    2. Live_Seed_Vocabulary.sql     <-- you are here
    3. LiveCatalog.json             uploaded through the app's Import page (/import)
    4. Live_Seed_PostImport.sql     cross-domain edges + primary link wiring

WHY THIS EXISTS
  The import document can declare a resource type's AllowCustomTags and a tag definition's
  content type / multi-valued / allowed values -- and nothing else. It cannot express:

    * ResourceType.ShortCode and .IconKey          (ResourceType_Upsert ignores both on update)
    * TagDefinition.DisplayName, .RequirementLevel, .IsDomainTag, .IsSystemTag, .DisplayOrder
                                                   (preserved on update, but never set by import)
    * The ResourceTypeTag per-type entry-point template, which has no place in the format at all

  So the vocabulary is defined here and the resources are imported on top of it. Running this
  BEFORE the import also means import finds every type and tag already present and does not
  invent them with default metadata.

IDEMPOTENT: MERGE throughout, keyed on the natural keys (TypeName, TagDefinitionKey). Safe to
re-run, and safe to run against a database that already holds the live catalog.

IconKey is deliberately left NULL -- the explorer's icon set is not used by this catalog.
=============================================================================================
*/
USE [ResourceMapper]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

-- =========================================================================================
-- 1. Resource types
--    Eleven types: nine real Azure kinds plus the two virtual ones (System, Application) that
--    describe the logical software rather than anything deployed.
-- =========================================================================================
MERGE [HTResourceMapper].[ResourceType] AS target
USING (VALUES
     ('App Configuration', 'APC',  CAST(1 AS BIT))
    ,('App Insights',      'AI',   CAST(1 AS BIT))
    ,('App Service',       'APP',  CAST(1 AS BIT))
    ,('Redis',             'RDS',  CAST(1 AS BIT))
    ,('Managed Redis',     'RDSE', CAST(1 AS BIT))
    ,('Service Bus',       'SB',   CAST(1 AS BIT))
    ,('Service Fabric',    'SF',   CAST(1 AS BIT))
    ,('Storage Account',   'STG',  CAST(1 AS BIT))
    ,('Queue',             'Q',    CAST(1 AS BIT))
    ,('System',            'SYS',  CAST(1 AS BIT))
    ,('Application',       'APL',  CAST(1 AS BIT))
) AS source ([TypeName], [ShortCode], [AllowCustomTags])
    ON target.[TypeName] = source.[TypeName]
WHEN MATCHED THEN
    UPDATE SET
         target.[ShortCode]       = source.[ShortCode]
        ,target.[AllowCustomTags] = source.[AllowCustomTags]
        ,target.[UpdatedOn]       = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([ResourceTypeUid], [TypeName], [ShortCode], [AllowCustomTags])
    VALUES ('rt-' + LOWER(REPLACE(CAST(NEWID() AS VARCHAR(40)), '-', ''))
           ,source.[TypeName], source.[ShortCode], source.[AllowCustomTags]);

-- =========================================================================================
-- 2. Tag definitions
--    'Domain' is the single IsDomainTag row and is also owned by Script.PostDeployment1.sql.
--    It is repeated here (with identical values) so that running this script against a
--    database that has not been re-published still leaves a consistent vocabulary. Keep the
--    two in step: 'Tier' is its display name, and 'shared' is the value carried by the
--    virtual System and Application resources, which are neither prod nor non-prod.
-- =========================================================================================
MERGE [HTResourceMapper].[TagDefinition] AS target
USING (VALUES
    -- Key, DisplayName, ContentType, AllowCustomValue, IsMultiValued, AllowedValues,
    --      RequirementLevel, IsDomainTag, IsSystemTag, DisplayOrder
     ('Domain',             'Tier',                       'Text', 0, 0, '["prod","non-prod","shared"]',                                          'Error',     1, 1,  10)
    ,('Subscription',       'Subscription',               'Text', 0, 0, '["DSG-DevTestMSDN","DSG-ProdPreprod"]',                                 'Optional',  0, 0,  20)
    ,('Environment',        'Environment',                'Text', 0, 1, '["localhost","development","pentest","preprod","preview","production"]','Optional',  0, 0,  30)
    ,('ResourceGroup',      'Resource Group',             'Text', 1, 0, NULL,                                                                    'Optional',  0, 0,  40)
    ,('System',             'System',                     'Text', 0, 0, '["Visibility","Alerting","LITE"]',                                      'Optional',  0, 0,  50)
    ,('PortalUrl',          'Portal',                     'Link', 1, 0, NULL,                                                                    'Suggested', 0, 0,  60)
    ,('ExplorerUrl',        'Service Fabric Explorer',    'Link', 1, 0, NULL,                                                                    'Optional',  0, 0,  70)
    ,('KuduUrl',            'Kudu Console',               'Link', 1, 0, NULL,                                                                    'Optional',  0, 0,  80)
    ,('ServiceConsoleUrl',  'Service Console',            'Link', 1, 0, NULL,                                                                    'Optional',  0, 0,  90)
    ,('RestartPipelineUrl', 'Restart Pipeline',           'Link', 1, 0, NULL,                                                                    'Optional',  0, 0, 100)
    ,('ConfigPrefix',       'Config Key Prefix',          'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 110)
    ,('AppInsightsFilter',  'App Insights Filter',        'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 120)
    ,('DbAppName',          'Database Application Name',  'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 130)
    ,('ServiceFabricApp',   'Service Fabric Application', 'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 150)
    ,('Container',          'Blob Container',             'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 160)
    ,('Search',             'Portal Search',              'Text', 1, 0, NULL,                                                                    'Optional',  0, 0, 170)
    -- Producer/Consumer hold APPLICATION NAMES, and Live_Seed_PostImport.sql joins them onto
    -- Resource.ResourceName to build the application-to-queue edges. Keep the values spelled
    -- exactly as the Application resources are named, or those edges silently stop appearing.
    ,('Producer',           'Producer',                   'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 180)
    ,('Consumer',           'Consumer',                   'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 190)
    ,('MessageType',        'Message Type',               'Text', 1, 1, NULL,                                                                    'Optional',  0, 0, 200)
) AS v ([TagDefinitionKey], [DisplayName], [ContentType], [AllowCustomValue], [IsMultiValued],
        [AllowedValues], [RequirementLevel], [IsDomainTag], [IsSystemTag], [DisplayOrder])
-- Content type is resolved by its code, never by a hardcoded id.
INNER JOIN [HTResourceMapper].[TagContentType] ct ON ct.[TagCode] = v.[ContentType]
    ON target.[TagDefinitionKey] = v.[TagDefinitionKey]
WHEN MATCHED THEN
    UPDATE SET
         target.[DisplayName]      = v.[DisplayName]
        ,target.[TagContentTypeId] = ct.[TagContentTypeId]
        ,target.[AllowCustomValue] = CAST(v.[AllowCustomValue] AS BIT)
        ,target.[IsMultiValued]    = CAST(v.[IsMultiValued] AS BIT)
        ,target.[AllowedValues]    = v.[AllowedValues]
        ,target.[RequirementLevel] = v.[RequirementLevel]
        ,target.[IsDomainTag]      = CAST(v.[IsDomainTag] AS BIT)
        ,target.[IsSystemTag]      = CAST(v.[IsSystemTag] AS BIT)
        ,target.[DisplayOrder]     = v.[DisplayOrder]
        ,target.[UpdatedOn]        = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TagDefinitionUid], [TagDefinitionKey], [DisplayName], [TagContentTypeId]
           ,[AllowCustomValue], [IsMultiValued], [AllowedValues], [RequirementLevel]
           ,[IsDomainTag], [IsSystemTag], [DisplayOrder])
    VALUES ('td-' + LOWER(REPLACE(CAST(NEWID() AS VARCHAR(40)), '-', ''))
           ,v.[TagDefinitionKey], v.[DisplayName], ct.[TagContentTypeId]
           ,CAST(v.[AllowCustomValue] AS BIT), CAST(v.[IsMultiValued] AS BIT)
           ,v.[AllowedValues], v.[RequirementLevel]
           ,CAST(v.[IsDomainTag] AS BIT), CAST(v.[IsSystemTag] AS BIT), v.[DisplayOrder]);

-- =========================================================================================
-- 3. Per-type entry-point templates (ResourceTypeTag)
--    Which tags each type expects, and which single Link tag seeds a new resource's primary
--    click-through. At most one IsDefaultPrimary row per type (enforced by a filtered unique
--    index), so 'Portal' is the primary everywhere it exists.
--    'System' gets no template: it carries no links and no per-environment values.
-- =========================================================================================
MERGE [HTResourceMapper].[ResourceTypeTag] AS target
USING (
    SELECT
         [ResourceTypeId]   = rt.[ResourceTypeId]
        ,[TagDefinitionId]  = td.[TagDefinitionId]
        ,[IsDefaultPrimary] = CAST(v.[IsDefaultPrimary] AS BIT)
        ,[RequirementLevel] = v.[RequirementLevel]
    FROM (VALUES
        -- TypeName, TagDefinitionKey, IsDefaultPrimary, per-type RequirementLevel override
         ('App Configuration', 'PortalUrl',          1, 'Error')
        ,('App Insights',      'PortalUrl',          1, 'Error')
        ,('App Service',       'PortalUrl',          1, 'Error')
        ,('App Service',       'KuduUrl',            0, NULL)
        ,('App Service',       'ServiceConsoleUrl',  0, NULL)
        ,('App Service',       'RestartPipelineUrl', 0, NULL)
        ,('App Service',       'Environment',        0, 'Suggested')
        ,('Redis',             'PortalUrl',          1, 'Error')
        ,('Managed Redis',     'PortalUrl',          1, 'Error')
        ,('Service Bus',       'PortalUrl',          1, 'Error')
        ,('Service Bus',       'Search',             0, NULL)
        ,('Service Fabric',    'PortalUrl',          1, 'Error')
        ,('Service Fabric',    'ExplorerUrl',        0, NULL)
        -- The application types hosted on the cluster, e.g. MacropointAlerting.SF-Prod.
        ,('Service Fabric',    'ServiceFabricApp',   0, NULL)
        ,('Storage Account',   'PortalUrl',          1, 'Error')
        ,('Storage Account',   'Container',          0, NULL)
        ,('Queue',             'PortalUrl',          1, 'Error')
        ,('Queue',             'Producer',           0, NULL)
        ,('Queue',             'Consumer',           0, NULL)
        ,('Queue',             'MessageType',        0, NULL)
        ,('Application',       'System',             0, 'Error')
        ,('Application',       'ConfigPrefix',       0, 'Suggested')
        ,('Application',       'AppInsightsFilter',  0, NULL)
        ,('Application',       'DbAppName',          0, NULL)
        ,('Application',       'ServiceFabricApp',   0, NULL)
    ) AS v ([TypeName], [TagDefinitionKey], [IsDefaultPrimary], [RequirementLevel])
    INNER JOIN [HTResourceMapper].[ResourceType]  rt ON rt.[TypeName]         = v.[TypeName]
    INNER JOIN [HTResourceMapper].[TagDefinition] td ON td.[TagDefinitionKey] = v.[TagDefinitionKey]
) AS source
    ON  target.[ResourceTypeId]  = source.[ResourceTypeId]
    AND target.[TagDefinitionId] = source.[TagDefinitionId]
WHEN MATCHED THEN
    UPDATE SET
         target.[IsDefaultPrimary] = source.[IsDefaultPrimary]
        ,target.[RequirementLevel] = source.[RequirementLevel]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([ResourceTypeId], [TagDefinitionId], [IsDefaultPrimary], [RequirementLevel])
    VALUES (source.[ResourceTypeId], source.[TagDefinitionId]
           ,source.[IsDefaultPrimary], source.[RequirementLevel]);

COMMIT TRANSACTION;
GO

SELECT
     [ResourceTypes]  = (SELECT COUNT(*) FROM [HTResourceMapper].[ResourceType])
    ,[TagDefinitions] = (SELECT COUNT(*) FROM [HTResourceMapper].[TagDefinition])
    ,[TypeTemplates]  = (SELECT COUNT(*) FROM [HTResourceMapper].[ResourceTypeTag]);
GO
