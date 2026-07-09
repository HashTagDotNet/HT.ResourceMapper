CREATE PROCEDURE [HTResourceMapper].TagDefinition_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        td.TagDefinitionId,
        td.TagDefinitionUid,
        td.TagDefinitionKey,
        td.DisplayName,
        td.TagContentTypeId,
        td.AllowCustomValue,
        td.IsMultiValued,
        td.AllowedValues,
        td.RequirementLevel,
        td.IsDomainTag,
        td.IsSystemTag,
        td.DisplayOrder,
        td.CreatedOn,
        td.UpdatedOn
    FROM [HTResourceMapper].[TagDefinition] td
    ORDER BY td.DisplayOrder ASC, td.TagDefinitionKey ASC;
END
