-- Per-type entry-point template: which tags a resource type expects, and which is the default
-- primary link. References real TagDefinitions (FK), not a free string.
CREATE TABLE [HTResourceMapper].[ResourceTypeTag]
(
    [ResourceTypeTagId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ResourceTypeTag_ResourceTypeTagId] PRIMARY KEY
    ,[ResourceTypeId] INT NOT NULL
        CONSTRAINT [FK_ResourceTypeTag_ResourceType] FOREIGN KEY REFERENCES [HTResourceMapper].[ResourceType](ResourceTypeId)
    ,[TagDefinitionId] INT NOT NULL
        CONSTRAINT [FK_ResourceTypeTag_TagDefinition] FOREIGN KEY REFERENCES [HTResourceMapper].[TagDefinition](TagDefinitionId)
    -- Seeds a new resource's PrimaryTagDefinitionId. At most one per type (index below).
    ,[IsDefaultPrimary] BIT NOT NULL
        CONSTRAINT [DF_ResourceTypeTag_IsDefaultPrimary] DEFAULT (0)
    -- Optional per-type override of the definition's default RequirementLevel.
    ,[RequirementLevel] NVARCHAR(20) NULL
        CONSTRAINT [CK_ResourceTypeTag_RequirementLevel] CHECK ([RequirementLevel] IN ('Error','Suggested','Optional'))
    ,CONSTRAINT [UK_ResourceTypeTag_Type_Tag] UNIQUE ([ResourceTypeId], [TagDefinitionId])
)
GO
-- At most one default-primary entry point per resource type.
CREATE UNIQUE NONCLUSTERED INDEX [UX_ResourceTypeTag_DefaultPrimary]
    ON [HTResourceMapper].[ResourceTypeTag] ([ResourceTypeId])
    WHERE [IsDefaultPrimary] = 1;
