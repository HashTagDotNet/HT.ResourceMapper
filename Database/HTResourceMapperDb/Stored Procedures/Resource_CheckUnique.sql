-- Early uniqueness check for the General tab's identity preview. Identity is (Type + Key) here;
-- Domain joins this predicate in slice #5. @ExcludeResourceUid excludes the resource being
-- edited from the collision check.
CREATE PROCEDURE [HTResourceMapper].[Resource_CheckUnique]
    @ResourceTypeId       INT,
    @ResourceKey          NVARCHAR(250),
    @ExcludeResourceUid   VARCHAR(40) = NULL,
    @IsUnique             BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

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
