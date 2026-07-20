-- A client's diagrams for Open-Recent (metadata only; no DiagramJson). Includes ShareId so the
-- caller can open a chosen diagram via Diagram_GetByShareId.
CREATE PROCEDURE [HTResourceMapper].[Diagram_ListForClient]
    @ClientId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DiagramUid, ShareId, Name, SeedResourceUid,
        CONVERT(VARCHAR(33), COALESCE(UpdatedOn, CreatedOn), 126) AS UpdatedOnUtc
    FROM [HTResourceMapper].[Diagram]
    WHERE ClientId = @ClientId
    ORDER BY COALESCE(UpdatedOn, CreatedOn) DESC;
END
