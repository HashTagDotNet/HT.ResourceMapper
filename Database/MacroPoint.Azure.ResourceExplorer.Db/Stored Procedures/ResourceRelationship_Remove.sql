CREATE PROCEDURE [ResourceExplorer].[ResourceRelationship_Remove]
    @FromResourceId INT,
    @ToResourceId   INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [ResourceExplorer].[ResourceRelationship]
    WHERE FromResourceId = @FromResourceId
      AND ToResourceId = @ToResourceId;
END
