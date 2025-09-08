CREATE PROCEDURE [HTResourceMapper].[Resource_GetByResourceUid]
    @ResourceUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT *
    FROM [HTResourceMapper].[Resource] WITH(NOLOCK)
    WHERE ResourceUid = @ResourceUid;
END
