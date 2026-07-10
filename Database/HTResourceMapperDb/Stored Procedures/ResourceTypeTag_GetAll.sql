-- Every per-type entry-point template row (ResourceTypeId, TagDefinitionId, IsDefaultPrimary,
-- RequirementLevel). Returns ALL rows (not filtered by type) so the client can re-seed the Tags
-- tab on a Type change with no round trip.
CREATE PROCEDURE [HTResourceMapper].ResourceTypeTag_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ResourceTypeId,
        TagDefinitionId,
        IsDefaultPrimary,
        RequirementLevel
    FROM [HTResourceMapper].[ResourceTypeTag]
    ORDER BY ResourceTypeId ASC, TagDefinitionId ASC;
END
