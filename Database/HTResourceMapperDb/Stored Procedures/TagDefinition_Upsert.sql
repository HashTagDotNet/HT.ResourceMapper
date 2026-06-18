CREATE PROCEDURE [HTResourceMapper].TagDefinition_Upsert
    @TagDefinitionKey  NVARCHAR(50),
    @TagDefinitionUid  VARCHAR(40),       -- used only on insert
    @ContentType       VARCHAR(50),       -- free-form; auto-registered in TagContentType
    @AllowCustomValue  BIT,
    @IsMultiValued     BIT,
    @AllowedValues     NVARCHAR(2000),    -- JSON array string or NULL
    @OnConflict        VARCHAR(10),       -- 'upsert' | 'skip'
    @Result            VARCHAR(10) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Auto-register the content type if unseen. TagContentTypeId has no IDENTITY, so use MAX+1.
    -- (Low-volume single-import use; the MAX+1 race is acceptable per design.)
    DECLARE @ContentTypeId INT;
    SELECT @ContentTypeId = TagContentTypeId
    FROM [HTResourceMapper].[TagContentType]
    WHERE TagCode = @ContentType;

    IF @ContentTypeId IS NULL
    BEGIN
        SELECT @ContentTypeId = ISNULL(MAX(TagContentTypeId), -1) + 1
        FROM [HTResourceMapper].[TagContentType];

        INSERT INTO [HTResourceMapper].[TagContentType] (TagContentTypeId, TagCode)
        VALUES (@ContentTypeId, @ContentType);
    END

    DECLARE @Id INT;
    SELECT @Id = TagDefinitionId
    FROM [HTResourceMapper].[TagDefinition]
    WHERE TagDefinitionKey = @TagDefinitionKey;

    IF @Id IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[TagDefinition]
            (TagDefinitionUid, TagDefinitionKey, TagContentTypeId, AllowCustomValue, IsMultiValued, AllowedValues)
        VALUES
            (@TagDefinitionUid, @TagDefinitionKey, @ContentTypeId, @AllowCustomValue, @IsMultiValued, @AllowedValues);
        SET @Result = 'created';
    END
    ELSE IF @OnConflict = 'upsert'
    BEGIN
        UPDATE [HTResourceMapper].[TagDefinition]
        SET TagContentTypeId = @ContentTypeId,
            AllowCustomValue  = @AllowCustomValue,
            IsMultiValued     = @IsMultiValued,
            AllowedValues     = @AllowedValues,
            UpdatedOn         = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id;
        SET @Result = 'updated';
    END
    ELSE
        SET @Result = 'skipped';
END
