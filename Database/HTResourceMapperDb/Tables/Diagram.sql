-- A saved explorer diagram, owned by an anonymous client (ClientId) with a separate public
-- share token (ShareId). DiagramJson is an opaque client-defined payload (nodes + positions +
-- expansion state); the server never inspects it. SeedResourceUid is a loose reference (no FK) —
-- the store is a decoupled client artifact. See docs/plans/explorer/resource-explorer-design-v1.md §7.
CREATE TABLE [HTResourceMapper].[Diagram]
(
    [DiagramId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_Diagram_DiagramId] PRIMARY KEY (DiagramId)
    ,[DiagramUid] VARCHAR(40) NOT NULL
        CONSTRAINT [UK_Diagram_DiagramUid] UNIQUE (DiagramUid)
    ,[ShareId] VARCHAR(40) NOT NULL
        CONSTRAINT [UK_Diagram_ShareId] UNIQUE (ShareId)
    ,[ClientId] VARCHAR(64) NOT NULL
    ,[Name] NVARCHAR(200) NOT NULL
    ,[SeedResourceUid] VARCHAR(40) NOT NULL
    ,[DisplayPreset] VARCHAR(20) NOT NULL
        CONSTRAINT [DF_Diagram_DisplayPreset] DEFAULT ('nameType')
    ,[DiagramJson] NVARCHAR(MAX) NOT NULL
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_Diagram_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
)
GO
-- Open-Recent lists a client's diagrams most-recent-first.
CREATE NONCLUSTERED INDEX [IX_Diagram_ClientId]
    ON [HTResourceMapper].[Diagram] ([ClientId]);
