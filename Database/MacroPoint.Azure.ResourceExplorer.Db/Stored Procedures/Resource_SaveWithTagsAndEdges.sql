-- Editor save in one transaction: the resource row (via Resource_Save), its non-domain tags (via
-- ResourceTag_SetForResource) and both directions of its relationship edges. Edges are a
-- full-set replace per direction: @DependsOnIds are this resource's out-edges (from=this,
-- to=target); @DependentOnIds are its in-edges (from=target, to=this). Each direction touches only
-- its own edges. A 'error' result from Resource_Save (type or domain changed) rolls back and writes
-- nothing.
CREATE PROCEDURE [ResourceExplorer].[Resource_SaveWithTagsAndEdges]
    @ResourceUid             VARCHAR(40),
    @ResourceTypeId          INT,
    @ResourceKey             NVARCHAR(250),
    @ResourceName            NVARCHAR(250),
    @Description             NVARCHAR(2000) = NULL,
    @Domain                  NVARCHAR(2000) = NULL,
    @PrimaryTagDefinitionId  INT = NULL,
    @Tags                    [ResourceExplorer].[TagKeyValueList] READONLY,
    @DependsOnIds            [ResourceExplorer].[ResourceIdList] READONLY,
    @DependentOnIds          [ResourceExplorer].[ResourceIdList] READONLY,
    @ResourceId              INT OUTPUT,
    @Result                  VARCHAR(10) OUTPUT  -- 'created' | 'updated' | 'error'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRAN;

        EXEC [ResourceExplorer].[Resource_Save]
            @ResourceUid            = @ResourceUid,
            @ResourceTypeId         = @ResourceTypeId,
            @ResourceKey            = @ResourceKey,
            @ResourceName           = @ResourceName,
            @Description            = @Description,
            @Domain                 = @Domain,
            @PrimaryTagDefinitionId = @PrimaryTagDefinitionId,
            @ResourceId             = @ResourceId OUTPUT,
            @Result                 = @Result OUTPUT;

        IF @Result = 'error'
        BEGIN
            ROLLBACK;
            RETURN;
        END

        EXEC [ResourceExplorer].[ResourceTag_SetForResource]
            @ResourceId = @ResourceId,
            @Tags       = @Tags;

        DELETE rel
        FROM [ResourceExplorer].[ResourceRelationship] rel
        WHERE rel.FromResourceId = @ResourceId
          AND NOT EXISTS (SELECT 1 FROM @DependsOnIds d WHERE d.ResourceId = rel.ToResourceId);

        INSERT INTO [ResourceExplorer].[ResourceRelationship] (FromResourceId, ToResourceId)
        SELECT @ResourceId, d.ResourceId
        FROM @DependsOnIds d
        WHERE NOT EXISTS (
            SELECT 1 FROM [ResourceExplorer].[ResourceRelationship] rel
            WHERE rel.FromResourceId = @ResourceId AND rel.ToResourceId = d.ResourceId
        );

        DELETE rel
        FROM [ResourceExplorer].[ResourceRelationship] rel
        WHERE rel.ToResourceId = @ResourceId
          AND NOT EXISTS (SELECT 1 FROM @DependentOnIds d WHERE d.ResourceId = rel.FromResourceId);

        INSERT INTO [ResourceExplorer].[ResourceRelationship] (FromResourceId, ToResourceId)
        SELECT d.ResourceId, @ResourceId
        FROM @DependentOnIds d
        WHERE NOT EXISTS (
            SELECT 1 FROM [ResourceExplorer].[ResourceRelationship] rel
            WHERE rel.FromResourceId = d.ResourceId AND rel.ToResourceId = @ResourceId
        );

    COMMIT;
END
