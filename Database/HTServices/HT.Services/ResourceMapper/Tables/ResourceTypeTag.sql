-- Please edit the table name accordingly.
CREATE TABLE [HT.ResourceMapper].[ResourceTypeTag]
(
	[ResourceTypeTagId] INT NOT NULL IDENTITY(1,1) 
		CONSTRAINT PK_ResourceTypeTag_ResourceTypeTagId PRIMARY KEY,
	[ResourceTypeId] INT NOT NULL 
		CONSTRAINT FK_ResourceTypeTag_ResourceType_ResourceTypeId FOREIGN KEY REFERENCES [HT.ResourceMapper].[ResourceType](ResourceTypeId),
	[Tag] NVARCHAR(40) NOT NULL,
	[TagValueTypeId] INT NOT NULL 
		CONSTRAINT FK_ResourceTypeTag_TagValueType_TagValueTypeId FOREIGN KEY REFERENCES [HT.ResourceMapper].[TagValueType](TagValueTypeId),		
)
