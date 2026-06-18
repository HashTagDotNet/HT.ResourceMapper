CREATE PROCEDURE [HTResourceMapper].Resource_GetAllKeys
AS
BEGIN
    SET NOCOUNT ON;

    -- Slice 1: bulk existing-key lookup for import conflict/dry-run checks.
    -- Returns every resource key; the service intersects in C# (case-insensitive).
    -- A TVP-based "keys-in-payload" variant is deferred to the write slice.
    SELECT r.ResourceKey
    FROM [HTResourceMapper].[Resource] r;
END
