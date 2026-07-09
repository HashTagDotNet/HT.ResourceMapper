CREATE PROCEDURE [HTResourceMapper].[Resource_GetByResourceUid]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ResourceId,
        ResourceUid,
        ResourceKey,
        ResourceTypeId,
        ResourceName,
        [Description],
        PrimaryTagDefinitionId,
        CreatedOn,
        UpdatedOn
    FROM [HTResourceMapper].[Resource] WITH(NOLOCK)
    WHERE ResourceUid = @ResourceUid;
END
