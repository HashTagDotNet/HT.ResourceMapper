CREATE TYPE [HTResourceMapper].[TagKeyValueList] AS TABLE
(
    [TagDefinitionKey] NVARCHAR(50) NOT NULL,
    [TagValue]         NVARCHAR(2000) NULL
);
