-- Explorer canvas read: the center resource (@ResourceUid) plus its one-hop neighbours, in a
-- single result set discriminated by Direction:
--   'Self'        = the resource itself
--   'DependsOn'   = a one-hop neighbour reached by an out-edge (what the center depends on)
--   'DependentOn' = a one-hop neighbour reached by an in-edge (what depends on the center)
--   'Edge'        = a relationship whose BOTH endpoints are on the canvas. Node columns are
--                   blank/NULL on these rows; FromResourceUid / ToResourceUid carry the endpoints.
-- Node rows carry the node's Domain (IsDomainTag tag value) and PrimaryUrl (the resource's primary
-- Link tag value, via PrimaryTagDefinitionId) so the canvas can render node content + the external
-- link without extra calls. Uid-keyed because the canvas never handles internal int ids.
-- Unknown uid returns an empty set (no 'Self' row) -> the service treats it as NotFound.
--
-- The 'Edge' rows exist because a "center + one-hop neighbours" read that only returns edges
-- incident to the center silently drops relationships BETWEEN two neighbours: both endpoints are
-- drawn, but the line between them never is, so the picture asserts they are unrelated. Edge rows
-- cover every relationship inside the on-canvas set, which is the center + its neighbours plus
-- @KnownResourceUids -- the comma-separated uids the caller already has drawn. Passing the known
-- set also backfills edges to nodes that were placed by an earlier expansion, and is idempotent
-- (the canvas skips edges it already holds).
CREATE PROCEDURE [HTResourceMapper].[Resource_GetForExplorer]
    @ResourceUid       VARCHAR(40),
    @KnownResourceUids NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResourceId INT =
        (SELECT ResourceId FROM [HTResourceMapper].[Resource] WHERE ResourceUid = @ResourceUid);

    IF @ResourceId IS NULL
        RETURN;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- Everything that will be on the canvas after this read. Scopes the 'Edge' rows only -- the
    -- node rows below remain the center + its one-hop neighbours. STRING_SPLIT over a NULL input
    -- yields no rows, so an omitted @KnownResourceUids simply narrows the scope to this hop.
    DECLARE @Scope TABLE (ResourceId INT NOT NULL PRIMARY KEY);

    INSERT INTO @Scope (ResourceId)
    SELECT @ResourceId
    UNION
    SELECT rel.ToResourceId
    FROM [HTResourceMapper].[ResourceRelationship] rel
    WHERE rel.FromResourceId = @ResourceId
    UNION
    SELECT rel.FromResourceId
    FROM [HTResourceMapper].[ResourceRelationship] rel
    WHERE rel.ToResourceId = @ResourceId
    UNION
    SELECT known.ResourceId
    FROM [HTResourceMapper].[Resource] known
    INNER JOIN STRING_SPLIT(@KnownResourceUids, ',') split
        ON known.ResourceUid = CAST(LTRIM(RTRIM(split.[value])) AS VARCHAR(40));

    -- Center node
    SELECT
        'Self' AS Direction,
        r.ResourceUid,
        r.ResourceKey,
        r.ResourceName,
        rt.TypeName    AS ResourceType,
        rt.ShortCode   AS ShortCode,
        rt.IconKey     AS IconKey,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl,
        CAST(NULL AS VARCHAR(40)) AS FromResourceUid,
        CAST(NULL AS VARCHAR(40)) AS ToResourceUid
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
        rt.ShortCode   AS ShortCode,
        rt.IconKey     AS IconKey,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl,
        CAST(NULL AS VARCHAR(40)) AS FromResourceUid,
        CAST(NULL AS VARCHAR(40)) AS ToResourceUid
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
        rt.ShortCode   AS ShortCode,
        rt.IconKey     AS IconKey,
        dt.TagValue    AS Domain,
        purl.TagValue  AS PrimaryUrl,
        CAST(NULL AS VARCHAR(40)) AS FromResourceUid,
        CAST(NULL AS VARCHAR(40)) AS ToResourceUid
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] other ON other.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[ResourceType] rt ON rt.ResourceTypeId = other.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = other.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] purl
        ON purl.ResourceId = other.ResourceId AND purl.TagDefinitionId = other.PrimaryTagDefinitionId
    WHERE rel.ToResourceId = @ResourceId

    UNION ALL

    -- Every relationship with BOTH endpoints on the canvas, including neighbour-to-neighbour ones
    -- that no center-incident query would ever return.
    SELECT
        'Edge' AS Direction,
        CAST('' AS VARCHAR(40))    AS ResourceUid,
        CAST('' AS NVARCHAR(250))  AS ResourceKey,
        CAST('' AS NVARCHAR(250))  AS ResourceName,
        CAST('' AS NVARCHAR(250))  AS ResourceType,
        CAST(NULL AS VARCHAR(10))   AS ShortCode,
        CAST(NULL AS VARCHAR(40))   AS IconKey,
        CAST(NULL AS NVARCHAR(2000)) AS Domain,
        CAST(NULL AS NVARCHAR(2000)) AS PrimaryUrl,
        rfrom.ResourceUid AS FromResourceUid,
        rto.ResourceUid   AS ToResourceUid
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN @Scope scopeFrom ON scopeFrom.ResourceId = rel.FromResourceId
    INNER JOIN @Scope scopeTo   ON scopeTo.ResourceId   = rel.ToResourceId
    INNER JOIN [HTResourceMapper].[Resource] rfrom ON rfrom.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[Resource] rto   ON rto.ResourceId   = rel.ToResourceId

    ORDER BY Direction, ResourceName;
END
