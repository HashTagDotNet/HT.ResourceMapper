-- Every applied tag value for @ResourceId, joined to its definition for render rules.
-- IsPrimary marks the row whose TagDefinitionId matches the resource's PrimaryTagDefinitionId.
-- Feeds resource-detail reads and the editor's edit-load.
CREATE PROCEDURE [ResourceExplorer].[ResourceTag_GetForResource]
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
        CAST(CASE WHEN r.PrimaryTagDefinitionId = td.TagDefinitionId THEN 1 ELSE 0 END AS BIT) AS IsPrimary
    FROM [ResourceExplorer].[ResourceTag] rt
    INNER JOIN [ResourceExplorer].[TagDefinition] td ON td.TagDefinitionId = rt.TagDefinitionId
    INNER JOIN [ResourceExplorer].[TagContentType] tc ON tc.TagContentTypeId = td.TagContentTypeId
    INNER JOIN [ResourceExplorer].[Resource] r ON r.ResourceId = rt.ResourceId
    WHERE rt.ResourceId = @ResourceId
    ORDER BY td.DisplayOrder ASC, td.TagDefinitionKey ASC;
END
