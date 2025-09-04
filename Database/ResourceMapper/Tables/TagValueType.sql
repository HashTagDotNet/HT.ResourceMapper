-- Please edit the table name accordingly.
CREATE TABLE [HTResourceMapper].[TagValueType]
(
	[TagValueTypeId] INT NOT NULL
		CONSTRAINT PK_TagValueType_TagValueTypeId PRIMARY KEY,
	[Code] NVARCHAR(50) NOT NULL
		CONSTRAINT UQ_TagValueType_Code UNIQUE,
	[IsRequired] BIT NOT NULL
		CONSTRAINT DF_TagValueType_IsRequired DEFAULT 0,
)
