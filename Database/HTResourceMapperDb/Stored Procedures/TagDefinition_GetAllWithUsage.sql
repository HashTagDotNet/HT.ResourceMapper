CREATE PROCEDURE [HTResourceMapper].TagDefinition_GetAllWithUsage
AS
BEGIN
    SET NOCOUNT ON;

    -- TagDefinition_GetAll's columns plus the three usage counts the management screen needs. They are
    -- exactly the three things that reference a definition, and therefore exactly what can block a
    -- delete (see TagDefinition_Delete):
    --   ResourceCount     - ResourceTag rows: resources carrying a value for this tag.
    --   TypeTemplateCount - ResourceTypeTag rows: resource types whose entry-point template lists it.
    --   PrimaryForCount   - Resource.PrimaryTagDefinitionId: resources whose click-through link IS this
    --                       tag. That FK is NO ACTION, so a delete would fail at the database rather
    --                       than politely, which is why the screen has to be able to see it coming.
    --
    -- Correlated subqueries rather than joins: three independent one-to-many counts would otherwise
    -- multiply each other, and this table is catalog-sized reference data read once per screen.

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
        td.UpdatedOn,
        (SELECT COUNT(*) FROM [HTResourceMapper].[ResourceTag] rt
          WHERE rt.TagDefinitionId = td.TagDefinitionId)       AS ResourceCount,
        (SELECT COUNT(*) FROM [HTResourceMapper].[ResourceTypeTag] rtt
          WHERE rtt.TagDefinitionId = td.TagDefinitionId)      AS TypeTemplateCount,
        (SELECT COUNT(*) FROM [HTResourceMapper].[Resource] r
          WHERE r.PrimaryTagDefinitionId = td.TagDefinitionId) AS PrimaryForCount
    FROM [HTResourceMapper].[TagDefinition] td
    INNER JOIN [HTResourceMapper].[TagContentType] tc ON tc.TagContentTypeId = td.TagContentTypeId
    ORDER BY td.DisplayOrder ASC, td.TagDefinitionKey ASC;
END
