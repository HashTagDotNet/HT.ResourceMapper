CREATE PROCEDURE [HTResourceMapper].DomainValue_Delete
    @Value         NVARCHAR(200),
    @ResourceCount INT = NULL OUTPUT,      -- resources still using it, when refused as 'inuse'
    @Result        VARCHAR(10) OUTPUT      -- 'deleted' | 'inuse' | 'last' | 'notfound' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    -- Removes a value from the domain vocabulary. Two refusals, both deliberate:
    --
    --   'inuse' - resources still carry it. Deleting the entry would not delete their stored text, so
    --             they would keep a domain the picker no longer offers: invisible in filters, wrong in
    --             the identity triple, and impossible to fix from the picker. Same rule Resource Types
    --             already apply to delete, for the same reason.
    --   'last'  - it is the only value left. Subscription is required to save a resource, so emptying
    --             the vocabulary would make the catalog unable to accept a new resource at all.
    --
    -- Only AllowedValues is touched, so this cannot alter the system-managed tag's shape (see
    -- DomainValue_Add for why that matters).

    SET @ResourceCount = 0;

    DECLARE @Id INT, @AllowedValues NVARCHAR(2000);

    SELECT @Id = TagDefinitionId, @AllowedValues = AllowedValues
    FROM [HTResourceMapper].[TagDefinition]
    WHERE IsDomainTag = 1;

    IF @Id IS NULL
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    SET @Value = LTRIM(RTRIM(@Value));

    IF @Value = N''
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    SET @AllowedValues = NULLIF(ISNULL(@AllowedValues, N'[]'), N'');

    SELECT @ResourceCount = COUNT(*)
    FROM [HTResourceMapper].[ResourceTag]
    WHERE TagDefinitionId = @Id AND UPPER(TagValue) = UPPER(@Value);

    IF @ResourceCount > 0
    BEGIN
        SET @Result = 'inuse';
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM OPENJSON(@AllowedValues) WHERE UPPER([value]) = UPPER(@Value))
    BEGIN
        SET @Result = 'notfound';
        RETURN;
    END

    IF (SELECT COUNT(*) FROM OPENJSON(@AllowedValues)) <= 1
    BEGIN
        SET @Result = 'last';
        RETURN;
    END

    DECLARE @Updated NVARCHAR(2000);

    -- Rebuilt in the original order ([key] is the array index), minus the removed entry.
    SELECT @Updated = N'[' + STRING_AGG(N'"' + STRING_ESCAPE([value], 'json') + N'"', N',')
                             WITHIN GROUP (ORDER BY CAST([key] AS INT)) + N']'
    FROM OPENJSON(@AllowedValues)
    WHERE UPPER([value]) <> UPPER(@Value);

    IF @Updated IS NULL
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    UPDATE [HTResourceMapper].[TagDefinition]
    SET AllowedValues = @Updated,
        UpdatedOn     = SYSUTCDATETIME()
    WHERE TagDefinitionId = @Id;

    SET @Result = 'deleted';
END
