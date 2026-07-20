-- Delete a client's own diagram. Returns 1 if a row was deleted, else 0.
CREATE PROCEDURE [HTResourceMapper].[Diagram_Delete]
    @ClientId VARCHAR(64),
    @DiagramUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [HTResourceMapper].[Diagram]
    WHERE ClientId = @ClientId AND DiagramUid = @DiagramUid;

    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS INT) AS Deleted;
END
