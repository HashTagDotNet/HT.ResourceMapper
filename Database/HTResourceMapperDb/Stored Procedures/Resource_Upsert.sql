CREATE PROCEDURE [HTResourceMapper].Resource_Upsert
    @ResourceKey   NVARCHAR(250),
    @ResourceUid   VARCHAR(40),        -- used only on insert
    @TypeName      NVARCHAR(250) = NULL,
    @ResourceName  NVARCHAR(250),
    @Description   NVARCHAR(2000) = NULL,
    @OnConflict    VARCHAR(10),        -- 'upsert' | 'skip'
    @ResourceId    INT OUTPUT,         -- returned in all cases, including skip
    @Result        VARCHAR(10) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResourceTypeId INT =
        (SELECT ResourceTypeId FROM [HTResourceMapper].[ResourceType] WHERE TypeName = @TypeName);

    SELECT @ResourceId = ResourceId
    FROM [HTResourceMapper].[Resource]
    WHERE ResourceKey = @ResourceKey;

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
