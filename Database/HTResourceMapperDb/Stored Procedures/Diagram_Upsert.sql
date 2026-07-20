-- Create or update a diagram. Matches on (DiagramUid, ClientId) so a client can only update its
-- own rows; @ShareId is used only on insert. Returns the authoritative Result/DiagramUid/ShareId
-- by selecting the row back (so updates return the real existing ShareId).
CREATE PROCEDURE [HTResourceMapper].[Diagram_Upsert]
    @DiagramUid VARCHAR(40),
    @ShareId VARCHAR(40),
    @ClientId VARCHAR(64),
    @Name NVARCHAR(200),
    @SeedResourceUid VARCHAR(40),
    @DisplayPreset VARCHAR(20),
    @DiagramJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result VARCHAR(10);

    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[Diagram]
               WHERE DiagramUid = @DiagramUid AND ClientId = @ClientId)
    BEGIN
        UPDATE [HTResourceMapper].[Diagram]
        SET Name = @Name, SeedResourceUid = @SeedResourceUid, DisplayPreset = @DisplayPreset,
            DiagramJson = @DiagramJson, UpdatedOn = SYSUTCDATETIME()
        WHERE DiagramUid = @DiagramUid AND ClientId = @ClientId;
        SET @Result = 'updated';
    END
    ELSE
    BEGIN
        INSERT INTO [HTResourceMapper].[Diagram]
            (DiagramUid, ShareId, ClientId, Name, SeedResourceUid, DisplayPreset, DiagramJson)
        VALUES (@DiagramUid, @ShareId, @ClientId, @Name, @SeedResourceUid, @DisplayPreset, @DiagramJson);
        SET @Result = 'created';
    END

    SELECT @Result AS Result, DiagramUid, ShareId
    FROM [HTResourceMapper].[Diagram]
    WHERE DiagramUid = @DiagramUid AND ClientId = @ClientId;
END
