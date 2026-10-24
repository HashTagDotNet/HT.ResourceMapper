-- Early uniqueness check for the General tab's identity preview. Identity is
-- (Domain + Type + Key) when a domain tag is defined (matched via a join, since Domain is a
-- ResourceTag, not a Resource column) — so two resources sharing (Type + Key) in DIFFERENT
-- domains are correctly reported as unique, not a collision. Falls back to (Type + Key) when no
-- domain tag is defined. @ExcludeResourceUid excludes the resource being edited.
-- No NOLOCK on purpose: the uniqueness check must read committed data.
CREATE PROCEDURE [ResourceExplorer].[Resource_CheckUnique]
    @ResourceTypeId       INT,
    @ResourceKey          NVARCHAR(250),
    @Domain               NVARCHAR(2000) = NULL,
    @ExcludeResourceUid   VARCHAR(40) = NULL,
    @IsUnique             BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [ResourceExplorer].[TagDefinition] WHERE IsDomainTag = 1);

    IF @DomainTagDefId IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM [ResourceExplorer].[Resource] r
            LEFT JOIN [ResourceExplorer].[ResourceTag] rt
                ON rt.ResourceId = r.ResourceId AND rt.TagDefinitionId = @DomainTagDefId
            WHERE r.ResourceTypeId = @ResourceTypeId
              AND r.ResourceKey = @ResourceKey
              -- NULL is a value: a NULL @Domain collides with domain-less resources.
              AND (rt.TagValue = @Domain OR (rt.TagValue IS NULL AND @Domain IS NULL))
              AND (@ExcludeResourceUid IS NULL OR r.ResourceUid <> @ExcludeResourceUid)
        )
            SET @IsUnique = 0;
        ELSE
            SET @IsUnique = 1;
    END
    ELSE
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM [ResourceExplorer].[Resource]
            WHERE ResourceTypeId = @ResourceTypeId
              AND ResourceKey = @ResourceKey
              AND (@ExcludeResourceUid IS NULL OR ResourceUid <> @ExcludeResourceUid)
        )
            SET @IsUnique = 0;
        ELSE
            SET @IsUnique = 1;
    END
END
