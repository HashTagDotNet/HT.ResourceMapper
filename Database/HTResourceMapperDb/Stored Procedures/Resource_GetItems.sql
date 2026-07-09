CREATE PROCEDURE [HTResourceMapper].[Resource_GetItems]
    @SearchFor NVARCHAR(255) = NULL,
    @OrderBy NVARCHAR(50) = NULL,
    @OrderDirection NVARCHAR(4) = NULL,
    @Skip INT = 0,
    @Take INT = 50,
    @TagLimit INT = 5,
    @Filters [HTResourceMapper].[ResourceFilterList] READONLY,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Validate and set defaults for pagination parameters
    SET @Skip = ISNULL(@Skip, 0);
    SET @Take = ISNULL(@Take, 50);

    -- Ensure Skip is not negative
    IF @Skip < 0 SET @Skip = 0;

    -- Ensure Take is within reasonable bounds
    IF @Take <= 0 SET @Take = 50;
    IF @Take > 1000 SET @Take = 1000; -- Prevent excessive data retrieval

    -- Clamp the per-resource tag limit (applied in SQL so a large page never returns an
    -- unbounded denormalized rowset).
    SET @TagLimit = ISNULL(@TagLimit, 5);
    IF @TagLimit < 1 SET @TagLimit = 1;

    -- Validate and set defaults for ordering
    SET @OrderDirection = ISNULL(@OrderDirection, 'ASC');
    IF @OrderDirection NOT IN ('ASC', 'DESC')
        SET @OrderDirection = 'ASC';

    -- Escape LIKE special characters and prepare search parameter
    DECLARE @SafeSearchFor NVARCHAR(255) = NULL;
    IF @SearchFor IS NOT NULL AND LTRIM(RTRIM(@SearchFor)) <> ''
    BEGIN
        -- Escape LIKE special characters: %, _, [, ]
        SET @SafeSearchFor = '%' +
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(@SearchFor, '[', '[[]'),
                    ']', '[]]'),
                '_', '[_]'),
            '%', '[%]') + '%';
    END

    -- Build the main query for resources with tags
    DECLARE @sql NVARCHAR(MAX);
    DECLARE @whereClause NVARCHAR(MAX) = N'WHERE 1 = 1';
    DECLARE @orderClause NVARCHAR(MAX) = '';
    DECLARE @priorityLogic NVARCHAR(MAX) = '';
    DECLARE @orderByLogic NVARCHAR(MAX) = '';

    -- Free-text search block (contains across key/name/description/tags) - parameterized
    IF @SafeSearchFor IS NOT NULL
    BEGIN
        SET @whereClause = @whereClause + '
        AND (
            r.ResourceKey LIKE @SafeSearchForParam ESCAPE '']''
            OR r.ResourceName LIKE @SafeSearchForParam ESCAPE '']''
            OR r.Description LIKE @SafeSearchForParam ESCAPE '']''
            OR EXISTS (
                SELECT 1 FROM [HTResourceMapper].[ResourceTag] rt2
                INNER JOIN [HTResourceMapper].[TagDefinition] td2 ON rt2.TagDefinitionId = td2.TagDefinitionId
                WHERE rt2.ResourceId = r.ResourceId
                AND (rt2.TagValue LIKE @SafeSearchForParam ESCAPE '']'' OR td2.TagDefinitionKey LIKE @SafeSearchForParam ESCAPE '']'')
            )
        )';

        SET @priorityLogic = '
            CASE
                WHEN @SafeSearchForParam IS NOT NULL AND (
                    rt.TagValue LIKE @SafeSearchForParam ESCAPE '']''
                    OR td.TagDefinitionKey LIKE @SafeSearchForParam ESCAPE '']'')
                THEN 0  -- Matching tags get priority 0 (highest)
                ELSE 1  -- Non-matching tags get priority 1
            END';

        SET @orderByLogic = '
                    CASE
                        WHEN @SafeSearchForParam IS NOT NULL AND (
                            rt.TagValue LIKE @SafeSearchForParam ESCAPE '']''
                            OR td.TagDefinitionKey LIKE @SafeSearchForParam ESCAPE '']'')
                        THEN 0
                        ELSE 1
                    END,
                    td.TagDefinitionKey,
                    rt.TagValue';
    END
    ELSE
    BEGIN
        SET @priorityLogic = '1'; -- All tags have same priority when no search
        SET @orderByLogic = 'td.TagDefinitionKey, rt.TagValue';
    END

    -- Structured filter block (AND across filters; OR within an enumerable filter).
    -- Every predicate is CONSTANT text that only references the @FiltersParam TVP - no user
    -- value or identifier is concatenated, so there is no injection surface.
    DECLARE @filterClause NVARCHAR(MAX) = N'
        AND ( NOT EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''ResourceType'' AND f.Operator = N''Equals'')
              OR rt_type.TypeName IN (SELECT f.FilterValue FROM @FiltersParam f WHERE f.FilterColumn = N''ResourceType'' AND f.Operator = N''Equals'' AND f.IsBlank = 0)
              OR (EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''ResourceType'' AND f.Operator = N''Equals'' AND f.IsBlank = 1) AND r.ResourceTypeId IS NULL) )
        AND NOT EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''ResourceType'' AND f.Operator = N''NotEquals''
                        AND ( f.FilterValue = rt_type.TypeName OR (f.IsBlank = 1 AND r.ResourceTypeId IS NULL) ))
        AND NOT EXISTS (
            SELECT 1 FROM (SELECT DISTINCT TagKey FROM @FiltersParam WHERE FilterColumn = N''Tag'' AND Operator = N''Equals'') g
            WHERE NOT EXISTS (
                SELECT 1 FROM [HTResourceMapper].[ResourceTag] rte
                INNER JOIN [HTResourceMapper].[TagDefinition] tde ON rte.TagDefinitionId = tde.TagDefinitionId
                INNER JOIN @FiltersParam f ON f.FilterColumn = N''Tag'' AND f.Operator = N''Equals'' AND f.TagKey = g.TagKey
                            AND ( f.FilterValue = rte.TagValue OR (f.IsBlank = 1 AND rte.TagValue IS NULL) )
                WHERE rte.ResourceId = r.ResourceId AND tde.TagDefinitionKey = g.TagKey ) )
        AND NOT EXISTS (
            SELECT 1 FROM [HTResourceMapper].[ResourceTag] rtn
            INNER JOIN [HTResourceMapper].[TagDefinition] tdn ON rtn.TagDefinitionId = tdn.TagDefinitionId
            INNER JOIN @FiltersParam f ON f.FilterColumn = N''Tag'' AND f.Operator = N''NotEquals'' AND f.TagKey = tdn.TagDefinitionKey
                        AND ( f.FilterValue = rtn.TagValue OR (f.IsBlank = 1 AND rtn.TagValue IS NULL) )
            WHERE rtn.ResourceId = r.ResourceId )
        AND ( NOT EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''ResourceName'' AND f.Operator = N''Contains'')
              OR EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''ResourceName'' AND f.Operator = N''Contains''
                         AND r.ResourceName LIKE N''%'' + REPLACE(REPLACE(REPLACE(REPLACE(f.FilterValue, ''['', ''[[]''), '']'', ''[]]''), ''_'', ''[_]''), ''%'', ''[%]'') + N''%'' ESCAPE '']'') )
        AND ( NOT EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''Description'' AND f.Operator = N''Contains'')
              OR EXISTS (SELECT 1 FROM @FiltersParam f WHERE f.FilterColumn = N''Description'' AND f.Operator = N''Contains''
                         AND r.Description LIKE N''%'' + REPLACE(REPLACE(REPLACE(REPLACE(f.FilterValue, ''['', ''[[]''), '']'', ''[]]''), ''_'', ''[_]''), ''%'', ''[%]'') + N''%'' ESCAPE '']'') )';

    SET @whereClause = @whereClause + @filterClause;

    -- Build ORDER BY expression with whitelist validation. This expression is used both to select
    -- the top-N page (OFFSET/FETCH) AND to number the rows (ROW_NUMBER) so the final result set is
    -- returned in the requested order - not ResourceUid order. It references the projected column
    -- names of the DistinctResources CTE below (e.g. ResourceType, not rt_type.TypeName). Only
    -- whitelisted identifiers are ever concatenated, so there is no injection surface.
    DECLARE @orderByExpr NVARCHAR(MAX) = N'ResourceName ' + @OrderDirection; -- default
    IF @OrderBy IS NOT NULL AND LTRIM(RTRIM(@OrderBy)) <> ''
    BEGIN
        IF @OrderBy = 'TypeName'
            SET @orderByExpr = N'ResourceType ' + @OrderDirection + N', ResourceName ASC';
        ELSE IF @OrderBy = 'ResourceName'
            SET @orderByExpr = N'ResourceName ' + @OrderDirection;
        ELSE IF @OrderBy IN ('ResourceId', 'ResourceUid', 'ResourceKey', 'Description', 'CreatedOn', 'UpdatedOn')
            SET @orderByExpr = @OrderBy + N' ' + @OrderDirection + N', ResourceName ASC';
        -- else: invalid column -> keep the default expression
    END
    -- Unique final tiebreak so the ROW_NUMBER order and the OFFSET/FETCH order are identical even
    -- when the sort column has duplicate values (ResourceId is the identity key).
    SET @orderByExpr = @orderByExpr + N', ResourceId ASC';
    SET @orderClause = N'ORDER BY ' + @orderByExpr;

    -- Shared parameter definition for the (possibly NULL) search value and the filter TVP.
    DECLARE @countParamDef NVARCHAR(MAX) =
        N'@SafeSearchForParam NVARCHAR(255), @FiltersParam [HTResourceMapper].[ResourceFilterList] READONLY, @TotalRecordsParam INT OUTPUT';
    DECLARE @pageParamDef NVARCHAR(MAX) =
        N'@SafeSearchForParam NVARCHAR(255), @SkipParam INT, @TakeParam INT, @TagLimitParam INT, @FiltersParam [HTResourceMapper].[ResourceFilterList] READONLY';

    -- Get total count of distinct resources (same @whereClause as the page query)
    SET @sql = '
    SELECT @TotalRecordsParam = COUNT(DISTINCT r.ResourceId)
    FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
    LEFT JOIN [HTResourceMapper].[ResourceType] rt_type WITH(NOLOCK) ON r.ResourceTypeId = rt_type.ResourceTypeId
    ' + @whereClause;

    EXEC sp_executesql @sql, @countParamDef,
        @SafeSearchForParam = @SafeSearchFor,
        @FiltersParam = @Filters,
        @TotalRecordsParam = @TotalRecords OUTPUT;

    -- Get paged resources with their tags (capped at @TagLimit per resource in SQL) in a single resultset
    SET @sql = '
    ;WITH DistinctResources AS (
        SELECT DISTINCT r.ResourceId, r.ResourceUid, r.ResourceKey, r.ResourceName,
               rt_type.TypeName as ResourceType, r.Description,
               r.CreatedOn, r.UpdatedOn, COALESCE(r.UpdatedOn, r.CreatedOn) as LastUpdatedOn
        FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
        LEFT JOIN [HTResourceMapper].[ResourceType] rt_type WITH(NOLOCK) ON r.ResourceTypeId = rt_type.ResourceTypeId
        ' + @whereClause + '
    ),
    PagedResources AS (
        SELECT ResourceId, ResourceUid, ResourceName, ResourceType, Description, LastUpdatedOn,
               ROW_NUMBER() OVER (' + @orderClause + ') as SortSeq
        FROM DistinctResources
        ' + @orderClause + '
        OFFSET @SkipParam ROWS
        FETCH NEXT @TakeParam ROWS ONLY
    ),
    TagsWithPriority AS (
        SELECT
            pr.ResourceUid,
            pr.ResourceName,
            pr.ResourceType,
            pr.Description,
            pr.LastUpdatedOn,
            pr.SortSeq,
            td.TagDefinitionUid as TagUid,
            td.TagDefinitionKey as TagKey,
            tct.TagCode as ContentType,
            rt.TagValue,
            ' + @priorityLogic + ' as Priority,
            ROW_NUMBER() OVER (
                PARTITION BY pr.ResourceUid
                ORDER BY
                    ' + @orderByLogic + '
            ) as TagRank
        FROM PagedResources pr
        LEFT JOIN [HTResourceMapper].[ResourceTag] rt WITH(NOLOCK) ON pr.ResourceId = rt.ResourceId
        LEFT JOIN [HTResourceMapper].[TagDefinition] td WITH(NOLOCK) ON rt.TagDefinitionId = td.TagDefinitionId
        LEFT JOIN [HTResourceMapper].[TagContentType] tct WITH(NOLOCK) ON td.TagContentTypeId = tct.TagContentTypeId
    )
    SELECT
        ResourceUid,
        ResourceName,
        ResourceType,
        Description,
        LastUpdatedOn,
        TagUid,
        TagKey,
        ContentType,
        TagValue,
        Priority,
        TagRank
    FROM TagsWithPriority
    WHERE TagRank <= @TagLimitParam
    ORDER BY SortSeq, Priority, TagRank';

    EXEC sp_executesql @sql, @pageParamDef,
        @SafeSearchForParam = @SafeSearchFor,
        @SkipParam = @Skip,
        @TakeParam = @Take,
        @TagLimitParam = @TagLimit,
        @FiltersParam = @Filters;

END
