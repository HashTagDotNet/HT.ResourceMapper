-- Make one view the owner's default, or clear the default entirely when @SavedViewUid is NULL.
--
-- The clear MUST happen before the set: UX_SavedView_Default permits only one IsDefault=1 row per
-- owner, so setting first would violate the index. Both run in one transaction so a failure cannot
-- leave the owner with no default when they asked for one.
CREATE PROCEDURE [HTResourceMapper].[SavedView_SetDefault]
    @OwnerId VARCHAR(64),
    @SavedViewUid VARCHAR(40) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

        UPDATE [HTResourceMapper].[SavedView]
        SET IsDefault = 0, UpdatedOn = SYSUTCDATETIME()
        WHERE OwnerId = @OwnerId AND IsDefault = 1;

        IF @SavedViewUid IS NOT NULL
            UPDATE [HTResourceMapper].[SavedView]
            SET IsDefault = 1, UpdatedOn = SYSUTCDATETIME()
            WHERE OwnerId = @OwnerId AND SavedViewUid = @SavedViewUid;

    COMMIT TRANSACTION;
END
