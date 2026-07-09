CREATE TABLE [HTResourceMapper].[TagContentType]
(
    -- Non-IDENTITY PK: content types are a constrained, seeded set with fixed ids (Text=1, Link=2).
    [TagContentTypeId] INT NOT NULL
        CONSTRAINT [PK_TagContentType_TagContentTypeId] PRIMARY KEY
    ,[TagCode] VARCHAR(50) NOT NULL
        CONSTRAINT [AK_TagContentType_TagCode] UNIQUE (TagCode)
        CONSTRAINT [CK_TagContentType_TagCode] CHECK ([TagCode] IN ('Text','Link'))
)
