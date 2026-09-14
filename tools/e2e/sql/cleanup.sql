SET QUOTED_IDENTIFIER ON;

-- FK-safe cleanup order (RD16): edges -> tags -> resources -> entry-point templates -> tag
-- definitions -> resource type. Never touches the shared domain TagDefinition (RD15). Safe to
-- run even if nothing was ever seeded (all guarded by @TypeId IS NOT NULL).

DECLARE @TypeId INT = (SELECT ResourceTypeId FROM ResourceExplorer.ResourceType WHERE TypeName = 'E2eDepType');

IF @TypeId IS NOT NULL
BEGIN
    DELETE FROM ResourceExplorer.ResourceRelationship
    WHERE FromResourceId IN (SELECT ResourceId FROM ResourceExplorer.Resource WHERE ResourceTypeId = @TypeId)
       OR ToResourceId   IN (SELECT ResourceId FROM ResourceExplorer.Resource WHERE ResourceTypeId = @TypeId);

    DELETE FROM ResourceExplorer.ResourceTag
    WHERE ResourceId IN (SELECT ResourceId FROM ResourceExplorer.Resource WHERE ResourceTypeId = @TypeId);

    DELETE FROM ResourceExplorer.Resource WHERE ResourceTypeId = @TypeId;

    DELETE FROM ResourceExplorer.ResourceTypeTag WHERE ResourceTypeId = @TypeId;

    DELETE FROM ResourceExplorer.TagDefinition WHERE TagDefinitionKey IN ('E2eOverview', 'E2eEnvironment');

    DELETE FROM ResourceExplorer.ResourceType WHERE ResourceTypeId = @TypeId;
END

-- Saved views created by saved-views.spec.js. Scoped by the E2e name prefix, NOT by owner: with a
-- single configured owner, deleting by owner would wipe the real saved views too.
DELETE FROM ResourceExplorer.SavedView WHERE Name LIKE 'E2e%';
