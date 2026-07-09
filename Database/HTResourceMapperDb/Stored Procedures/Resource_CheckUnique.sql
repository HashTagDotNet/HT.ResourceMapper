-- Early uniqueness check for the General tab's identity preview. Identity is
-- (Domain + Type + Key) when a domain tag is defined (matched via a join, since Domain is a
-- ResourceTag, not a Resource column) — so two resources sharing (Type + Key) in DIFFERENT
-- domains are correctly reported as unique, not a collision. Falls back to (Type + Key) when no
-- domain tag is defined. @ExcludeResourceUid excludes the resource being edited.
CREATE PROCEDURE [HTResourceMapper].[Resource_CheckUnique]
    @ResourceTypeId       INT,
    @ResourceKey          NVARCHAR(250),
    @Domain               NVARCHAR(2000) = NULL,
    @ExcludeResourceUid   VARCHAR(40) = NULL,
    @IsUnique             BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    IF @DomainTagDefId IS NOT NULL
    BEGIN
        IF EXISTS (
            SELECT 1
            FROM [HTResourceMapper].[Resource] r
            INNER JOIN [HTResourceMapper].[ResourceTag] rt
                ON rt.ResourceId = r.ResourceId AND rt.TagDefinitionId = @DomainTagDefId
            WHERE r.ResourceTypeId = @ResourceTypeId
              AND r.ResourceKey = @ResourceKey
              AND rt.TagValue = @Domain
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
            FROM [HTResourceMapper].[Resource]
            WHERE ResourceTypeId = @ResourceTypeId
              AND ResourceKey = @ResourceKey
              AND (@ExcludeResourceUid IS NULL OR ResourceUid <> @ExcludeResourceUid)
        )
            SET @IsUnique = 0;
        ELSE
            SET @IsUnique = 1;
    END
END
