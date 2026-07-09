CREATE PROCEDURE [HTResourceMapper].[ResourceRelationship_Remove]
    @FromResourceId INT,
    @ToResourceId   INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [HTResourceMapper].[ResourceRelationship]
    WHERE FromResourceId = @FromResourceId
      AND ToResourceId = @ToResourceId;
END
