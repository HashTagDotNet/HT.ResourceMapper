CREATE TABLE [HTResourceMapper].[Resource]
(
    [ResourceId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_Resource_ResourceId] PRIMARY KEY (ResourceId)
    ,ResourceUid VARCHAR(40) NOT NULL
        CONSTRAINT [UK_Resource_ResourceUid] UNIQUE (ResourceUid)
    -- Raw business key. Unique per (Domain + Type + Key) is enforced in code, not the DB
    -- (Domain is a tag, so the full triple can't be a single DB constraint). No global unique.
    ,[ResourceKey] NVARCHAR(250) NOT NULL
    ,[ResourceTypeId] INT NOT NULL
        CONSTRAINT [FK_Resource_ResourceTypeId] FOREIGN KEY (ResourceTypeId) REFERENCES [HTResourceMapper].[ResourceType](ResourceTypeId)
    ,[ResourceName] NVARCHAR(250) NOT NULL
    ,[Description] NVARCHAR(2000) NULL
    -- Default click-through Link tag (the "primary" entry point). NO ACTION: a TagDefinition
    -- that is some resource's primary cannot be deleted until unreferenced.
    ,[PrimaryTagDefinitionId] INT NULL
        CONSTRAINT [FK_Resource_PrimaryTagDefinitionId] FOREIGN KEY (PrimaryTagDefinitionId) REFERENCES [HTResourceMapper].[TagDefinition](TagDefinitionId)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_Resource_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
GO
-- Non-unique lookup index on the raw key (uniqueness of the (Domain+Type+Key) triple is code-enforced).
CREATE NONCLUSTERED INDEX [IX_Resource_ResourceKey]
    ON [HTResourceMapper].[Resource] ([ResourceKey]);
