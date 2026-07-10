SET QUOTED_IDENTIFIER ON;

-- FK-safe cleanup order (RD16): edges -> tags -> resources -> entry-point templates -> tag
-- definitions -> resource type. Never touches the shared domain TagDefinition (RD15). Safe to
-- run even if nothing was ever seeded (all guarded by @TypeId IS NOT NULL).

DECLARE @TypeId INT = (SELECT ResourceTypeId FROM HTResourceMapper.ResourceType WHERE TypeName = 'E2eDepType');

IF @TypeId IS NOT NULL
BEGIN
    DELETE FROM HTResourceMapper.ResourceRelationship
    WHERE FromResourceId IN (SELECT ResourceId FROM HTResourceMapper.Resource WHERE ResourceTypeId = @TypeId)
       OR ToResourceId   IN (SELECT ResourceId FROM HTResourceMapper.Resource WHERE ResourceTypeId = @TypeId);

    DELETE FROM HTResourceMapper.ResourceTag
    WHERE ResourceId IN (SELECT ResourceId FROM HTResourceMapper.Resource WHERE ResourceTypeId = @TypeId);

    DELETE FROM HTResourceMapper.Resource WHERE ResourceTypeId = @TypeId;

    DELETE FROM HTResourceMapper.ResourceTypeTag WHERE ResourceTypeId = @TypeId;

    DELETE FROM HTResourceMapper.TagDefinition WHERE TagDefinitionKey IN ('E2eOverview', 'E2eEnvironment');

    DELETE FROM HTResourceMapper.ResourceType WHERE ResourceTypeId = @TypeId;
END
