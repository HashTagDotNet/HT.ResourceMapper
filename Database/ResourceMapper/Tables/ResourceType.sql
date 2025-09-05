CREATE TABLE [HTResourceMapper].[ResourceType]
(
    [ResourceTypeId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ResourceType_ResourceTypeId] PRIMARY KEY
    ,[ResourceTypeUid] VARCHAR(40) NOT NULL
        CONSTRAINT [AK_ResourceType_ResourceTypeUid] UNIQUE
    ,TypeName NVARCHAR(250) NOT NULL
        CONSTRAINT [UK_ResourceType_TypeName] UNIQUE
    ,AllowCustomTags BIT NOT NULL
        CONSTRAINT [DF_ResourceType_AllowCustomTags] DEFAULT (1)    
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_ResourceType_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
