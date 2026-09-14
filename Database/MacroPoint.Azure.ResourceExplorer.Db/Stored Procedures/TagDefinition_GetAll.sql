CREATE PROCEDURE [ResourceExplorer].TagDefinition_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    -- ContentType is denormalized here (as ResourceTag_GetForResource also does) so callers
    -- get the Text|Link code without a separate lookup. Additive column; existing readers that
    -- select specific field names by name are unaffected.
    SELECT
        td.TagDefinitionId,
        td.TagDefinitionUid,
        td.TagDefinitionKey,
        td.DisplayName,
        td.TagContentTypeId,
        tc.TagCode AS ContentType,
        td.AllowCustomValue,
        td.IsMultiValued,
        td.AllowedValues,
        td.RequirementLevel,
        td.IsDomainTag,
        td.IsSystemTag,
        td.DisplayOrder,
        td.CreatedOn,
        td.UpdatedOn
    FROM [ResourceExplorer].[TagDefinition] td
    INNER JOIN [ResourceExplorer].[TagContentType] tc ON tc.TagContentTypeId = td.TagContentTypeId
    ORDER BY td.DisplayOrder ASC, td.TagDefinitionKey ASC;
END
