-- Editor-facing create/update for a resource type (PL-28).
--
-- Distinct from ResourceType_Upsert, which is the IMPORT path: that one keys on TypeName (so it
-- can never rename) and does not touch ShortCode / IconKey. This one keys on the surrogate
-- ResourceTypeId, so a rename is a normal update, and it owns the two columns the explorer
-- renders from (ShortCode prints inside the node, IconKey picks the glyph + colour).
--
-- @ResourceTypeId NULL or 0 = create. TypeName collisions are reported as 'duplicate' rather
-- than surfacing UK_ResourceType_TypeName as a raw SQL error.
CREATE PROCEDURE [HTResourceMapper].ResourceType_Save
    @ResourceTypeUid  VARCHAR(40),           -- used only on insert
    @TypeName         NVARCHAR(250),
    @ShortCode        VARCHAR(10)  = NULL,
    @IconKey          VARCHAR(40)  = NULL,
    @AllowCustomTags  BIT          = 1,
    @ResourceTypeId   INT          = NULL OUTPUT,  -- in: row to update; out: affected row id
    @Result           VARCHAR(10)  OUTPUT          -- 'created' | 'updated' | 'duplicate' | 'notfound'
AS
BEGIN
    SET NOCOUNT ON;

    -- The UI sends 0 for "new"; treat it the same as NULL.
    IF @ResourceTypeId = 0 SET @ResourceTypeId = NULL;

    SET @TypeName  = LTRIM(RTRIM(@TypeName));
    SET @ShortCode = NULLIF(LTRIM(RTRIM(@ShortCode)), '');
    SET @IconKey   = NULLIF(LTRIM(RTRIM(@IconKey)), '');

    -- Update target must still exist (it may have been deleted by another session).
    IF @ResourceTypeId IS NOT NULL
       AND NOT EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceType] WHERE ResourceTypeId = @ResourceTypeId)
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    -- TypeName is unique (UK_ResourceType_TypeName); a row may of course keep its own name.
    IF EXISTS (
        SELECT 1
        FROM [HTResourceMapper].[ResourceType]
        WHERE TypeName = @TypeName
          AND (@ResourceTypeId IS NULL OR ResourceTypeId <> @ResourceTypeId)
    )
    BEGIN
        SET @Result = 'duplicate';
        RETURN;
    END

    IF @ResourceTypeId IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[ResourceType]
            (ResourceTypeUid, TypeName, AllowCustomTags, ShortCode, IconKey)
        VALUES
            (@ResourceTypeUid, @TypeName, @AllowCustomTags, @ShortCode, @IconKey);

        SET @ResourceTypeId = CAST(SCOPE_IDENTITY() AS INT);
        SET @Result = 'created';
    END
    ELSE
    BEGIN
        UPDATE [HTResourceMapper].[ResourceType]
        SET TypeName        = @TypeName,
            ShortCode       = @ShortCode,
            IconKey         = @IconKey,
            AllowCustomTags = @AllowCustomTags,
            UpdatedOn       = SYSUTCDATETIME()
        WHERE ResourceTypeId = @ResourceTypeId;

        SET @Result = 'updated';
    END
END
