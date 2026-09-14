CREATE PROCEDURE [HTResourceMapper].[ClientSetting_Get]
    @OwnerId VARCHAR(64),
    @SettingKey VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SettingJson AS Value
    FROM [HTResourceMapper].[ClientSetting]
    WHERE OwnerId = @OwnerId AND SettingKey = @SettingKey;
END
