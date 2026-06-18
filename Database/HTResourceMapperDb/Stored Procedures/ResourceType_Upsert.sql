CREATE PROCEDURE [HTResourceMapper].ResourceType_Upsert
    @TypeName         NVARCHAR(250),
    @ResourceTypeUid  VARCHAR(40),     -- used only on insert
    @AllowCustomTags  BIT,
    @OnConflict       VARCHAR(10),     -- 'upsert' | 'skip'
    @Result           VARCHAR(10) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Id INT;
    SELECT @Id = ResourceTypeId
    FROM [HTResourceMapper].[ResourceType]
    WHERE TypeName = @TypeName;

    IF @Id IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[ResourceType] (ResourceTypeUid, TypeName, AllowCustomTags)
        VALUES (@ResourceTypeUid, @TypeName, @AllowCustomTags);
        SET @Result = 'created';
    END
    ELSE IF @OnConflict = 'upsert'
    BEGIN
        UPDATE [HTResourceMapper].[ResourceType]
        SET AllowCustomTags = @AllowCustomTags,
            UpdatedOn = SYSUTCDATETIME()
        WHERE ResourceTypeId = @Id;
        SET @Result = 'updated';
    END
    ELSE
        SET @Result = 'skipped';
END
