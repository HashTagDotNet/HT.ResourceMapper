--/*
--=============================================
--Demo Data Purge Script for Resource Explorer graph (IDEMPOTENT)
--=============================================
--Removes exactly what Demo_Insert_ExplorerGraph.sql adds: the DEMOEXP-* resources,
--their relationships and tags, the demo-owned resource types, and the demo-owned
--primary-URL TagDefinition. Leaves everything else untouched -- in particular the
--system Domain TagDefinition (TagDefinitionKey = 'Domain') and TagContentType rows
--are NOT touched, since this script does not own them.
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

DELETE FROM [HTResourceMapper].TagDefinition
WHERE TagDefinitionKey = 'DemoExplorerPrimaryUrl'

COMMIT
