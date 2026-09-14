-- Create or update one saved view. Matches on (SavedViewUid, OwnerId) so an owner can only update
-- its own rows; a uid that exists under a DIFFERENT owner is refused rather than blind-inserted
-- into a UK_SavedView_SavedViewUid violation. Also serves rename -- Name is just another column.
CREATE PROCEDURE [HTResourceMapper].[SavedView_Upsert]
    @SavedViewUid VARCHAR(40),
    @OwnerId VARCHAR(64),
    @Name NVARCHAR(200),
    @QueryString NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Result VARCHAR(10);

    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[SavedView]
               WHERE SavedViewUid = @SavedViewUid AND OwnerId = @OwnerId)
    BEGIN
        UPDATE [HTResourceMapper].[SavedView]
        SET Name = @Name, QueryString = @QueryString, UpdatedOn = SYSUTCDATETIME()
        WHERE SavedViewUid = @SavedViewUid AND OwnerId = @OwnerId;
        SET @Result = 'updated';
    END
    ELSE IF EXISTS (SELECT 1 FROM [HTResourceMapper].[SavedView] WHERE SavedViewUid = @SavedViewUid)
    BEGIN
        SET @Result = 'denied';
    END
    ELSE
    BEGIN
        -- New views land at the end of this owner's list.
        DECLARE @NextOrder INT =
            ISNULL((SELECT MAX(SortOrder) + 1 FROM [HTResourceMapper].[SavedView] WHERE OwnerId = @OwnerId), 0);

        INSERT INTO [HTResourceMapper].[SavedView] (SavedViewUid, OwnerId, Name, QueryString, SortOrder)
        VALUES (@SavedViewUid, @OwnerId, @Name, @QueryString, @NextOrder);
        SET @Result = 'created';
    END

    IF @Result = 'denied'
        SELECT @Result AS Result, CAST('' AS VARCHAR(40)) AS SavedViewUid;
    ELSE
        SELECT @Result AS Result, @SavedViewUid AS SavedViewUid;
END
