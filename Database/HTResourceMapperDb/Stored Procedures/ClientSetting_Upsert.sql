CREATE PROCEDURE [HTResourceMapper].[ClientSetting_Upsert]
    @OwnerId VARCHAR(64),
    @SettingKey VARCHAR(100),
    @SettingJson NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[ClientSetting] WHERE OwnerId = @OwnerId AND SettingKey = @SettingKey)
        UPDATE [HTResourceMapper].[ClientSetting]
        SET SettingJson = @SettingJson, UpdatedOn = SYSUTCDATETIME()
        WHERE OwnerId = @OwnerId AND SettingKey = @SettingKey;
    ELSE
        INSERT INTO [HTResourceMapper].[ClientSetting] (OwnerId, SettingKey, SettingJson)
        VALUES (@OwnerId, @SettingKey, @SettingJson);
END
