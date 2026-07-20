-- Resolve a diagram by its public share token (read-only share; also used by the owner to open
-- a recent diagram, since the list returns each row's ShareId).
CREATE PROCEDURE [HTResourceMapper].[Diagram_GetByShareId]
    @ShareId VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DiagramUid, ShareId, ClientId, Name, SeedResourceUid, DisplayPreset, DiagramJson,
        CONVERT(VARCHAR(33), COALESCE(UpdatedOn, CreatedOn), 126) AS UpdatedOnUtc
    FROM [HTResourceMapper].[Diagram]
    WHERE ShareId = @ShareId;
END
