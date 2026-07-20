CREATE PROCEDURE [HTResourceMapper].[ClientSetting_Get]
    @ClientId VARCHAR(64),
    @SettingKey VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SettingJson AS Value
    FROM [HTResourceMapper].[ClientSetting]
    WHERE ClientId = @ClientId AND SettingKey = @SettingKey;
END
