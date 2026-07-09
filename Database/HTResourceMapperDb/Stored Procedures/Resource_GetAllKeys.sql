CREATE PROCEDURE [HTResourceMapper].Resource_GetAllKeys
AS
BEGIN
    SET NOCOUNT ON;

    -- Bulk identity lookup for import conflict/dedup/dependency-resolution checks. Returns the
    -- full (Domain + Type + Key) tuple per resource (Domain is NULL when no domain tag is
    -- applied, e.g. no domain tag is defined at all — design §6 "Unused" state).
    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    SELECT
        r.ResourceId,
        r.ResourceKey,
        rty.TypeName,
        dt.TagValue AS Domain
    FROM [HTResourceMapper].[Resource] r
    INNER JOIN [HTResourceMapper].[ResourceType] rty ON rty.ResourceTypeId = r.ResourceTypeId
    LEFT JOIN [HTResourceMapper].[ResourceTag] dt
        ON dt.ResourceId = r.ResourceId AND dt.TagDefinitionId = @DomainTagDefId;
END
