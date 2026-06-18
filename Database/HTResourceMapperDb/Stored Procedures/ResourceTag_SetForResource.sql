CREATE PROCEDURE [HTResourceMapper].ResourceTag_SetForResource
    @ResourceId INT,
    @Tags       [HTResourceMapper].[TagKeyValueList] READONLY
AS
BEGIN
    SET NOCOUNT ON;

    -- Replace all tags for the resource atomically so a failed insert can't leave it tagless.
    BEGIN TRAN;

        DELETE FROM [HTResourceMapper].[ResourceTag]
        WHERE ResourceId = @ResourceId;

        INSERT INTO [HTResourceMapper].[ResourceTag] (ResourceId, TagDefinitionId, TagValue)
        SELECT @ResourceId, td.TagDefinitionId, t.TagValue
        FROM @Tags t
        JOIN [HTResourceMapper].[TagDefinition] td
            ON td.TagDefinitionKey = t.TagDefinitionKey;

    COMMIT;
END
