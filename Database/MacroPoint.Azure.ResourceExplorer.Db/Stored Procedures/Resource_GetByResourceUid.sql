CREATE PROCEDURE [ResourceExplorer].[Resource_GetByResourceUid]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [ResourceExplorer].[TagDefinition] WHERE IsDomainTag = 1);

    SELECT
        r.ResourceId,
        r.ResourceUid,
        r.ResourceKey,
        r.ResourceTypeId,
        r.ResourceName,
        r.[Description],
        r.PrimaryTagDefinitionId,
        dt.TagValue AS Domain,
        r.CreatedOn,
        r.UpdatedOn
    FROM [ResourceExplorer].[Resource] r WITH(NOLOCK)
    LEFT JOIN [ResourceExplorer].[ResourceTag] dt
        ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    WHERE r.ResourceUid = @ResourceUid;
END
