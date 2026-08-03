-- Whole-catalog resource read for the JSON export. One set-based query for every resource
-- (no per-resource round-trips). Domain is the IsDomainTag=1 tag's value, joined the same way
-- Resource_GetAllKeys does, and is NULL when no domain tag is defined or applied.
CREATE PROCEDURE [HTResourceMapper].[Export_GetResources]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    SELECT
        r.ResourceId,
        r.ResourceUid,
        r.ResourceKey,
        rty.TypeName,
        r.ResourceName,
        r.[Description],
        dt.TagValue AS Domain
    FROM [HTResourceMapper].[Resource] r
    INNER JOIN [HTResourceMapper].[ResourceType] rty
        ON rty.ResourceTypeId = r.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    -- Stable ordering so two exports of an unchanged catalog are byte-identical (diffable).
    ORDER BY dt.TagValue, rty.TypeName, r.ResourceKey;
END
