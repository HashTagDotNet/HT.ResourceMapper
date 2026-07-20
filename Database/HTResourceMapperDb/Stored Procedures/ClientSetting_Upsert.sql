CREATE PROCEDURE [HTResourceMapper].[ClientSetting_Upsert]
    @ClientId VARCHAR(64),
    @SettingKey VARCHAR(100),
    @SettingJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[ClientSetting] WHERE ClientId = @ClientId AND SettingKey = @SettingKey)
        UPDATE [HTResourceMapper].[ClientSetting]
        SET SettingJson = @SettingJson, UpdatedOn = SYSUTCDATETIME()
        WHERE ClientId = @ClientId AND SettingKey = @SettingKey;
    ELSE
        INSERT INTO [HTResourceMapper].[ClientSetting] (ClientId, SettingKey, SettingJson)
        VALUES (@ClientId, @SettingKey, @SettingJson);
END
