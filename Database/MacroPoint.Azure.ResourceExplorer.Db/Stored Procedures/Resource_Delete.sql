-- Deletes a resource and everything that references it: its relationship edges (both
-- directions, since the FKs are NO ACTION) and its tags. The caller is responsible for any
-- "are you sure" / dependents-warning confirmation before calling this.
CREATE PROCEDURE [ResourceExplorer].[Resource_Delete]
    @ResourceId INT
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRAN;

        DELETE FROM [ResourceExplorer].[ResourceRelationship]
        WHERE FromResourceId = @ResourceId
           OR ToResourceId = @ResourceId;

        DELETE FROM [ResourceExplorer].[ResourceTag]
        WHERE ResourceId = @ResourceId;

        DELETE FROM [ResourceExplorer].[Resource]
        WHERE ResourceId = @ResourceId;

    COMMIT;
END
