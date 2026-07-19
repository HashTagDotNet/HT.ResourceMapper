-- Explorer canvas read: the center resource (@ResourceUid) plus its one-hop neighbours, in a
-- single result set discriminated by Direction: 'Self' = the resource itself; 'DependsOn' = an
-- out-edge (what it depends on); 'DependentOn' = an in-edge (what depends on it). Each row also
-- carries the node's Domain (IsDomainTag tag value) and PrimaryUrl (the resource's primary Link
-- tag value, via PrimaryTagDefinitionId) so the canvas can render node content + the external
-- link without extra calls. Uid-keyed because the canvas never handles internal int ids.
-- Unknown uid returns an empty set (no 'Self' row) -> the service treats it as NotFound.
CREATE PROCEDURE [HTResourceMapper].[Resource_GetForExplorer]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @ResourceUid);

    IF @ResourceId IS NULL
        RETURN;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- Center node
    SELECT
        'Self' AS Direction,
        r.ResourceUid,
        r.ResourceKey,
        r.ResourceName,
        rt.TypeName    AS ResourceType,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[Resource] r
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = r.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = r.ResourceId AND purl.TagDefinitionId = r.PrimaryTagDefinitionId
    WHERE r.ResourceId = @ResourceId

    UNION ALL

    -- Out-edges: what this resource depends on
    SELECT
        'DependsOn' AS Direction,
        other.ResourceUid,
        other.ResourceKey,
        other.ResourceName,
        rt.TypeName    AS ResourceType,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.ToResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.FromResourceId = @ResourceId

    UNION ALL

    -- In-edges: what depends on this resource
    SELECT
        'DependentOn' AS Direction,
        other.ResourceUid,
        other.ResourceKey,
        other.ResourceName,
        rt.TypeName    AS ResourceType,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.ToResourceId = @ResourceId

    ORDER BY Direction, ResourceName;
END
