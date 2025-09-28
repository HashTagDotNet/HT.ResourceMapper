CREATE TABLE [HTResourceMapper].[TagDefinition]
(
    [TagDefinitionId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_TagDefinitons_TagDefinitionId] PRIMARY KEY (TagDefinitionId)
    ,[TagDefinitionUid] VARCHAR(40) NOT NULL
        CONSTRAINT [AK_TagDefinition_TagDefinitionUid] UNIQUE (TagDefinitionUid)
    ,[TagDefinitionKey] NVARCHAR(50) NOT NULL
        CONSTRAINT [UK_TagDefinition_TagDefinitionKey] UNIQUE (TagDefinitionKey)
    ,[TagContentTypeId] INT NOT NULL
        CONSTRAINT [FK_TagDefinition_TagContentTypeId] FOREIGN KEY (TagContentTypeId) REFERENCES [HTResourceMapper].TagContentType(TagContentTypeId)
    ,[AllowCustomValue] BIT NOT NULL
        CONSTRAINT [DF_TagDefinition_AllowCustomValue] DEFAULT (1)
    ,[IsMultiValued] BIT NOT NULL
        CONSTRAINT [DF_TagDefinition_IsMultiValued] DEFAULT (1)
    ,[AllowedValues] NVARCHAR(2000)
    ,[IsSystemTag] INT NOT NULL
        CONSTRAINT [DF_TagDefinition_IsSystemTag] DEFAULT (0)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_TagDefinition_CreatedOn] DEFAULT  (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
