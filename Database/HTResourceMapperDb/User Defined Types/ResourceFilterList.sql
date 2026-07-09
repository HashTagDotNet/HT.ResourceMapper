CREATE TYPE [HTResourceMapper].[ResourceFilterList] AS TABLE
(
    [FilterIndex]  TINYINT        NOT NULL,   -- groups the multi-value rows of one filter
    [FilterColumn] NVARCHAR(50)   NOT NULL,   -- 'ResourceType' | 'ResourceName' | 'Description' | 'Tag'
    [TagKey]       NVARCHAR(50)   NULL,       -- the tag key; set iff FilterColumn = 'Tag'
    [Operator]     NVARCHAR(20)   NOT NULL,   -- 'Equals' | 'NotEquals' | 'Contains'
    [FilterValue]  NVARCHAR(2100) NULL,       -- enumerable/tag value, or contains-text
    [IsBlank]      BIT            NOT NULL DEFAULT (0)  -- 1 => this row targets the NULL/blank bucket
);
