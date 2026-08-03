-- Every ResourceTag in the catalog, joined to its definition, for the JSON export. The service
-- groups these by ResourceId in memory rather than issuing a query per resource. IsMultiValued
-- drives the string-vs-array emit decision (ImportExportApiDesign #8); IsDomainTag lets the
-- service keep the domain tag in the exported tag map (that is what re-establishes identity on
-- re-import) while still knowing which key it is.
CREATE PROCEDURE [HTResourceMapper].[Export_GetResourceTags]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rt.ResourceId,
        td.TagDefinitionKey,
        rt.TagValue,
        td.IsMultiValued,
        td.IsDomainTag
    FROM [HTResourceMapper].[ResourceTag] rt
    INNER JOIN [HTResourceMapper].[TagDefinition] td
        ON td.TagDefinitionId = rt.TagDefinitionId
    -- Deterministic ordering: definition display order, then key, then insert order for the
    -- multi-valued case, so exported arrays keep a stable element order.
    ORDER BY rt.ResourceId, td.DisplayOrder, td.TagDefinitionKey, rt.ResourceTagId;
END
