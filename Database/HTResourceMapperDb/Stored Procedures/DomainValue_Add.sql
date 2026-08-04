CREATE PROCEDURE [HTResourceMapper].DomainValue_Add
    @Value   NVARCHAR(200),
    @Result  VARCHAR(10) OUTPUT -- 'added' | 'exists' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    -- Appends one choice to the DOMAIN tag's vocabulary (a Subscription, in this deployment) and
    -- nothing else.
    --
    -- Why this exists rather than a call to TagDefinition_Upsert: that procedure deliberately
    -- refuses to write a system-managed tag, because it rewrites the whole row and an import naming
    -- 'Domain' had been flipping the restricted ["prod","non-prod"] list into free text
    -- (AllowCustomValue 0 -> 1, AllowedValues -> NULL). Since resource identity is
    -- (Domain + Type + Key), that let a typo mint a new identity namespace. The guard is right, and
    -- this procedure does not relax it: the UPDATE below touches AllowedValues only, so
    -- ContentType, AllowCustomValue, IsMultiValued, IsDomainTag and IsSystemTag cannot be altered
    -- by this path even in principle. Adding a subscription is the one edit to that vocabulary that
    -- is safe to expose, so it gets its own narrow door instead of a wider one.
    --
    -- Idempotent: an existing value (compared case-insensitively, matching the service layer)
    -- reports 'exists' rather than appending a near-duplicate that differs only by case.

    DECLARE @Id INT, @AllowedValues NVARCHAR(2000);

    SELECT @Id = TagDefinitionId, @AllowedValues = AllowedValues
    FROM [HTResourceMapper].[TagDefinition]
    WHERE IsDomainTag = 1;

    IF @Id IS NULL
    BEGIN
        SET @Result = 'error';   -- no domain tag designated; nothing to extend
        RETURN;
    END

    SET @Value = LTRIM(RTRIM(@Value));

    IF @Value = N''
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    -- The vocabulary is stored as a JSON array string, so OPENJSON is the reader that matches how
    -- every other consumer parses it.
    IF @AllowedValues IS NOT NULL
       AND EXISTS (SELECT 1 FROM OPENJSON(@AllowedValues) WHERE UPPER([value]) = UPPER(@Value))
    BEGIN
        SET @Result = 'exists';
        RETURN;
    END

    DECLARE @Updated NVARCHAR(2000) =
        CASE
            WHEN @AllowedValues IS NULL OR @AllowedValues = N'' THEN N'["' + STRING_ESCAPE(@Value, 'json') + N'"]'
            -- Splice before the closing bracket: rebuilding via FOR JSON would reorder nothing but
            -- would also rewrite the existing entries, and this list is read by name elsewhere.
            ELSE STUFF(@AllowedValues, LEN(@AllowedValues), 1, N',"' + STRING_ESCAPE(@Value, 'json') + N'"]')
        END;

    IF LEN(@Updated) > 2000
    BEGIN
        SET @Result = 'error';   -- would exceed TagDefinition.AllowedValues
        RETURN;
    END

    UPDATE [HTResourceMapper].[TagDefinition]
    SET AllowedValues = @Updated,
        UpdatedOn     = SYSUTCDATETIME()
    WHERE TagDefinitionId = @Id;

    SET @Result = 'added';
END
