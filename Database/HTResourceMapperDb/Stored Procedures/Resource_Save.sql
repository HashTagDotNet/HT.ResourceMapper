-- Editor create/update, keyed by the immutable ResourceUid (not by (type+key), which the
-- import upsert uses). The editor lets Key be renamed, so a key-based upsert would wrongly
-- insert a duplicate on rename. Type is frozen after creation: an update that tries to change
-- ResourceTypeId is rejected with @Result='error'. Does not touch tags or relationship edges.
CREATE PROCEDURE [HTResourceMapper].[Resource_Save]
    @ResourceUid             VARCHAR(40),
    @ResourceTypeId          INT,
    @ResourceKey             NVARCHAR(250),
    @ResourceName            NVARCHAR(250),
    @Description             NVARCHAR(2000) = NULL,
    @PrimaryTagDefinitionId  INT = NULL,
    @ResourceId              INT OUTPUT,
    @Result                  VARCHAR(10) OUTPUT  -- 'created' | 'updated' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ExistingResourceId INT;
    DECLARE @ExistingResourceTypeId INT;

    SELECT
        @ExistingResourceId = ResourceId,
        @ExistingResourceTypeId = ResourceTypeId
    FROM [HTResourceMapper].[Resource]
    WHERE ResourceUid = @ResourceUid;

    IF @ExistingResourceId IS NULL
    BEGIN
        INSERT INTO [HTResourceMapper].[Resource]
            (ResourceUid, ResourceKey, ResourceTypeId, ResourceName, [Description], PrimaryTagDefinitionId)
        VALUES
            (@ResourceUid, @ResourceKey, @ResourceTypeId, @ResourceName, @Description, @PrimaryTagDefinitionId);

        SET @ResourceId = SCOPE_IDENTITY();
        SET @Result = 'created';
    END
    ELSE IF @ExistingResourceTypeId <> @ResourceTypeId
    BEGIN
        -- Type is read-only after save; defense-in-depth (the service also enforces this).
        SET @ResourceId = @ExistingResourceId;
        SET @Result = 'error';
    END
    ELSE
    BEGIN
        UPDATE [HTResourceMapper].[Resource]
        SET ResourceKey             = @ResourceKey,
            ResourceName            = @ResourceName,
            [Description]           = @Description,
            PrimaryTagDefinitionId  = @PrimaryTagDefinitionId,
            UpdatedOn               = SYSUTCDATETIME()
        WHERE ResourceId = @ExistingResourceId;

        SET @ResourceId = @ExistingResourceId;
        SET @Result = 'updated';
    END
END
