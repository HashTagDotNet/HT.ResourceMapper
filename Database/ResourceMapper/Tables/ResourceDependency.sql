CREATE TABLE [HTResourceMapper].[ResourceDependency]
(
    [ResourceDependencyId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ResourceDependency_ResourceDependencyId] PRIMARY KEY (ResourceDependencyId)
    ,[ResourceId] INT NOT NULL
        CONSTRAINT [FK_ResourceId_ResourceDependency_Resources] FOREIGN KEY (ResourceId) REFERENCES [HTResourceMapper].[Resource](ResourceId)
    ,[DependencyResourceId] INT NOT NULL
        CONSTRAINT [FK_DependencyResourceId_ResourcesDependency_ResourceId] FOREIGN KEY (DependencyResourceId) REFERENCES [HTResourceMapper].[Resource](ResourceId)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_ResourceDependency_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
