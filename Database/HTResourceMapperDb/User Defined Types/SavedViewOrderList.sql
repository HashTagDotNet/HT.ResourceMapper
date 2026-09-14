-- One row per saved view, carrying its new position. Lets a drag-reorder of the whole list commit
-- in a single round trip rather than one UPDATE per row.
CREATE TYPE [HTResourceMapper].[SavedViewOrderList] AS TABLE
(
    [SavedViewUid] VARCHAR(40) NOT NULL,
    [SortOrder]    INT         NOT NULL
);
