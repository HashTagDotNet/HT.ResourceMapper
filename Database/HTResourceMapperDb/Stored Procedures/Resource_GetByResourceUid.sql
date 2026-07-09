CREATE PROCEDURE [HTResourceMapper].[Resource_GetByResourceUid]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

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
    FROM [HTResourceMapper].[Resource] r WITH(NOLOCK)
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId
    WHERE r.ResourceUid = @ResourceUid;
END
