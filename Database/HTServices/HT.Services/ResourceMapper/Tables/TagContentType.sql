CREATE TABLE [HT.ResourceMapper].[TagContentType]
(
    [TagContentTypeId] INT NOT NULL
        CONSTRAINT [PK_TagContentType_TagTypeId] PRIMARY KEY
    ,[TagCode] VARCHAR(50)
        CONSTRAINT [AK_TagContentType_TagCode] UNIQUE (TagCode)        
)
