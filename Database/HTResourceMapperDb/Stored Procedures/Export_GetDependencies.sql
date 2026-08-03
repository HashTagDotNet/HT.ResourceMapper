-- Every dependency edge (From = dependent, To = dependency) for the JSON export, in one query.
-- Both endpoints' domains are returned because the import format expresses a dependency as a bare
-- key resolved within the dependent's own domain: an edge whose endpoints sit in different domains
-- cannot be represented, so the service drops it and warns rather than emitting a key that would
-- fail import validation.
CREATE PROCEDURE [HTResourceMapper].[Export_GetDependencies]
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    SELECT
        rel.FromResourceId,
        f.ResourceKey  AS FromResourceKey,
        fd.TagValue    AS FromDomain,
        t.ResourceKey  AS ToResourceKey,
        td.TagValue    AS ToDomain
    FROM [HTResourceMapper].[ResourceRelationship] rel
    INNER JOIN [HTResourceMapper].[Resource] f ON f.ResourceId = rel.FromResourceId
    INNER JOIN [HTResourceMapper].[Resource] t ON t.ResourceId = rel.ToResourceId
    LEFT JOIN [HTResourceMapper].[ResourceTag] fd
        ON fd.ResourceId = f.ResourceId AND fd.TagDefinitionId = @DomainTagDefId
    LEFT JOIN [HTResourceMapper].[ResourceTag] td
        ON td.ResourceId = t.ResourceId AND td.TagDefinitionId = @DomainTagDefId
    ORDER BY rel.FromResourceId, t.ResourceKey;
END
