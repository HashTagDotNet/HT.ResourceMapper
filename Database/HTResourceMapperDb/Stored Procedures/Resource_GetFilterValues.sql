CREATE PROCEDURE [HTResourceMapper].[Resource_GetFilterValues]
    @FacetColumn NVARCHAR(50),                 -- 'ResourceType' | 'Tag'
    @FacetTagKey NVARCHAR(50) = NULL,          -- required when @FacetColumn = 'Tag'
    @SearchFor   NVARCHAR(255) = NULL,         -- applied to counts (counts match the searched grid)
    @Filters     [HTResourceMapper].[ResourceFilterList] READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- Escape LIKE special characters for the free-text search (identical to Resource_GetItems)
    DECLARE @SafeSearchFor NVARCHAR(255) = NULL;
    IF @SearchFor IS NOT NULL AND LTRIM(RTRIM(@SearchFor)) <> ''
        SET @SafeSearchFor = '%' +
            REPLACE(REPLACE(REPLACE(REPLACE(@SearchFor, '[', '[[]'), ']', '[]]'), '_', '[_]'), '%', '[%]') + '%';

    -- All active filters EXCEPT this facet's own dimension (Column, and TagKey when it's a tag facet).
    -- (The C# service also strips it; this is belt-and-suspenders so a facet never constrains itself.)
    DECLARE @F [HTResourceMapper].[ResourceFilterList];
    INSERT INTO @F (FilterIndex, FilterColumn, TagKey, Operator, FilterValue, IsBlank)
    SELECT FilterIndex, FilterColumn, TagKey, Operator, FilterValue, IsBlank
    FROM @Filters
    WHERE NOT (
        FilterColumn = @FacetColumn
        AND (@FacetColumn <> N'Tag' OR ISNULL(TagKey, N'') = ISNULL(@FacetTagKey, N''))
    );

    IF @FacetColumn = N'ResourceType'
    BEGIN
        SELECT
            CAST(CASE WHEN r.ResourceTypeId IS NULL THEN 1 ELSE 0 END AS BIT) AS IsBlank,
            MAX(rt_type.TypeName) AS Value,
            COUNT(DISTINCT r.ResourceId) AS ItemCount
        FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
        LEFT JOIN [HTResourceMapper].[ResourceType] rt_type WITH(NOLOCK) ON r.ResourceTypeId = rt_type.ResourceTypeId
        WHERE 1 = 1
            AND (
                @SafeSearchFor IS NULL
                OR r.ResourceKey LIKE @SafeSearchFor ESCAPE ']'
                OR r.ResourceName LIKE @SafeSearchFor ESCAPE ']'
                OR r.Description LIKE @SafeSearchFor ESCAPE ']'
                OR EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceTag] rts
                           INNER JOIN [HTResourceMapper].[TagDefinition] tds ON rts.TagDefinitionId = tds.TagDefinitionId
                           WHERE rts.ResourceId = r.ResourceId
                           AND (rts.TagValue LIKE @SafeSearchFor ESCAPE ']' OR tds.TagDefinitionKey LIKE @SafeSearchFor ESCAPE ']'))
            )
            -- Tag filters (per-key AND, value OR)
            AND NOT EXISTS (
                SELECT 1 FROM (SELECT DISTINCT TagKey FROM @F WHERE FilterColumn = N'Tag' AND Operator = N'Equals') g
                WHERE NOT EXISTS (
                    SELECT 1 FROM [HTResourceMapper].[ResourceTag] rte
                    INNER JOIN [HTResourceMapper].[TagDefinition] tde ON rte.TagDefinitionId = tde.TagDefinitionId
                    INNER JOIN @F f ON f.FilterColumn = N'Tag' AND f.Operator = N'Equals' AND f.TagKey = g.TagKey
                                AND (f.FilterValue = rte.TagValue OR (f.IsBlank = 1 AND rte.TagValue IS NULL))
                    WHERE rte.ResourceId = r.ResourceId AND tde.TagDefinitionKey = g.TagKey))
            AND NOT EXISTS (
                SELECT 1 FROM [HTResourceMapper].[ResourceTag] rtn
                INNER JOIN [HTResourceMapper].[TagDefinition] tdn ON rtn.TagDefinitionId = tdn.TagDefinitionId
                INNER JOIN @F f ON f.FilterColumn = N'Tag' AND f.Operator = N'NotEquals' AND f.TagKey = tdn.TagDefinitionKey
                            AND (f.FilterValue = rtn.TagValue OR (f.IsBlank = 1 AND rtn.TagValue IS NULL))
                WHERE rtn.ResourceId = r.ResourceId)
            -- Text filters (contains)
            AND (NOT EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceName' AND f.Operator = N'Contains')
                 OR EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceName' AND f.Operator = N'Contains'
                            AND r.ResourceName LIKE N'%' + REPLACE(REPLACE(REPLACE(REPLACE(f.FilterValue, '[', '[[]'), ']', '[]]'), '_', '[_]'), '%', '[%]') + N'%' ESCAPE ']'))
            AND (NOT EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'Description' AND f.Operator = N'Contains')
                 OR EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'Description' AND f.Operator = N'Contains'
                            AND r.Description LIKE N'%' + REPLACE(REPLACE(REPLACE(REPLACE(f.FilterValue, '[', '[[]'), ']', '[]]'), '_', '[_]'), '%', '[%]') + N'%' ESCAPE ']'))
        GROUP BY r.ResourceTypeId
        ORDER BY IsBlank ASC, ItemCount DESC, Value ASC;
    END
    ELSE IF @FacetColumn = N'Tag'
    BEGIN
        SELECT
            CAST(CASE WHEN rtf0.TagValue IS NULL THEN 1 ELSE 0 END AS BIT) AS IsBlank,
            rtf0.TagValue AS Value,
            COUNT(DISTINCT r.ResourceId) AS ItemCount
        FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
        LEFT JOIN [HTResourceMapper].[ResourceType] rt_type WITH(NOLOCK) ON r.ResourceTypeId = rt_type.ResourceTypeId
        INNER JOIN [HTResourceMapper].[ResourceTag] rtf0 WITH(NOLOCK) ON rtf0.ResourceId = r.ResourceId
        INNER JOIN [HTResourceMapper].[TagDefinition] tdf0 WITH(NOLOCK) ON rtf0.TagDefinitionId = tdf0.TagDefinitionId
        WHERE tdf0.TagDefinitionKey = @FacetTagKey
            AND (
                @SafeSearchFor IS NULL
                OR r.ResourceKey LIKE @SafeSearchFor ESCAPE ']'
                OR r.ResourceName LIKE @SafeSearchFor ESCAPE ']'
                OR r.Description LIKE @SafeSearchFor ESCAPE ']'
                OR EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceTag] rts
                           INNER JOIN [HTResourceMapper].[TagDefinition] tds ON rts.TagDefinitionId = tds.TagDefinitionId
                           WHERE rts.ResourceId = r.ResourceId
                           AND (rts.TagValue LIKE @SafeSearchFor ESCAPE ']' OR tds.TagDefinitionKey LIKE @SafeSearchFor ESCAPE ']'))
            )
            -- ResourceType filters
            AND (NOT EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceType' AND f.Operator = N'Equals')
                 OR rt_type.TypeName IN (SELECT f.FilterValue FROM @F f WHERE f.FilterColumn = N'ResourceType' AND f.Operator = N'Equals' AND f.IsBlank = 0)
                 OR (EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceType' AND f.Operator = N'Equals' AND f.IsBlank = 1) AND r.ResourceTypeId IS NULL))
            AND NOT EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceType' AND f.Operator = N'NotEquals'
                            AND (f.FilterValue = rt_type.TypeName OR (f.IsBlank = 1 AND r.ResourceTypeId IS NULL)))
            -- Other tag-key filters (per-key AND, value OR)
            AND NOT EXISTS (
                SELECT 1 FROM (SELECT DISTINCT TagKey FROM @F WHERE FilterColumn = N'Tag' AND Operator = N'Equals') g
                WHERE NOT EXISTS (
                    SELECT 1 FROM [HTResourceMapper].[ResourceTag] rte
                    INNER JOIN [HTResourceMapper].[TagDefinition] tde ON rte.TagDefinitionId = tde.TagDefinitionId
                    INNER JOIN @F f ON f.FilterColumn = N'Tag' AND f.Operator = N'Equals' AND f.TagKey = g.TagKey
                                AND (f.FilterValue = rte.TagValue OR (f.IsBlank = 1 AND rte.TagValue IS NULL))
                    WHERE rte.ResourceId = r.ResourceId AND tde.TagDefinitionKey = g.TagKey))
            AND NOT EXISTS (
                SELECT 1 FROM [HTResourceMapper].[ResourceTag] rtn
                INNER JOIN [HTResourceMapper].[TagDefinition] tdn ON rtn.TagDefinitionId = tdn.TagDefinitionId
                INNER JOIN @F f ON f.FilterColumn = N'Tag' AND f.Operator = N'NotEquals' AND f.TagKey = tdn.TagDefinitionKey
                            AND (f.FilterValue = rtn.TagValue OR (f.IsBlank = 1 AND rtn.TagValue IS NULL))
                WHERE rtn.ResourceId = r.ResourceId)
            -- Text filters (contains)
            AND (NOT EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceName' AND f.Operator = N'Contains')
                 OR EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'ResourceName' AND f.Operator = N'Contains'
                            AND r.ResourceName LIKE N'%' + REPLACE(REPLACE(REPLACE(REPLACE(f.FilterValue, '[', '[[]'), ']', '[]]'), '_', '[_]'), '%', '[%]') + N'%' ESCAPE ']'))
            AND (NOT EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'Description' AND f.Operator = N'Contains')
                 OR EXISTS (SELECT 1 FROM @F f WHERE f.FilterColumn = N'Description' AND f.Operator = N'Contains'
                            AND r.Description LIKE N'%' + REPLACE(REPLACE(REPLACE(REPLACE(f.FilterValue, '[', '[[]'), ']', '[]]'), '_', '[_]'), '%', '[%]') + N'%' ESCAPE ']'))
        GROUP BY rtf0.TagValue
        ORDER BY IsBlank ASC, ItemCount DESC, Value ASC;
    END
END
