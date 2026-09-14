/*
=============================================================================================
Live_Purge_Catalog.sql  --  clean slate before seeding the live catalog
=============================================================================================
Removes the demo/sample catalog so the live seed starts from an empty board. Run this FIRST:

    1. Live_Purge_Catalog.sql       <-- you are here
    2. Live_Seed_Vocabulary.sql     resource types + tag definitions + per-type templates
    3. LiveCatalog.json             uploaded through the app's Import page (/import)
    4. Live_Seed_PostImport.sql     what the import format cannot express

WHAT IT DELETES
  Every resource, relationship, resource tag, resource type, per-type tag template, and every
  saved explorer diagram (diagrams reference resources by uid, so they are meaningless once the
  resources they point at are gone).

WHAT IT KEEPS
  [TagContentType]  -- the fixed Text/Link set, owned by Script.PostDeployment1.sql.
  [TagDefinition] rows where IsSystemTag = 1 -- currently just 'Domain', the single IsDomainTag
                     row the identity model depends on. Also owned by the post-deployment script;
                     deleting it here would leave a published database without a domain tag until
                     the next publish, and import rejects resources whose domain cannot resolve.
  [ClientSetting] -- per-browser grid preferences. Harmless, and not catalog data.

DESTRUCTIVE and NOT idempotent in the usual sense: it is a delete, so running it twice simply
finds nothing the second time. There is no undo. Intended for a development or freshly
provisioned database, not for one holding curated data you want to keep.
=============================================================================================
*/
USE [ResourceExplorer]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET XACT_ABORT ON
GO

BEGIN TRANSACTION;

    -- Order matters: every FK below is NO ACTION, so children go before parents.

    -- 1. Relationship edges reference Resource twice (From/To).
    DELETE FROM [ResourceExplorer].[ResourceRelationship];

    -- 2. Release Resource -> TagDefinition before any tag definition is deleted. This FK is the
    --    reason a "delete the tags first" ordering does not work.
    UPDATE [ResourceExplorer].[Resource]
    SET [PrimaryTagDefinitionId] = NULL
    WHERE [PrimaryTagDefinitionId] IS NOT NULL;

    -- 3. Applied tag values.
    DELETE FROM [ResourceExplorer].[ResourceTag];

    -- 4. Per-type entry-point templates (ResourceType + TagDefinition).
    DELETE FROM [ResourceExplorer].[ResourceTypeTag];

    -- 5. Saved explorer diagrams -- they hold resource uids that no longer exist.
    DELETE FROM [ResourceExplorer].[Diagram];

    -- 6. The resources themselves, then the types they referenced.
    DELETE FROM [ResourceExplorer].[Resource];
    DELETE FROM [ResourceExplorer].[ResourceType];

    -- 7. User-created tag definitions. System tags (the 'Domain' row) survive -- see header.
    DELETE FROM [ResourceExplorer].[TagDefinition]
    WHERE [IsSystemTag] = 0;

COMMIT TRANSACTION;
GO

-- Report what is left, so a run that did not do what you expected is visible immediately.
SELECT
     [Resources]        = (SELECT COUNT(*) FROM [ResourceExplorer].[Resource])
    ,[ResourceTypes]    = (SELECT COUNT(*) FROM [ResourceExplorer].[ResourceType])
    ,[TagDefinitions]   = (SELECT COUNT(*) FROM [ResourceExplorer].[TagDefinition])
    ,[Relationships]    = (SELECT COUNT(*) FROM [ResourceExplorer].[ResourceRelationship])
    ,[ResourceTags]     = (SELECT COUNT(*) FROM [ResourceExplorer].[ResourceTag])
    ,[TypeTemplates]    = (SELECT COUNT(*) FROM [ResourceExplorer].[ResourceTypeTag])
    ,[Diagrams]         = (SELECT COUNT(*) FROM [ResourceExplorer].[Diagram]);
GO
