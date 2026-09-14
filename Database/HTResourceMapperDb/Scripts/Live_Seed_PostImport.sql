/*
=============================================================================================
Live_Seed_PostImport.sql  --  what the import format cannot express
=============================================================================================
Step 4 of 4, run AFTER LiveCatalog.json has been uploaded through the app's Import page:

    1. Live_Purge_Catalog.sql       clean slate
    2. Live_Seed_Vocabulary.sql     resource types + tag definitions + per-type templates
    3. LiveCatalog.json             uploaded through /import
    4. Live_Seed_PostImport.sql     <-- you are here

THREE JOBS, all of them gaps in the import contract rather than anything exotic.

  (a) CROSS-DOMAIN DEPENDENCY EDGES
      ImportService resolves every dependency key inside the DEPENDENT's own domain
      (ValidateAndResolveDependencies). An Application is domain 'shared' and an App Service is
      'prod' or 'non-prod', so an Application -> App Service edge cannot be expressed in the
      JSON at all -- it is not a warning, it fails the whole import. The JSON therefore carries
      only the same-domain Application -> System edges, and the cross-domain ones are added here.

  (b) APPLICATION <-> QUEUE EDGES
      The same cross-domain problem, but derived from the queues' own Producer and Consumer
      tags rather than a hardcoded list, so adding a queue to LiveCatalog.json is enough for
      its edges to appear. A diagnostic query reports any tag value that matched no
      application, which is how spelling drift between tag and catalog becomes visible.

  (c) PRIMARY CLICK-THROUGH LINK
      Neither Resource_Upsert nor ImportService sets Resource.PrimaryTagDefinitionId, so every
      imported resource lands with no primary link and the grid has nothing to click through to.
      This wires each resource to the IsDefaultPrimary tag of its type -- but only where the
      resource actually carries a value for that tag, so nothing points at an empty link.

IDEMPOTENT: every section is guarded against re-insert, and the unique constraint on
(FromResourceId, ToResourceId) is respected rather than relied upon.
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
-- (a) Application -> App Service edges. From = dependent (the application), To = dependency
--     (the site it runs on). Matched on ResourceKey, which is unique per (Domain, Type, Key);
--     app service keys are globally unique here, so the join needs no domain predicate.
-- =========================================================================================
;WITH [Edges] AS (
    SELECT * FROM (VALUES
         ('vnext',       'usspi-aps-dt-mplt-dev-visibility-macropoint-com')
        ,('vnext',       'usspi-aps-pp-mplt-preprod-visibility-macropoint-com')
        ,('vnext',       'usspi-aps-pd-mplt-visibility-macropoint-com')
        ,('vnext-proxy', 'usspi-aps-dt-mplt-dev-vnextproxy-macropoint-com')
        ,('vnext-proxy', 'usspi-aps-pp-mplt-preprod-vnextproxy-macropoint-com')
        ,('vnext-proxy', 'usspi-aps-pd-mplt-vnextproxy-macropoint-com')
    ) AS e ([FromKey], [ToKey])
)
INSERT INTO [HTResourceMapper].[ResourceRelationship] ([FromResourceId], [ToResourceId])
SELECT rFrom.[ResourceId], rTo.[ResourceId]
FROM [Edges] e
INNER JOIN [HTResourceMapper].[Resource] rFrom ON rFrom.[ResourceKey] = e.[FromKey]
INNER JOIN [HTResourceMapper].[Resource] rTo   ON rTo.[ResourceKey]   = e.[ToKey]
WHERE rFrom.[ResourceId] <> rTo.[ResourceId]
  AND NOT EXISTS (
      SELECT 1 FROM [HTResourceMapper].[ResourceRelationship] x
      WHERE x.[FromResourceId] = rFrom.[ResourceId]
        AND x.[ToResourceId]   = rTo.[ResourceId]);

-- =========================================================================================
-- (b) Application <-> Queue edges, derived from the queues' own Producer and Consumer tags
--     rather than a hardcoded list -- add a queue to LiveCatalog.json and its edges appear
--     here automatically. Cross-domain again (Application is 'shared', a queue is prod or
--     non-prod), so import cannot carry these either.
--
--     Direction: From = the application, To = the queue. ResourceRelationship is untyped, so
--     the edge cannot say which way messages flow -- that is what the Producer and Consumer
--     tags are for. A queue name exists in both tiers, so an application links to both.
-- =========================================================================================
INSERT INTO [HTResourceMapper].[ResourceRelationship] ([FromResourceId], [ToResourceId])
SELECT DISTINCT app.[ResourceId], q.[ResourceId]
FROM [HTResourceMapper].[ResourceTag] rt
INNER JOIN [HTResourceMapper].[TagDefinition] td
        ON td.[TagDefinitionId] = rt.[TagDefinitionId]
       AND td.[TagDefinitionKey] IN ('Producer', 'Consumer')
INNER JOIN [HTResourceMapper].[Resource] q   ON q.[ResourceId] = rt.[ResourceId]
INNER JOIN [HTResourceMapper].[Resource] app ON app.[ResourceName] = rt.[TagValue]
INNER JOIN [HTResourceMapper].[ResourceType] appType
        ON appType.[ResourceTypeId] = app.[ResourceTypeId]
       AND appType.[TypeName] = 'Application'
WHERE app.[ResourceId] <> q.[ResourceId]
  AND NOT EXISTS (
      SELECT 1 FROM [HTResourceMapper].[ResourceRelationship] x
      WHERE x.[FromResourceId] = app.[ResourceId]
        AND x.[ToResourceId]   = q.[ResourceId]);

-- Any Producer/Consumer value that matched no Application is a spelling drift between the tag
-- and the catalog, and means a missing edge. Empty result = every value resolved.
SELECT DISTINCT
     [UnmatchedApplication] = rt.[TagValue]
    ,[TagKey]               = td.[TagDefinitionKey]
FROM [HTResourceMapper].[ResourceTag] rt
INNER JOIN [HTResourceMapper].[TagDefinition] td
        ON td.[TagDefinitionId] = rt.[TagDefinitionId]
       AND td.[TagDefinitionKey] IN ('Producer', 'Consumer')
WHERE NOT EXISTS (
    SELECT 1
    FROM [HTResourceMapper].[Resource] app
    INNER JOIN [HTResourceMapper].[ResourceType] t ON t.[ResourceTypeId] = app.[ResourceTypeId]
    WHERE app.[ResourceName] = rt.[TagValue] AND t.[TypeName] = 'Application');

-- =========================================================================================
-- (c) Primary click-through link, from each type's IsDefaultPrimary template entry. Only set
--     where the resource actually has a value for that tag; left NULL otherwise.
-- =========================================================================================
UPDATE r
SET [PrimaryTagDefinitionId] = rtt.[TagDefinitionId]
FROM [HTResourceMapper].[Resource] r
INNER JOIN [HTResourceMapper].[ResourceTypeTag] rtt
        ON rtt.[ResourceTypeId] = r.[ResourceTypeId]
       AND rtt.[IsDefaultPrimary] = 1
WHERE (r.[PrimaryTagDefinitionId] IS NULL OR r.[PrimaryTagDefinitionId] <> rtt.[TagDefinitionId])
  AND EXISTS (
      SELECT 1 FROM [HTResourceMapper].[ResourceTag] rt
      WHERE rt.[ResourceId]       = r.[ResourceId]
        AND rt.[TagDefinitionId]  = rtt.[TagDefinitionId]
        AND NULLIF(LTRIM(RTRIM(rt.[TagValue])), '') IS NOT NULL);

COMMIT TRANSACTION;
GO

-- Anything listed here has no click-through link: either its type has no default primary
-- (System, Application) or it is missing the tag the template points at.
SELECT
     [Type] = rt.[TypeName]
    ,[Name] = r.[ResourceName]
FROM [HTResourceMapper].[Resource] r
INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.[ResourceTypeId] = r.[ResourceTypeId]
WHERE r.[PrimaryTagDefinitionId] IS NULL
ORDER BY rt.[TypeName], r.[ResourceName];
GO

SELECT
     [Resources]     = (SELECT COUNT(*) FROM [HTResourceMapper].[Resource])
    ,[Relationships] = (SELECT COUNT(*) FROM [HTResourceMapper].[ResourceRelationship])
    ,[WithPrimary]   = (SELECT COUNT(*) FROM [HTResourceMapper].[Resource]
                        WHERE [PrimaryTagDefinitionId] IS NOT NULL);
GO
