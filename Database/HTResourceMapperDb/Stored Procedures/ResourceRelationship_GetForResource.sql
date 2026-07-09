-- Returns every relationship edge touching @ResourceId, from both directions in one result
-- set: 'DependsOn' rows are this resource's out-edges (what it depends on); 'DependentOn'
-- rows are its in-edges (what depends on it). Feeds the Dependencies / Dependent On tabs and
-- the delete-confirmation dependents list.
CREATE PROCEDURE [HTResourceMapper].[ResourceRelationship_GetForResource]
    @ResourceId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rel.RelationshipId,
        'DependsOn' AS Direction,
        other.ResourceId   AS OtherResourceId,
        other.ResourceUid  AS OtherResourceUid,
        other.ResourceKey  AS OtherResourceKey,
        other.ResourceName AS OtherResourceName,
        rt.TypeName        AS OtherResourceType
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.ToResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    WHERE rel.FromResourceId = @ResourceId

    UNION ALL

    SELECT
        rel.RelationshipId,
        'DependentOn' AS Direction,
        other.ResourceId   AS OtherResourceId,
        other.ResourceUid  AS OtherResourceUid,
        other.ResourceKey  AS OtherResourceKey,
        other.ResourceName AS OtherResourceName,
        rt.TypeName        AS OtherResourceType
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    WHERE rel.ToResourceId = @ResourceId

    ORDER BY Direction, OtherResourceName;
END
