-- Create or update a diagram. Matches on (DiagramUid, OwnerId) so a client can only update its
-- own rows; @ShareId is used only on insert. Returns the authoritative Result/DiagramUid/ShareId
-- by selecting the row back (so updates return the real existing ShareId). If @DiagramUid already
-- exists under a DIFFERENT OwnerId, the request is denied (Result='denied', empty ids) rather
-- than attempting a blind INSERT that would violate UK_Diagram_DiagramUid.
CREATE PROCEDURE [HTResourceMapper].[Diagram_Upsert]
    @DiagramUid VARCHAR(40),
    @ShareId VARCHAR(40),
    @OwnerId VARCHAR(64),
    @Name NVARCHAR(200),
    @SeedResourceUid VARCHAR(40),
    @DisplayPreset VARCHAR(20),
    @DiagramJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result VARCHAR(10);

    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[Diagram]
               WHERE DiagramUid = @DiagramUid AND OwnerId = @OwnerId)
    BEGIN
        UPDATE [HTResourceMapper].[Diagram]
        SET Name = @Name, SeedResourceUid = @SeedResourceUid, DisplayPreset = @DisplayPreset,
            DiagramJson = @DiagramJson, UpdatedOn = SYSUTCDATETIME()
        WHERE DiagramUid = @DiagramUid AND OwnerId = @OwnerId;
        SET @Result = 'updated';
    END
    ELSE IF EXISTS (SELECT 1 FROM [HTResourceMapper].[Diagram] WHERE DiagramUid = @DiagramUid)
    BEGIN
        -- uid exists under a different client: refuse (no blind insert -> no UK violation).
        SET @Result = 'denied';
    END
    ELSE
    BEGIN
        INSERT INTO [HTResourceMapper].[Diagram]
            (DiagramUid, ShareId, OwnerId, Name, SeedResourceUid, DisplayPreset, DiagramJson)
        VALUES (@DiagramUid, @ShareId, @OwnerId, @Name, @SeedResourceUid, @DisplayPreset, @DiagramJson);
        SET @Result = 'created';
    END

    IF @Result = 'denied'
        SELECT @Result AS Result, CAST('' AS VARCHAR(40)) AS DiagramUid, CAST('' AS VARCHAR(40)) AS ShareId;
    ELSE
        SELECT @Result AS Result, DiagramUid, ShareId
        FROM [HTResourceMapper].[Diagram]
        WHERE DiagramUid = @DiagramUid AND OwnerId = @OwnerId;
END
