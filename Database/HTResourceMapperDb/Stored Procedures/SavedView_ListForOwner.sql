-- An owner's saved views, in manual order then by name. Drives both the menu and the manage page.
CREATE PROCEDURE [HTResourceMapper].[SavedView_ListForOwner]
    @OwnerId VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT SavedViewUid, Name, QueryString, SortOrder, IsDefault
    FROM [HTResourceMapper].[SavedView] WITH(NOLOCK)
    WHERE OwnerId = @OwnerId
    ORDER BY SortOrder, Name;
END
