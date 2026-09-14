-- A client's diagrams for Open-Recent (metadata only; no DiagramJson). Includes ShareId so the
-- caller can open a chosen diagram via Diagram_GetByShareId.
CREATE PROCEDURE [ResourceExplorer].[Diagram_ListForOwner]
    @OwnerId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DiagramUid, ShareId, Name, SeedResourceUid,
        CONVERT(VARCHAR(33), COALESCE(UpdatedOn, CreatedOn), 126) AS UpdatedOnUtc
    FROM [ResourceExplorer].[Diagram]
    WHERE OwnerId = @OwnerId
    ORDER BY COALESCE(UpdatedOn, CreatedOn) DESC;
END
