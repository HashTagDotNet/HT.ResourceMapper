CREATE PROCEDURE [HTResourceMapper].[Resource_GetItems]
    @SearchFor NVARCHAR(255) = NULL,
    @OrderBy NVARCHAR(50) = NULL,
    @OrderDirection NVARCHAR(4) = NULL,
    @Skip INT = 0,
    @Take INT = 50,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    -- Validate and set defaults
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
    DECLARE @whereClause NVARCHAR(MAX) = '';
    DECLARE @orderClause NVARCHAR(MAX) = '';
    
    -- Build WHERE clause for search - using parameterized approach
    IF @SafeSearchFor IS NOT NULL
    BEGIN
        SET @whereClause = '
        WHERE (
            r.ResourceKey LIKE @SafeSearchFor ESCAPE '']''
            OR r.ResourceName LIKE @SafeSearchFor ESCAPE '']''
            OR r.Description LIKE @SafeSearchFor ESCAPE '']''
            OR EXISTS (
                SELECT 1 FROM [HTResourceMapper].[ResourceTag] rt2
                INNER JOIN [HTResourceMapper].[TagDefinition] td2 ON rt2.TagDefinitionId = td2.TagDefinitionId
                WHERE rt2.ResourceId = r.ResourceId
                AND (rt2.TagValue LIKE @SafeSearchFor ESCAPE '']'' OR td2.TagDefinitionKey LIKE @SafeSearchFor ESCAPE '']'')
            )
        )';
    END
    
    -- Build ORDER BY clause with whitelist validation
    IF @OrderBy IS NOT NULL AND LTRIM(RTRIM(@OrderBy)) <> ''
    BEGIN
        -- Strict whitelist validation to prevent SQL injection
        IF @OrderBy IN ('ResourceId', 'ResourceUid', 'ResourceKey', 'ResourceName', 'Description', 'CreatedOn', 'UpdatedOn', 'TypeName')
        BEGIN
            IF @OrderBy = 'TypeName'
                SET @orderClause = 'ORDER BY rt_type.TypeName ' + @OrderDirection + ', r.ResourceName ASC';
            ELSE
                SET @orderClause = 'ORDER BY r.' + @OrderBy + ' ' + @OrderDirection;
        END
        ELSE
            SET @orderClause = 'ORDER BY r.ResourceName ASC'; -- Default fallback for invalid columns
    END
    ELSE
        SET @orderClause = 'ORDER BY r.ResourceName ASC'; -- Default order
    
    -- Get total count of distinct resources
    SET @sql = '
    SELECT @TotalRecords = COUNT(DISTINCT r.ResourceId)
    FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
    LEFT JOIN [HTResourceMapper].[ResourceType] rt_type WITH(NOLOCK) ON r.ResourceTypeId = rt_type.ResourceTypeId
    ' + @whereClause;
    
    IF @SafeSearchFor IS NOT NULL
    BEGIN
        EXEC sp_executesql @sql, 
            N'@SafeSearchFor NVARCHAR(255), @TotalRecords INT OUTPUT', 
            @SafeSearchFor, @TotalRecords OUTPUT;
    END
    ELSE
    BEGIN
        EXEC sp_executesql @sql, 
            N'@TotalRecords INT OUTPUT', 
            @TotalRecords OUTPUT;
    END
    
    -- Get paged resources with all their tags in single resultset
    SET @sql = '
    ;WITH PagedResources AS (
        SELECT DISTINCT r.ResourceId, r.ResourceUid, r.ResourceName, rt_type.TypeName as ResourceType,
               r.Description, COALESCE(r.UpdatedOn, r.CreatedOn) as LastUpdatedOn
        FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
        LEFT JOIN [HTResourceMapper].[ResourceType] rt_type WITH(NOLOCK) ON r.ResourceTypeId = rt_type.ResourceTypeId
        ' + @whereClause + '
        ' + @orderClause + '
        OFFSET @Skip ROWS
        FETCH NEXT @Take ROWS ONLY
    ),
    TagsWithPriority AS (
        SELECT 
            pr.ResourceUid,
            pr.ResourceName,
            pr.ResourceType,
            pr.Description,
            pr.LastUpdatedOn,
            td.TagDefinitionUid as TagUid,
            td.TagDefinitionKey as TagKey,
            tct.TagCode as ContentType,
            rt.TagValue,
            CASE 
                WHEN @SafeSearchFor IS NOT NULL AND (
                    rt.TagValue LIKE @SafeSearchFor ESCAPE '']''
                    OR td.TagDefinitionKey LIKE @SafeSearchFor ESCAPE '']'')
                THEN 0  -- Matching tags get priority 0 (highest)
                ELSE 1  -- Non-matching tags get priority 1
            END as Priority,
            ROW_NUMBER() OVER (
                PARTITION BY pr.ResourceUid 
                ORDER BY 
                    CASE 
                        WHEN @SafeSearchFor IS NOT NULL AND (
                            rt.TagValue LIKE @SafeSearchFor ESCAPE '']''
                            OR td.TagDefinitionKey LIKE @SafeSearchFor ESCAPE '']'')
                        THEN 0 
                        ELSE 1 
                    END,
                    td.TagDefinitionKey,
                    rt.TagValue
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
    ORDER BY ResourceUid, Priority, TagRank';
    
    IF @SafeSearchFor IS NOT NULL
    BEGIN
        EXEC sp_executesql @sql, 
            N'@SafeSearchFor NVARCHAR(255), @Skip INT, @Take INT', 
            @SafeSearchFor, @Skip, @Take;
    END
    ELSE
    BEGIN
        EXEC sp_executesql @sql, 
            N'@Skip INT, @Take INT', 
            @Skip, @Take;
    END
        
END