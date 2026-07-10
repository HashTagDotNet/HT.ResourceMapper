-- Same-domain resource search backing the Dependencies / Dependent On tabs' picker. Domain is a
-- ResourceTag value (joined via the IsDomainTag=1 TagDefinition), not a Resource column, so it's
-- matched the same way Resource_GetByResourceUid / Resource_CheckUnique / Resource_GetAllKeys do.
-- @Domain filter: matches when both sides share the same domain value, OR both have none (the
-- "Unused" domain state, design §6) — a domain-having side never matches a domain-less side.
CREATE PROCEDURE [HTResourceMapper].[Resource_SearchForPicker]
    @SearchFor           NVARCHAR(255) = NULL,
    @Domain              NVARCHAR(2000) = NULL,
    @ResourceTypeId      INT = NULL,
    @ExcludeResourceUid  VARCHAR(40) = NULL,
    @Skip                INT = 0,
    @Take                INT = 20,
    @TotalRecords        INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    SET @Skip = ISNULL(@Skip, 0);
    IF @Skip < 0 SET @Skip = 0;
    SET @Take = ISNULL(@Take, 20);
    IF @Take <= 0 SET @Take = 20;
    IF @Take > 100 SET @Take = 100; -- prevent excessive data retrieval from a picker autocomplete

    -- Escape LIKE special characters (%, _, [, ]) so a search containing them isn't misread as
    -- wildcards — same convention as Resource_GetItems.
    DECLARE @SafeSearchFor NVARCHAR(255) = NULL;
    IF @SearchFor IS NOT NULL AND LTRIM(RTRIM(@SearchFor)) <> ''
    BEGIN
        SET @SafeSearchFor = '%' +
            REPLACE(
                REPLACE(
                    REPLACE(
                        REPLACE(@SearchFor, '[', '[[]'),
                    ']', '[]]'),
                '_', '[_]'),
            '%', '[%]') + '%';
    END

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- A CTE only scopes to the single statement immediately following its WITH clause, so the
    -- candidate definition is repeated for the count vs. the page query (same convention as
    -- Resource_GetItems, which re-runs its @whereClause for the count then the page).
    ;WITH Candidates AS (
        SELECT
            r.ResourceUid,
            r.ResourceName,
            r.ResourceKey,
            rty.TypeName AS ResourceTypeName,
            dt.TagValue  AS Domain
        FROM [HTResourceMapper].[Resource] r
        INNER JOIN [HTResourceMapper].[ResourceType] rty ON rty.ResourceTypeId = r.ResourceTypeId
        LEFT JOIN [HTResourceMapper].[ResourceTag] dt
            ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
        WHERE (@ExcludeResourceUid IS NULL OR r.ResourceUid <> @ExcludeResourceUid)
          AND (@ResourceTypeId IS NULL OR r.ResourceTypeId = @ResourceTypeId)
          AND ((@Domain IS NULL AND dt.TagValue IS NULL) OR dt.TagValue = @Domain)
          AND (@SafeSearchFor IS NULL
               OR r.ResourceName LIKE @SafeSearchFor ESCAPE ']'
               OR r.ResourceKey  LIKE @SafeSearchFor ESCAPE ']')
    )
    SELECT @TotalRecords = COUNT(*) FROM Candidates;

    ;WITH Candidates AS (
        SELECT
            r.ResourceUid,
            r.ResourceName,
            r.ResourceKey,
            rty.TypeName AS ResourceTypeName,
            dt.TagValue  AS Domain
        FROM [HTResourceMapper].[Resource] r
        INNER JOIN [HTResourceMapper].[ResourceType] rty ON rty.ResourceTypeId = r.ResourceTypeId
        LEFT JOIN [HTResourceMapper].[ResourceTag] dt
            ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
        WHERE (@ExcludeResourceUid IS NULL OR r.ResourceUid <> @ExcludeResourceUid)
          AND (@ResourceTypeId IS NULL OR r.ResourceTypeId = @ResourceTypeId)
          AND ((@Domain IS NULL AND dt.TagValue IS NULL) OR dt.TagValue = @Domain)
          AND (@SafeSearchFor IS NULL
               OR r.ResourceName LIKE @SafeSearchFor ESCAPE ']'
               OR r.ResourceKey  LIKE @SafeSearchFor ESCAPE ']')
    )
    SELECT
        ResourceUid,
        ResourceName,
        ResourceKey,
        ResourceTypeName,
        Domain
    FROM Candidates
    ORDER BY ResourceName ASC, ResourceUid ASC
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
END
