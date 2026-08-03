--/*
--=============================================
--Demo Data Purge Script for the tag vocabulary (IDEMPOTENT)
--=============================================
--Removes exactly what Demo_Insert_TagVocabulary.sql adds, and nothing else:
--  1. Resource.PrimaryTagDefinitionId pointers aimed at the demo-owned definitions
--     (FK_Resource_PrimaryTagDefinitionId is NO ACTION, so these must be cleared first).
--  2. ResourceTag rows for those definitions.
--  3. ResourceTypeTag entry-point template rows for those definitions.
--  4. The ten demo-owned TagDefinitions themselves.
--
--NOT touched (owned elsewhere):
--  - 'Domain'                 -- the system IsDomainTag = 1 row (Script.PostDeployment1.sql).
--  - 'DemoExplorerPrimaryUrl' -- owned by Demo_Insert_ExplorerGraph.sql, purged by
--                                Demo_Purge_ExplorerGraph.sql.
--  - TagContentType, ResourceType, Resource and ResourceRelationship rows.
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

DECLARE @VocabTagIds TABLE (TagDefinitionId INT NOT NULL PRIMARY KEY)

INSERT INTO @VocabTagIds (TagDefinitionId)
SELECT TagDefinitionId
FROM [HTResourceMapper].TagDefinition
WHERE TagDefinitionKey IN (N'PortalUrl', N'Repository', N'Runbook', N'Documentation',
                           N'Owner', N'OnCall', N'Tier', N'CostCenter',
                           N'Lifecycle', N'Component')
  -- belt and braces: never let a key collision drag the system tags out with it
  AND IsSystemTag = 0
  AND IsDomainTag = 0

-- 1. Clear primary-entry-point pointers (FK is NO ACTION).
UPDATE r
SET  r.PrimaryTagDefinitionId = NULL
    ,r.UpdatedOn = SYSUTCDATETIME()
FROM [HTResourceMapper].[Resource] r
INNER JOIN @VocabTagIds v ON v.TagDefinitionId = r.PrimaryTagDefinitionId

-- 2. Applied tag values.
DELETE rt
FROM [HTResourceMapper].ResourceTag rt
INNER JOIN @VocabTagIds v ON v.TagDefinitionId = rt.TagDefinitionId

-- 3. Per-type entry-point template rows.
DELETE tt
FROM [HTResourceMapper].ResourceTypeTag tt
INNER JOIN @VocabTagIds v ON v.TagDefinitionId = tt.TagDefinitionId

-- 4. The definitions.
DELETE td
FROM [HTResourceMapper].TagDefinition td
INNER JOIN @VocabTagIds v ON v.TagDefinitionId = td.TagDefinitionId

COMMIT
