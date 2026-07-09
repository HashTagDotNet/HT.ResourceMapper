CREATE PROCEDURE [HTResourceMapper].Resource_Upsert
    @ResourceKey   NVARCHAR(250),
    @ResourceUid   VARCHAR(40),        -- used only on insert
    @TypeName      NVARCHAR(250) = NULL,
    @ResourceName  NVARCHAR(250),
    @Description   NVARCHAR(2000) = NULL,
    @Domain        NVARCHAR(2000) = NULL,   -- the resource's domain-tag value, if a domain tag is defined
    @OnConflict    VARCHAR(10),        -- 'upsert' | 'skip'
    @ResourceId    INT OUTPUT,         -- returned in all cases, including skip
    @Result        VARCHAR(10) OUTPUT  -- 'created' | 'updated' | 'skipped' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResourceTypeId INT =
        (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = @TypeName);

    -- Resource.ResourceTypeId is NOT NULL: a resource type is mandatory. Fail explicitly
    -- rather than attempt an insert/update that would violate the NOT NULL constraint.
    IF @ResourceTypeId IS NULL
    BEGIN
        SET @Result = 'error';
        SET @ResourceId = NULL;
        RETURN;
    END

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- Identity is (Domain + ResourceType + ResourceKey) when a domain tag is defined (matched
    -- via a join, since Domain is stored as a ResourceTag, not a Resource column); falls back to
    -- (ResourceType + ResourceKey) when no domain tag is defined (design §6 "Unused" state).
    IF @DomainTagDefId IS NOT NULL
    BEGIN
        SELECT @ResourceId = r.ResourceId
        FROM [HTResourceMapper].[Resource] r
        INNER JOIN [HTResourceMapper].[ResourceTag] rt
            ON rt.ResourceId = r.ResourceId AND rt.TagDefinitionId = @DomainTagDefId
        WHERE r.ResourceKey = @ResourceKey
          AND r.ResourceTypeId = @ResourceTypeId
          AND rt.TagValue = @Domain;
    END
    ELSE
    BEGIN
        SELECT @ResourceId = ResourceId
        FROM [HTResourceMapper].[Resource]
        WHERE ResourceKey = @ResourceKey
          AND ResourceTypeId = @ResourceTypeId;
    END

    IF @ResourceId IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[Resource]
            (ResourceUid, ResourceKey, ResourceTypeId, ResourceName, [Description])
        VALUES
            (@ResourceUid, @ResourceKey, @ResourceTypeId, @ResourceName, @Description);
        SET @ResourceId = SCOPE_IDENTITY();

        -- The upsert owns writing the domain tag on insert; ResourceTag_SetForResource (called
        -- afterwards for the resource's other tags) never touches it.
        IF @DomainTagDefId IS NOT NULL AND @Domain IS NOT NULL
        BEGIN
            INSERT INTO [HTResourceMapper].[ResourceTag] (ResourceId, TagDefinitionId, TagValue)
            VALUES (@ResourceId, @DomainTagDefId, @Domain);
        END

        SET @Result = 'created';
    END
    ELSE IF @OnConflict = 'upsert'
    BEGIN
        UPDATE [HTResourceMapper].[Resource]
        SET ResourceName   = @ResourceName,
            [Description]  = @Description,
            ResourceTypeId = @ResourceTypeId,
            UpdatedOn      = SYSUTCDATETIME()
        WHERE ResourceId = @ResourceId;
        SET @Result = 'updated';
    END
    ELSE
        SET @Result = 'skipped';   -- @ResourceId already holds the existing row's id
END
