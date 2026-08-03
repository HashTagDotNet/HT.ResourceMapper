-- Deletes a resource type, but only when nothing points at it (PL-28).
--
-- Resource.ResourceTypeId is NOT NULL with FK_Resource_ResourceTypeId, so deleting a type that is
-- in use would otherwise fail as a raw FK violation. Instead we count the dependents up front and
-- return 'inuse' with @DependentCount so the caller can say "12 resources use this type".
--
-- ResourceTypeTag rows ARE removed: they are the type's own entry-point template, not an
-- independent record, so they die with it. Resources never do.
CREATE PROCEDURE [HTResourceMapper].ResourceType_Delete
    @ResourceTypeId  INT,
    @DependentCount  INT = 0 OUTPUT,
    @Result          VARCHAR(10) OUTPUT   -- 'deleted' | 'inuse' | 'notfound'
AS
BEGIN
    SET NOCOUNT ON;

    SET @DependentCount = 0;

    IF NOT EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceType] WHERE ResourceTypeId = @ResourceTypeId)
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    SELECT @DependentCount = COUNT(*)
    FROM [HTResourceMapper].[Resource]
    WHERE ResourceTypeId = @ResourceTypeId;

    IF @DependentCount > 0
    BEGIN
        SET @Result = 'inuse';
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        DELETE FROM [HTResourceMapper].[ResourceTypeTag]
        WHERE ResourceTypeId = @ResourceTypeId;

        DELETE FROM [HTResourceMapper].[ResourceType]
        WHERE ResourceTypeId = @ResourceTypeId;

        COMMIT TRANSACTION;
        SET @Result = 'deleted';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
