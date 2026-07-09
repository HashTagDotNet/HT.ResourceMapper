CREATE PROCEDURE [HTResourceMapper].Resource_Upsert
    @ResourceKey   NVARCHAR(250),
    @ResourceUid   VARCHAR(40),        -- used only on insert
    @TypeName      NVARCHAR(250) = NULL,
    @ResourceName  NVARCHAR(250),
    @Description   NVARCHAR(2000) = NULL,
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

    -- Identity is (ResourceType + ResourceKey) here; Domain is added to this predicate in
    -- slice #5 once Domain is written/read as a tag value (IsDomainTag = 1).
    SELECT @ResourceId = ResourceId
    FROM [HTResourceMapper].[Resource]
    WHERE ResourceKey = @ResourceKey
      AND ResourceTypeId = @ResourceTypeId;

    IF @ResourceId IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[Resource]
            (ResourceUid, ResourceKey, ResourceTypeId, ResourceName, [Description])
        VALUES
            (@ResourceUid, @ResourceKey, @ResourceTypeId, @ResourceName, @Description);
        SET @ResourceId = SCOPE_IDENTITY();
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
