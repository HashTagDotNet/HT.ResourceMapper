CREATE PROCEDURE [HTResourceMapper].TagDefinition_Upsert
    @TagDefinitionKey  NVARCHAR(50),
    @TagDefinitionUid  VARCHAR(40),       -- used only on insert
    @ContentType       VARCHAR(50),       -- must match a seeded TagContentType.TagCode ('Text'|'Link')
    @AllowCustomValue  BIT,
    @IsMultiValued     BIT,
    @AllowedValues     NVARCHAR(2000) = NULL,  -- JSON array string or NULL
    @DisplayName       NVARCHAR(100) = NULL,
    @RequirementLevel  NVARCHAR(20)  = 'Optional',  -- 'Error' | 'Suggested' | 'Optional'
    @IsDomainTag       BIT = 0,
    @IsSystemTag       BIT = 0,
    @DisplayOrder      INT = 1000,
    @OnConflict        VARCHAR(10),       -- 'upsert' | 'skip'
    @Result            VARCHAR(10) OUTPUT -- 'created' | 'updated' | 'skipped' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    -- ContentType is a constrained, seeded set (Text, Link) - no auto-registration.
    DECLARE @ContentTypeId INT;
    SELECT @ContentTypeId = TagContentTypeId
    FROM [HTResourceMapper].[TagContentType]
    WHERE TagCode = @ContentType;

    IF @ContentTypeId IS NULL
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    DECLARE @Id INT;
    SELECT @Id = TagDefinitionId
    FROM [HTResourceMapper].[TagDefinition]
    WHERE TagDefinitionKey = @TagDefinitionKey;

    -- At most one designated domain tag (also enforced by UX_TagDefinition_SingleDomainTag).
    -- Clear any other row's flag first so setting a new domain tag doesn't violate the index.
    IF @IsDomainTag = 1
    BEGIN
        UPDATE [HTResourceMapper].[TagDefinition]
        SET IsDomainTag = 0
        WHERE IsDomainTag = 1
          AND TagDefinitionId <> ISNULL(@Id, -1);
    END

    IF @Id IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[TagDefinition]
            (TagDefinitionUid, TagDefinitionKey, DisplayName, TagContentTypeId, AllowCustomValue,
             IsMultiValued, AllowedValues, RequirementLevel, IsDomainTag, IsSystemTag, DisplayOrder)
        VALUES
            (@TagDefinitionUid, @TagDefinitionKey, @DisplayName, @ContentTypeId, @AllowCustomValue,
             @IsMultiValued, @AllowedValues, @RequirementLevel, @IsDomainTag, @IsSystemTag, @DisplayOrder);
        SET @Result = 'created';
    END
    ELSE IF @OnConflict = 'upsert'
    BEGIN
        UPDATE [HTResourceMapper].[TagDefinition]
        SET TagContentTypeId  = @ContentTypeId,
            AllowCustomValue  = @AllowCustomValue,
            IsMultiValued     = @IsMultiValued,
            AllowedValues     = @AllowedValues,
            DisplayName       = @DisplayName,
            RequirementLevel  = @RequirementLevel,
            IsDomainTag       = @IsDomainTag,
            IsSystemTag       = @IsSystemTag,
            DisplayOrder      = @DisplayOrder,
            UpdatedOn         = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id;
        SET @Result = 'updated';
    END
    ELSE
        SET @Result = 'skipped';
END
