-- A named grid query, owned by ICurrentIdentity.OwnerId.
--
-- QueryString is the grid's own '?n=...&s=...&f=...' payload and is OPAQUE to the server: nothing
-- here parses it, exactly as nothing parses Diagram.DiagramJson. That is what lets the filter
-- serialisation change shape without a migration -- ResourceFilterState.FromQueryString already
-- skips tokens it does not recognise.
--
-- NVARCHAR(MAX) is deliberate and measured: four filters over the catalog's highest-cardinality
-- tags already serialise to ~2100 characters, and that grows with the data. A 2000-char cap would
-- silently truncate a legitimate view.
CREATE TABLE [HTResourceMapper].[SavedView]
(
    [SavedViewId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_SavedView_SavedViewId] PRIMARY KEY (SavedViewId)
    ,[SavedViewUid] VARCHAR(40) NOT NULL
        CONSTRAINT [UK_SavedView_SavedViewUid] UNIQUE (SavedViewUid)
    ,[OwnerId] VARCHAR(64) NOT NULL
    ,[Name] NVARCHAR(200) NOT NULL
    ,[QueryString] NVARCHAR(MAX) NOT NULL
    ,[SortOrder] INT NOT NULL
        CONSTRAINT [DF_SavedView_SortOrder] DEFAULT (0)
    ,[IsDefault] BIT NOT NULL
        CONSTRAINT [DF_SavedView_IsDefault] DEFAULT (0)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_SavedView_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
    -- Names are how the menu identifies a view, so they must be unique per owner.
    ,CONSTRAINT [UK_SavedView_Owner_Name] UNIQUE ([OwnerId], [Name])
)
GO
-- The menu lists an owner's views on every page load.
CREATE NONCLUSTERED INDEX [IX_SavedView_OwnerId]
    ON [HTResourceMapper].[SavedView] ([OwnerId]);
GO
-- At most one default per owner, enforced by the database rather than by code remembering to clear
-- the previous one. Same filtered-unique-index pattern as UX_ResourceTypeTag_DefaultPrimary: it
-- makes the "two defaults" bug unrepresentable instead of merely unlikely.
CREATE UNIQUE NONCLUSTERED INDEX [UX_SavedView_Default]
    ON [HTResourceMapper].[SavedView] ([OwnerId])
    WHERE [IsDefault] = 1;
