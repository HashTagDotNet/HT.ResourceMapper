-- Apply a whole new ordering in one round trip. Rows not named in @Order keep their position, and
-- uids belonging to another owner are excluded by the OwnerId predicate rather than erroring.
CREATE PROCEDURE [ResourceExplorer].[SavedView_Reorder]
    @OwnerId VARCHAR(64),
    @Order [ResourceExplorer].[SavedViewOrderList] READONLY
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE sv
    SET SortOrder = o.SortOrder, UpdatedOn = SYSUTCDATETIME()
    FROM [ResourceExplorer].[SavedView] sv
    INNER JOIN @Order o ON o.SavedViewUid = sv.SavedViewUid
    WHERE sv.OwnerId = @OwnerId;

    SELECT @@ROWCOUNT AS UpdatedCount;
END
