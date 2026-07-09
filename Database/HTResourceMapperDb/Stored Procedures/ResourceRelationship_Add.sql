-- Idempotent: adding an edge that already exists is a no-op. Self-loops are rejected by the
-- CK_ResourceRelationship_NoSelfLoop check constraint (will throw if From = To).
CREATE PROCEDURE [HTResourceMapper].[ResourceRelationship_Add]
    @FromResourceId INT,
    @ToResourceId   INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM [HTResourceMapper].[ResourceRelationship]
        WHERE FromResourceId = @FromResourceId AND ToResourceId = @ToResourceId
    )
    BEGIN
        INSERT INTO [HTResourceMapper].[ResourceRelationship] (FromResourceId, ToResourceId)
        VALUES (@FromResourceId, @ToResourceId);
    END
END
