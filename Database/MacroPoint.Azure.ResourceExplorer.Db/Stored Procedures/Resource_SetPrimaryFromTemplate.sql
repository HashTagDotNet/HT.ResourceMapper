-- Import helper: seeds Resource.PrimaryTagDefinitionId from the resource type's IsDefaultPrimary
-- template tag, only when the resource has no primary yet and actually carries a value for that tag.
CREATE PROCEDURE [ResourceExplorer].Resource_SetPrimaryFromTemplate
    @ResourceId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE r
    SET r.PrimaryTagDefinitionId = rtt.TagDefinitionId
    FROM [ResourceExplorer].[Resource] r
    INNER JOIN [ResourceExplorer].[ResourceTypeTag] rtt
        ON rtt.ResourceTypeId = r.ResourceTypeId AND rtt.IsDefaultPrimary = 1
    WHERE r.ResourceId = @ResourceId
      AND r.PrimaryTagDefinitionId IS NULL
      AND EXISTS (SELECT 1 FROM [ResourceExplorer].[ResourceTag] t
                  WHERE t.ResourceId = r.ResourceId AND t.TagDefinitionId = rtt.TagDefinitionId);
END
