CREATE PROCEDURE [HTResourceMapper].DomainValue_Rename
    @OldValue           NVARCHAR(200),
    @NewValue           NVARCHAR(200),
    @AffectedResources  INT = NULL OUTPUT,   -- resources whose stored value was rewritten
    @Result             VARCHAR(10) OUTPUT   -- 'renamed' | 'exists' | 'notfound' | 'error'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Renames a domain value (a Subscription here) everywhere it appears: the vocabulary AND every
    -- resource carrying it, in one transaction.
    --
    -- The cascade is the whole point. A resource's domain is stored as TEXT on ResourceTag, not as a
    -- foreign key, so renaming the vocabulary entry alone would strand every resource on the old
    -- spelling — still displayed, no longer offered by the picker, and silently outside the renamed
    -- value. Callers are expected to warn first: resource identity is (Domain + Type + Key), so this
    -- changes the identity of every affected resource.
    --
    -- Like DomainValue_Add, this deliberately does NOT go through TagDefinition_Upsert: that path
    -- refuses system-managed tags because it rewrites the whole row. Here only AllowedValues and
    -- ResourceTag.TagValue are touched, so the tag's shape and flags are out of reach.

    SET @AffectedResources = 0;

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

    SET @AllowedValues = NULLIF(ISNULL(@AllowedValues, N'[]'), N'');

    DECLARE @IsListed BIT = CASE WHEN EXISTS (
        SELECT 1 FROM OPENJSON(@AllowedValues) WHERE UPPER([value]) = UPPER(@OldValue)) THEN 1 ELSE 0 END;

    DECLARE @UsedCount INT = (
        SELECT COUNT(*) FROM [HTResourceMapper].[ResourceTag]
        WHERE TagDefinitionId = @Id AND UPPER(TagValue) = UPPER(@OldValue));

    -- Neither in the vocabulary nor on any resource: there is nothing to rename.
    IF @IsListed = 0 AND @UsedCount = 0
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    -- Target name already taken by a DIFFERENT entry. A pure case change (prod -> PROD) is allowed
    -- through, since that is a legitimate rename of the same value.
    IF UPPER(@OldValue) <> UPPER(@NewValue)
       AND EXISTS (SELECT 1 FROM OPENJSON(@AllowedValues) WHERE UPPER([value]) = UPPER(@NewValue))
    BEGIN
        SET @Result = 'exists';
        RETURN;
    END

    DECLARE @Updated NVARCHAR(2000);

    IF @IsListed = 1
    BEGIN
        -- Rebuild the array in its original order ([key] is the element index for a JSON array), so
        -- the picker's ordering is not silently reshuffled by an edit.
        SELECT @Updated = N'[' + STRING_AGG(N'"' + STRING_ESCAPE(v, 'json') + N'"', N',')
                                 WITHIN GROUP (ORDER BY k) + N']'
        FROM (
            SELECT CAST([key] AS INT) AS k,
                   CASE WHEN UPPER([value]) = UPPER(@OldValue) THEN @NewValue ELSE [value] END AS v
            FROM OPENJSON(@AllowedValues)
        ) x;
    END
    ELSE
    BEGIN
        -- Used but unlisted (an import artefact): renaming it is also the moment to bring it into the
        -- vocabulary, otherwise the value stays invisible to the picker after being fixed.
        SELECT @Updated = CASE
            WHEN @AllowedValues IS NULL OR @AllowedValues = N'[]'
                THEN N'["' + STRING_ESCAPE(@NewValue, 'json') + N'"]'
            ELSE STUFF(@AllowedValues, LEN(@AllowedValues), 1,
                       N',"' + STRING_ESCAPE(@NewValue, 'json') + N'"]')
        END;
    END

    IF @Updated IS NULL OR LEN(@Updated) > 2000
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    BEGIN TRANSACTION;

        UPDATE [HTResourceMapper].[TagDefinition]
        SET AllowedValues = @Updated,
            UpdatedOn     = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id;

        UPDATE [HTResourceMapper].[ResourceTag]
        SET TagValue  = @NewValue,
            UpdatedOn = SYSUTCDATETIME()
        WHERE TagDefinitionId = @Id
          AND UPPER(TagValue) = UPPER(@OldValue);

        SET @AffectedResources = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SET @Result = 'renamed';
END
