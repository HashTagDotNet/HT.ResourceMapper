CREATE PROCEDURE [HTResourceMapper].ResourceTag_SetForResource
    @ResourceId INT,
    @Tags       [HTResourceMapper].[TagKeyValueList] READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- Replace all NON-DOMAIN tags for the resource atomically so a failed insert can't leave it
    -- tagless. The domain tag is written/owned by Resource_Upsert / Resource_Save and is never
    -- touched here — it survives the delete, and a domain-keyed row in @Tags (if the caller
    -- included one) is simply excluded from the insert.
    BEGIN TRAN;

        DELETE FROM [HTResourceMapper].[ResourceTag]
        WHERE ResourceId = @ResourceId
          AND (@DomainTagDefId IS NULL OR TagDefinitionId <> @DomainTagDefId);

        INSERT INTO [HTResourceMapper].[ResourceTag] (ResourceId, TagDefinitionId, TagValue)
        SELECT @ResourceId, td.TagDefinitionId, t.TagValue
        FROM @Tags t
        JOIN [HTResourceMapper].[TagDefinition] td
            ON td.TagDefinitionKey = t.TagDefinitionKey
        WHERE @DomainTagDefId IS NULL OR td.TagDefinitionId <> @DomainTagDefId;

    COMMIT;
END
