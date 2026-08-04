CREATE PROCEDURE [HTResourceMapper].TagDefinition_ForceDelete
    @TagDefinitionId    INT,
    @ValuesRemoved      INT = NULL OUTPUT,   -- ResourceTag rows deleted
    @TemplatesRemoved   INT = NULL OUTPUT,   -- ResourceTypeTag rows deleted
    @PrimaryLinksCleared INT = NULL OUTPUT,  -- resources whose primary link was reset to none
    @Result             VARCHAR(10) OUTPUT   -- 'deleted' | 'system' | 'notfound' | 'error'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- "Remove everywhere": deletes the tag AND every reference to it, in one transaction. The safe
    -- delete (TagDefinition_Delete) refuses while anything points at the tag; this is the deliberate
    -- override for when the answer really is "get rid of it".
    --
    -- What it destroys is metadata, never resources: the values resources had recorded for this tag,
    -- its place in any type's entry-point template, and its use as a resource's click-through link.
    -- Each count is reported so the caller can say exactly what was lost.
    --
    -- Order matters. Resource.PrimaryTagDefinitionId is a NO ACTION FK, so it has to be cleared BEFORE
    -- the definition row goes, or the delete fails as a constraint violation at the last step.
    --
    -- System-managed tags are still refused. That guard is not about safety of the cascade — it is that
    -- their existence is owned by deployment, so removing one here would be undone by the next publish
    -- while its data stayed destroyed.

    SET @ValuesRemoved = 0;
    SET @TemplatesRemoved = 0;
    SET @PrimaryLinksCleared = 0;

    IF NOT EXISTS (SELECT 1 FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionId = @TagDefinitionId)
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM [HTResourceMapper].[TagDefinition]
               WHERE TagDefinitionId = @TagDefinitionId AND (IsSystemTag = 1 OR IsDomainTag = 1))
    BEGIN
        SET @Result = 'system';
        RETURN;
    END

    BEGIN TRANSACTION;

        UPDATE [HTResourceMapper].[Resource]
        SET PrimaryTagDefinitionId = NULL,
            UpdatedOn = SYSUTCDATETIME()
        WHERE PrimaryTagDefinitionId = @TagDefinitionId;

        SET @PrimaryLinksCleared = @@ROWCOUNT;

        DELETE FROM [HTResourceMapper].[ResourceTag]
        WHERE TagDefinitionId = @TagDefinitionId;

        SET @ValuesRemoved = @@ROWCOUNT;

        DELETE FROM [HTResourceMapper].[ResourceTypeTag]
        WHERE TagDefinitionId = @TagDefinitionId;

        SET @TemplatesRemoved = @@ROWCOUNT;

        DELETE FROM [HTResourceMapper].[TagDefinition]
        WHERE TagDefinitionId = @TagDefinitionId;

    COMMIT TRANSACTION;

    SET @Result = 'deleted';
END
