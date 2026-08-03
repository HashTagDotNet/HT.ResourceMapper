-- Management-screen read for the Resource Types page (PL-28). Same columns as ResourceType_GetAll
-- plus the two dependency counts the screen needs: how many resources use the type (drives the
-- blocked-delete rule) and how many entry-point template rows it owns.
--
-- Deliberately NOT folded into ResourceType_GetAll: that one is on the editor-open hot path and
-- should not pay for two correlated aggregates on every resource open.
CREATE PROCEDURE [HTResourceMapper].ResourceType_GetAllWithUsage
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        rt.ResourceTypeId,
        rt.ResourceTypeUid,
        rt.TypeName,
        rt.ShortCode,
        rt.IconKey,
        rt.AllowCustomTags,
        rt.CreatedOn,
        rt.UpdatedOn,
        ResourceCount = (
            SELECT COUNT(*)
            FROM [HTResourceMapper].[Resource] r
            WHERE r.ResourceTypeId = rt.ResourceTypeId
        ),
        EntryPointTagCount = (
            SELECT COUNT(*)
            FROM [HTResourceMapper].[ResourceTypeTag] rtt
            WHERE rtt.ResourceTypeId = rt.ResourceTypeId
        )
    FROM [HTResourceMapper].[ResourceType] rt
    ORDER BY rt.TypeName ASC;
END
