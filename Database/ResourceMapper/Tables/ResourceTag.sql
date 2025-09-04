CREATE TABLE [HTResourceMapper].[ResourceTag]
(
    [ResourceTagId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ResourceTag_ResourceTagId] PRIMARY KEY (ResourceTagId)
    ,[ResourceId] INT NOT NULL
        CONSTRAINT [FK_ResourceTag_ResourceId] FOREIGN KEY (ResourceId) REFERENCES [HTResourceMapper].[Resource](ResourceId)
    ,[TagDefinitionId] INT NOT NULL
        CONSTRAINT [FK_ResourceTag_TagDefintionId] FOREIGN KEY ([TagDefinitionId]) REFERENCES [HTResourceMapper].[TagDefinition]([TagDefinitionId])
    ,[TagValue] NVARCHAR(2000) NOT NULL
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_ResourceTag_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL

)
