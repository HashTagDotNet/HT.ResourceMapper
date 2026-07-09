-- Directed relationship edge between resources. Untyped for now (implicitly "DependsOn":
-- From = dependent, To = dependency). A typed multigraph (relationshipTypeId + RelationshipType)
-- is deferred until the producer/consumer view is built.
CREATE TABLE [HTResourceMapper].[ResourceRelationship]
(
    [RelationshipId] INT NOT NULL IDENTITY(1,1)
        CONSTRAINT [PK_ResourceRelationship_RelationshipId] PRIMARY KEY (RelationshipId)
    ,[FromResourceId] INT NOT NULL
        CONSTRAINT [FK_ResourceRelationship_FromResource] FOREIGN KEY (FromResourceId) REFERENCES [HTResourceMapper].[Resource](ResourceId)
    ,[ToResourceId] INT NOT NULL
        CONSTRAINT [FK_ResourceRelationship_ToResource] FOREIGN KEY (ToResourceId) REFERENCES [HTResourceMapper].[Resource](ResourceId)
    ,[CreatedOn] DateTime2(0) NOT NULL
        CONSTRAINT [DF_ResourceRelationship_CreatedOn] DEFAULT (SYSUTCDATETIME())
    ,[UpdatedOn] DateTime2(0) NULL
    -- No duplicate edges; no self-loops.
    ,CONSTRAINT [UK_ResourceRelationship_From_To] UNIQUE ([FromResourceId], [ToResourceId])
    ,CONSTRAINT [CK_ResourceRelationship_NoSelfLoop] CHECK ([FromResourceId] <> [ToResourceId])
    -- FKs are NO ACTION (two FKs to the same table can't both cascade). Cascade-delete of a
    -- resource's edges is handled in the delete stored procedure (slice 2).
)
