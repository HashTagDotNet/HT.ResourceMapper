-- Delete one of the owner's views. Deleting the default simply leaves the owner without one, which
-- the grid treats as "fall back to the resume setting" -- no special handling needed here.
CREATE PROCEDURE [HTResourceMapper].[SavedView_Delete]
    @OwnerId VARCHAR(64),
    @SavedViewUid VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [HTResourceMapper].[SavedView]
    WHERE OwnerId = @OwnerId AND SavedViewUid = @SavedViewUid;

    SELECT @@ROWCOUNT AS DeletedCount;
END
