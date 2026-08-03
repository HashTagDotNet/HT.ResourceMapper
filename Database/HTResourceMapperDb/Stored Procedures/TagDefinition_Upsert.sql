CREATE PROCEDURE [HTResourceMapper].TagDefinition_Upsert
    @TagDefinitionKey  NVARCHAR(50),
    @TagDefinitionUid  VARCHAR(40),       -- used only on insert
    @ContentType       VARCHAR(50),       -- must match a seeded TagContentType.TagCode ('Text'|'Link')
    @AllowCustomValue  BIT,
    @IsMultiValued     BIT,
    @AllowedValues     NVARCHAR(2000) = NULL,  -- JSON array string or NULL (NULL clears the vocabulary)
    -- Presentation/identity metadata. These five are NOT expressible in the import document, so
    -- import cannot supply them. NULL therefore means "caller has no opinion": use the documented
    -- default on INSERT, and LEAVE THE EXISTING VALUE ALONE on UPDATE (punchlist PL-49). Before
    -- this, omitting them silently reset the row - including clearing IsDomainTag, the anchor of
    -- the (Domain + Type + Key) identity model - on every import that named an existing tag.
    @DisplayName       NVARCHAR(100) = NULL,
    @RequirementLevel  NVARCHAR(20)  = NULL,   -- 'Error' | 'Suggested' | 'Optional'; INSERT default 'Optional'
    @IsDomainTag       BIT = NULL,             -- INSERT default 0; can be set but never cleared here
    @IsSystemTag       BIT = NULL,             -- INSERT default 0; can be set but never cleared here
    @DisplayOrder      INT = NULL,             -- INSERT default 1000
    @OnConflict        VARCHAR(10),       -- 'upsert' | 'skip'
    @TagDefinitionId   INT = NULL OUTPUT, -- the definition's id, in all non-error cases (optional: import doesn't pass it)
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
        SET @TagDefinitionId = NULL;
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
        -- ...but never take the designation off a SYSTEM-managed tag. That path would have
        -- cleared IsDomainTag on the deployment-owned 'Domain' row while bypassing the
        -- monotonic guard below (it targets other rows), reintroducing the PL-49 failure from
        -- the opposite direction. Refuse loudly rather than half-applying the move.
        IF EXISTS (SELECT 1 FROM [HTResourceMapper].[TagDefinition]
                   WHERE IsDomainTag = 1 AND IsSystemTag = 1
                     AND TagDefinitionId <> ISNULL(@Id, -1))
        BEGIN
            SET @Result = 'error';
            SET @TagDefinitionId = @Id;
            RETURN;
        END

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
             @IsMultiValued, @AllowedValues, ISNULL(@RequirementLevel, 'Optional'),
             ISNULL(@IsDomainTag, 0), ISNULL(@IsSystemTag, 0), ISNULL(@DisplayOrder, 1000));
        SET @TagDefinitionId = SCOPE_IDENTITY();
        SET @Result = 'created';
    END
    ELSE IF @OnConflict = 'upsert'
    BEGIN
        -- A system tag's shape is owned by deployment (Script.PostDeployment1.sql), not by user
        -- data. Without this, an import naming 'Domain' silently turned the required, restricted
        -- ["prod","non-prod"] vocabulary into a free-text field: AllowCustomValue 0 -> 1 and
        -- AllowedValues -> NULL. Since resource identity is (Domain + Type + Key), that lets any
        -- typo mint a whole new identity namespace. Report 'skipped' so the caller can tally it
        -- rather than believing it wrote something.
        IF EXISTS (SELECT 1 FROM [HTResourceMapper].[TagDefinition]
                   WHERE TagDefinitionId = @Id AND IsSystemTag = 1)
        BEGIN
            SET @TagDefinitionId = @Id;
            SET @Result = 'skipped';
            RETURN;
        END

        UPDATE [HTResourceMapper].[TagDefinition]
        SET TagContentTypeId  = @ContentTypeId,
            AllowCustomValue  = @AllowCustomValue,
            IsMultiValued     = @IsMultiValued,
            AllowedValues     = @AllowedValues,
            -- NULL = preserve (see the parameter block).
            DisplayName       = ISNULL(@DisplayName, DisplayName),
            RequirementLevel  = ISNULL(@RequirementLevel, RequirementLevel),
            DisplayOrder      = ISNULL(@DisplayOrder, DisplayOrder),
            -- Identity flags are monotonic: a caller may SET them, but no update path may clear
            -- them. Losing IsDomainTag breaks identity resolution for the whole catalog, and
            -- nothing in the UI would surface that it happened. Moving the domain designation to
            -- another row still works - the block above clears the previous holder explicitly.
            IsDomainTag       = CASE WHEN @IsDomainTag = 1 THEN 1 ELSE IsDomainTag END,
            IsSystemTag       = CASE WHEN @IsSystemTag = 1 THEN 1 ELSE IsSystemTag END,
            UpdatedOn         = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id;
        SET @TagDefinitionId = @Id;
        SET @Result = 'updated';
    END
    ELSE
    BEGIN
        SET @TagDefinitionId = @Id;
        SET @Result = 'skipped';
    END
END
