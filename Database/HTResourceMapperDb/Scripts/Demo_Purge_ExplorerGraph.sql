--/*
--=============================================
--Demo Data Purge Script for Resource Explorer graph (IDEMPOTENT)
--=============================================
--Removes exactly what Demo_Insert_ExplorerGraph.sql adds: the DEMOEXP-* resources,
--their relationships and tags, and the demo-owned resource types. Leaves everything
--else untouched -- in particular the system Domain TagDefinition (TagDefinitionKey =
--'Domain'), the shared 'PortalUrl' TagDefinition (owned by Demo_Insert_TagVocabulary.sql,
--purged by Demo_Purge_TagVocabulary.sql) and TagContentType rows are NOT touched, since
--this script does not own them.
--
--IDEMPOTENT: safe to run multiple times / when nothing is left to remove.
--=============================================
--*/
USE [ResourceMapper]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

BEGIN TRANSACTION

DELETE rel
FROM [HTResourceMapper].ResourceRelationship rel
INNER JOIN [HTResourceMapper].[Resource] r
    ON r.ResourceId = rel.FromResourceId OR r.ResourceId = rel.ToResourceId
WHERE r.ResourceUid LIKE 'DEMOEXP-%'

DELETE rt
FROM [HTResourceMapper].ResourceTag rt
INNER JOIN [HTResourceMapper].[Resource] r
    ON r.ResourceId = rt.ResourceId
WHERE r.ResourceUid LIKE 'DEMOEXP-%'

DELETE FROM [HTResourceMapper].[Resource]
WHERE ResourceUid LIKE 'DEMOEXP-%'

DELETE FROM [HTResourceMapper].ResourceType
WHERE ResourceTypeUid LIKE 'DEMOEXP-%'

-- PL-46 legacy sweep: this script used to own 'DemoExplorerPrimaryUrl'. The insert script now
-- migrates that row to 'PortalUrl' and drops it, so this is a no-op on a current database --
-- kept so purging an older one still leaves no retired definition behind. Deliberately NOT a
-- delete of 'PortalUrl': that definition is vocabulary-owned.
DELETE FROM [HTResourceMapper].TagDefinition
WHERE TagDefinitionKey = 'DemoExplorerPrimaryUrl'

COMMIT
