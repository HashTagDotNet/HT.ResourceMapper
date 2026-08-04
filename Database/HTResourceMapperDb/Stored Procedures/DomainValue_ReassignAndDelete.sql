CREATE PROCEDURE [HTResourceMapper].DomainValue_ReassignAndDelete
    @OldValue          NVARCHAR(200),
    @NewValue          NVARCHAR(200),
    @AffectedResources INT = NULL OUTPUT,   -- resources moved onto @NewValue
    @Conflicts         INT = NULL OUTPUT,   -- identity collisions that blocked the move
    @Result            VARCHAR(12) OUTPUT   -- 'reassigned' | 'notfound' | 'nonewvalue' | 'same' | 'conflict' | 'error'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- "Remove everywhere" for a domain value: moves every resource in it onto another value, then drops
    -- the old one. A move rather than a destruction, because Subscription is REQUIRED to save a resource
    -- — there is no empty state to leave these resources in, so a plain cascade delete would either
    -- destroy them or strand them unsaveable.
    --
    -- The conflict check is the load-bearing part. Resource identity is (Domain + Type + Key) and is
    -- enforced in code, not by a constraint, so moving a resource to another domain can silently create
    -- a duplicate identity — same Type + Key already sitting in the target. This counts those first and
    -- refuses the whole operation rather than manufacturing duplicates the app believes cannot exist.

    SET @AffectedResources = 0;
    SET @Conflicts = 0;

    DECLARE @Id INT, @AllowedValues NVARCHAR(2000);

    SELECT @Id = TagDefinitionId, @AllowedValues = AllowedValues
    FROM [HTResourceMapper].[TagDefinition]
    WHERE IsDomainTag = 1;

    IF @Id IS NULL
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    SET @OldValue = LTRIM(RTRIM(@OldValue));
    SET @NewValue = LTRIM(RTRIM(@NewValue));

    IF @OldValue = N'' OR @NewValue = N''
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    IF UPPER(@OldValue) = UPPER(@NewValue)
    BEGIN
        SET @Result = 'same';
        RETURN;
    END

    SET @AllowedValues = NULLIF(ISNULL(@AllowedValues, N'[]'), N'');

    DECLARE @OldListed BIT = CASE WHEN EXISTS (
        SELECT 1 FROM OPENJSON(@AllowedValues) WHERE UPPER([value]) = UPPER(@OldValue)) THEN 1 ELSE 0 END;

    DECLARE @OldUsed INT = (
        SELECT COUNT(*) FROM [HTResourceMapper].[ResourceTag]
        WHERE TagDefinitionId = @Id AND UPPER(TagValue) = UPPER(@OldValue));

    IF @OldListed = 0 AND @OldUsed = 0
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    -- The target must already be a real choice: inventing it here would turn a delete into a silent
    -- create, and the caller picked from the existing list.
    IF NOT EXISTS (SELECT 1 FROM OPENJSON(@AllowedValues) WHERE UPPER([value]) = UPPER(@NewValue))
    BEGIN
        SET @Result = 'nonewvalue';
        RETURN;
    END

    -- Same (Type + Key) already present in the target domain => moving would duplicate an identity.
    SELECT @Conflicts = COUNT(*)
    FROM [HTResourceMapper].[Resource] r
    INNER JOIN [HTResourceMapper].[ResourceTag] rt
        ON rt.ResourceId = r.ResourceId AND rt.TagDefinitionId = @Id
       AND UPPER(rt.TagValue) = UPPER(@OldValue)
    WHERE EXISTS (
        SELECT 1
        FROM [HTResourceMapper].[Resource] t
        INNER JOIN [HTResourceMapper].[ResourceTag] tt
            ON tt.ResourceId = t.ResourceId AND tt.TagDefinitionId = @Id
           AND UPPER(tt.TagValue) = UPPER(@NewValue)
        WHERE t.ResourceTypeId = r.ResourceTypeId
          AND UPPER(t.ResourceKey) = UPPER(r.ResourceKey)
          AND t.ResourceId <> r.ResourceId);

    IF @Conflicts > 0
    BEGIN
        SET @Result = 'conflict';
        RETURN;
    END

    DECLARE @Updated NVARCHAR(2000);

    IF @OldListed = 1
    BEGIN
        -- Rebuild in the original order ([key] is the array index), minus the removed entry.
        SELECT @Updated = N'[' + STRING_AGG(N'"' + STRING_ESCAPE([value], 'json') + N'"', N',')
                                 WITHIN GROUP (ORDER BY CAST([key] AS INT)) + N']'
        FROM OPENJSON(@AllowedValues)
        WHERE UPPER([value]) <> UPPER(@OldValue);
    END
    ELSE
    BEGIN
        SET @Updated = @AllowedValues;   -- an unlisted value: only the resources need moving
    END

    IF @Updated IS NULL
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    BEGIN TRANSACTION;

        UPDATE [HTResourceMapper].[ResourceTag]
        SET TagValue  = @NewValue,
            UpdatedOn = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id
          AND UPPER(TagValue) = UPPER(@OldValue);

        SET @AffectedResources = @@ROWCOUNT;

        UPDATE [HTResourceMapper].[TagDefinition]
        SET AllowedValues = @Updated,
            UpdatedOn     = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id;

    COMMIT TRANSACTION;

    SET @Result = 'reassigned';
END
