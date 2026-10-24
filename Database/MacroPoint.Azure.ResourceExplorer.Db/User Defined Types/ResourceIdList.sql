-- A set of resource ids, e.g. the desired relationship targets for one direction of an editor save.
CREATE TYPE [ResourceExplorer].[ResourceIdList] AS TABLE
(
    [ResourceId] INT NOT NULL PRIMARY KEY
);
