-- Generic per-client settings, keyed by the anonymous clientId (see design §7.1). First consumer:
-- the home grid's saved view ('home.gridView'). SettingJson is an opaque client string.
CREATE TABLE [HTResourceMapper].[ClientSetting]
(
    [ClientSettingId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ClientSetting_ClientSettingId] PRIMARY KEY (ClientSettingId)
    ,[ClientId] VARCHAR(64) NOT NULL
    ,[SettingKey] VARCHAR(100) NOT NULL
    ,[SettingJson] NVARCHAR(MAX) NOT NULL
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_ClientSetting_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
    ,CONSTRAINT [UK_ClientSetting_Client_Key] UNIQUE (ClientId, SettingKey)
)
