CREATE PROCEDURE [HTResourceMapper].TagDefinition_GetAll
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        td.TagDefinitionId,
        td.TagDefinitionUid,
        td.TagDefinitionKey,
        td.TagContentTypeId,
        td.AllowCustomValue,
        td.IsMultiValued,
        td.AllowedValues,
        td.IsSystemTag,
        td.CreatedOn,
        td.UpdatedOn
    FROM [HTResourceMapper].[TagDefinition] td
    ORDER BY td.TagDefinitionKey ASC;
END
