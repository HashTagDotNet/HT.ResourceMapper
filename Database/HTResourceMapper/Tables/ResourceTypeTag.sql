-- Please edit the table name accordingly.
CREATE TABLE [HTResourceMapper].[ResourceTypeTag]
(
	[ResourceTypeTagId] INT NOT NULL IDENTITY(1,1) 
		CONSTRAINT PK_ResourceTypeTag_ResourceTypeTagId PRIMARY KEY,
	[ResourceTypeId] INT NOT NULL 
		CONSTRAINT FK_ResourceTypeTag_ResourceType_ResourceTypeId FOREIGN KEY REFERENCES [HTResourceMapper].[ResourceType](ResourceTypeId),
	[Tag] NVARCHAR(40) NOT NULL,
	[TagValueTypeId] INT NOT NULL 
		CONSTRAINT FK_ResourceTypeTag_TagValueType_TagValueTypeId FOREIGN KEY REFERENCES [HTResourceMapper].[TagValueType](TagValueTypeId),		
)
