CREATE PROCEDURE [HTResourceMapper].ResourceType_ReassignAndDelete
    @ResourceTypeId    INT,
    @NewResourceTypeId INT,
    @AffectedResources INT = NULL OUTPUT,   -- resources moved onto the new type
    @Conflicts         INT = NULL OUTPUT,   -- identity collisions that blocked the move
    @Result            VARCHAR(12) OUTPUT   -- 'reassigned' | 'notfound' | 'nonewtype' | 'same' | 'conflict' | 'error'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- "Remove everywhere" for a resource type: moves every resource of it onto another type, then drops
    -- the old type (and its entry-point template, which is the type's own record and dies with it).
    -- A move rather than a destruction for the same reason as the domain version: Resource.ResourceTypeId
    -- is NOT NULL, so there is no empty state, and the resources themselves are the catalog.
    --
    -- Identity is (Domain + Type + Key), enforced in code rather than by a constraint, so a move can
    -- silently create a duplicate: same Domain + Key already present under the target type. Counted and
    -- refused up front.
    --
    -- Deliberately NOT touching the moved resources' tags. Tag values are the user's data, and the target
    -- type's template only ever seeded NEW resources — re-seeding here would invent values nobody
    -- entered, while removing off-template ones would destroy values they did.

    SET @AffectedResources = 0;
    SET @Conflicts = 0;

    IF @ResourceTypeId = @NewResourceTypeId
    BEGIN
        SET @Result = 'same';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceType] WHERE ResourceTypeId = @ResourceTypeId)
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceType] WHERE ResourceTypeId = @NewResourceTypeId)
    BEGIN
        SET @Result = 'nonewtype';
        RETURN;
    END

    DECLARE @DomainTagDefId INT =
        (SELECT TagDefinitionId FROM [HTResourceMapper].[TagDefinition] WHERE IsDomainTag = 1);

    -- Same (Domain + Key) already under the target type => the move would duplicate an identity. With no
    -- domain tag designated, identity degenerates to (Type + Key) and the comparison follows suit.
    SELECT @Conflicts = COUNT(*)
    FROM [HTResourceMapper].[Resource] r
    WHERE r.ResourceTypeId = @ResourceTypeId
      AND EXISTS (
        SELECT 1
        FROM [HTResourceMapper].[Resource] t
        WHERE t.ResourceTypeId = @NewResourceTypeId
          AND UPPER(t.ResourceKey) = UPPER(r.ResourceKey)
          AND (
                @DomainTagDefId IS NULL
                OR ISNULL((SELECT TOP 1 UPPER(rt.TagValue) FROM [HTResourceMapper].[ResourceTag] rt
                           WHERE rt.ResourceId = t.ResourceId AND rt.TagDefinitionId = @DomainTagDefId), N'')
                 = ISNULL((SELECT TOP 1 UPPER(rt2.TagValue) FROM [HTResourceMapper].[ResourceTag] rt2
                           WHERE rt2.ResourceId = r.ResourceId AND rt2.TagDefinitionId = @DomainTagDefId), N'')
              ));

    IF @Conflicts > 0
    BEGIN
        SET @Result = 'conflict';
        RETURN;
    END

    BEGIN TRANSACTION;

        UPDATE [HTResourceMapper].[Resource]
        SET ResourceTypeId = @NewResourceTypeId,
            UpdatedOn      = SYSUTCDATETIME()
        WHERE ResourceTypeId = @ResourceTypeId;

        SET @AffectedResources = @@ROWCOUNT;

        DELETE FROM [HTResourceMapper].[ResourceTypeTag]
        WHERE ResourceTypeId = @ResourceTypeId;

        DELETE FROM [HTResourceMapper].[ResourceType]
        WHERE ResourceTypeId = @ResourceTypeId;

    COMMIT TRANSACTION;

    SET @Result = 'reassigned';
END
