-- Every applied tag value for @ResourceId, joined to its definition for render rules.
-- IsPrimary marks the row whose TagDefinitionId matches the resource's PrimaryTagDefinitionId.
-- Feeds resource-detail reads and the editor's edit-load.
CREATE PROCEDURE [HTResourceMapper].[ResourceTag_GetForResource]
    @ResourceId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        td.TagDefinitionId,
        td.TagDefinitionKey,
        td.DisplayName,
        tc.TagCode AS ContentType,
        rt.TagValue,
        td.IsSystemTag,
        td.IsMultiValued,
        CASE WHEN r.PrimaryTagDefinitionId = td.TagDefinitionId THEN 1 ELSE 0 END AS IsPrimary
    FROM [HTResourceMapper].[ResourceTag] rt
    INNER JOIN [HTResourceMapper].[TagDefinition] td ON td.TagDefinitionId = rt.TagDefinitionId
    INNER JOIN [HTResourceMapper].[TagContentType] tc ON tc.TagContentTypeId = td.TagContentTypeId
    INNER JOIN [HTResourceMapper].[Resource] r ON r.ResourceId = rt.ResourceId
    WHERE rt.ResourceId = @ResourceId
    ORDER BY td.DisplayOrder ASC, td.TagDefinitionKey ASC;
END
