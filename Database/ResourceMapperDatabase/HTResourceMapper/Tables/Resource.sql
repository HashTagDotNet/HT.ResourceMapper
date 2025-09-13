CREATE TABLE [HTResourceMapper].[Resource]
(
    [ResourceId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_Resource_ResourceId] PRIMARY KEY (ResourceId)
    ,ResourceUid VARCHAR(40) NOT NULL
        CONSTRAINT [UK_Resource_ResourceUid] UNIQUE (ResourceUid)
    ,[ResourceKey] NVARCHAR(250) NOT NULL
        CONSTRAINT [UK_Resource_ResourceKey] UNIQUE(ResourceKey)
    ,[ResourceTypeId] INT NULL
        CONSTRAINT [FK_Resource_ResourceTypeId] FOREIGN KEY (ResourceTypeId) REFERENCES [HTResourceMapper].[ResourceType](ResourceTypeId)
    ,[ResourceName] NVARCHAR(250) NULL       
    ,[Description] NVARCHAR(2000) NULL
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_Resource_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
