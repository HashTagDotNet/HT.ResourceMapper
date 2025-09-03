CREATE PROCEDURE [HT.ResourceMapper].[Resource_GetByResourceUid]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [HT.ResourceMapper].[Resource] WITH(NOLOCK)
    WHERE ResourceUid = @ResourceUid;
END
