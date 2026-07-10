-- Returns every relationship edge touching @ResourceId, from both directions in one result
-- set: 'DependsOn' rows are this resource's out-edges (what it depends on); 'DependentOn'
-- rows are its in-edges (what depends on it). Feeds the Dependencies / Dependent On tabs and
-- the delete-confirmation dependents list. OtherDomain is the other resource's domain (a
-- ResourceTag value, joined via the IsDomainTag=1 TagDefinition — same pattern as
-- Resource_GetByResourceUid), so the tabs can show/validate same-domain without a second call.
CREATE PROCEDURE [HTResourceMapper].[ResourceRelationship_GetForResource]
    @ResourceId INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    SELECT
        rel.RelationshipId,
        'DependsOn' AS Direction,
        other.ResourceId   AS OtherResourceId,
        other.ResourceUid  AS OtherResourceUid,
        other.ResourceKey  AS OtherResourceKey,
        other.ResourceName AS OtherResourceName,
        rt.TypeName        AS OtherResourceType,
        odt.TagValue       AS OtherDomain
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.ToResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] odt
        ON odt.ResourceId = other.ResourceId AND odt.TagDefinitionId = @DomainTagDefId
    WHERE rel.FromResourceId = @ResourceId

    UNION ALL

    SELECT
        rel.RelationshipId,
        'DependentOn' AS Direction,
        other.ResourceId   AS OtherResourceId,
        other.ResourceUid  AS OtherResourceUid,
        other.ResourceKey  AS OtherResourceKey,
        other.ResourceName AS OtherResourceName,
        rt.TypeName        AS OtherResourceType,
        odt.TagValue       AS OtherDomain
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] odt
        ON odt.ResourceId = other.ResourceId AND odt.TagDefinitionId = @DomainTagDefId
    WHERE rel.ToResourceId = @ResourceId

    ORDER BY Direction, OtherResourceName;
END
