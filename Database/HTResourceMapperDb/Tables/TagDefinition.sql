CREATE TABLE [HTResourceMapper].[TagDefinition]
(
    [TagDefinitionId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_TagDefinitions_TagDefinitionId] PRIMARY KEY (TagDefinitionId)
    ,[TagDefinitionUid] VARCHAR(40) NOT NULL
        CONSTRAINT [AK_TagDefinition_TagDefinitionUid] UNIQUE (TagDefinitionUid)
    -- Stable internal key (used for identity/import/wire). Never shown raw in the UI.
    ,[TagDefinitionKey] NVARCHAR(50) NOT NULL
        CONSTRAINT [UK_TagDefinition_TagDefinitionKey] UNIQUE (TagDefinitionKey)
    -- User-facing label; UI/queries COALESCE(DisplayName, TagDefinitionKey).
    ,[DisplayName] NVARCHAR(100) NULL
    ,[TagContentTypeId] INT NOT NULL
        CONSTRAINT [FK_TagDefinition_TagContentTypeId] FOREIGN KEY (TagContentTypeId) REFERENCES [HTResourceMapper].TagContentType(TagContentTypeId)
    ,[AllowCustomValue] BIT NOT NULL
        CONSTRAINT [DF_TagDefinition_AllowCustomValue] DEFAULT (1)
    ,[IsMultiValued] BIT NOT NULL
        CONSTRAINT [DF_TagDefinition_IsMultiValued] DEFAULT (1)
    ,[AllowedValues] NVARCHAR(2000)
    -- How the tag is treated in validation: Error (required) | Suggested | Optional.
    ,[RequirementLevel] NVARCHAR(20) NOT NULL
        CONSTRAINT [DF_TagDefinition_RequirementLevel] DEFAULT ('Optional')
        CONSTRAINT [CK_TagDefinition_RequirementLevel] CHECK ([RequirementLevel] IN ('Error','Suggested','Optional'))
    -- Designates the single boundary/domain tag (identity scope). At most one row = 1 (index below).
    ,[IsDomainTag] BIT NOT NULL
        CONSTRAINT [DF_TagDefinition_IsDomainTag] DEFAULT (0)
    -- Editability/provenance: system-managed (user cannot edit) vs user-created. Still visible.
    ,[IsSystemTag] BIT NOT NULL
        CONSTRAINT [DF_TagDefinition_IsSystemTag] DEFAULT (0)
    -- Admin-defined grid ordering (lower sorts first); unset tags default to the end.
    ,[DisplayOrder] INT NOT NULL
        CONSTRAINT [DF_TagDefinition_DisplayOrder] DEFAULT (1000)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_TagDefinition_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
GO
-- Enforce at most one designated domain/boundary tag.
CREATE UNIQUE NONCLUSTERED INDEX [UX_TagDefinition_SingleDomainTag]
    ON [HTResourceMapper].[TagDefinition] ([IsDomainTag])
    WHERE [IsDomainTag] = 1;
