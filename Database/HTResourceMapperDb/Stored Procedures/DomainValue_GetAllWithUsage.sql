CREATE PROCEDURE [HTResourceMapper].DomainValue_GetAllWithUsage
AS
BEGIN
    SET NOCOUNT ON;

    -- Every domain value (a Subscription in this deployment) with the number of resources using it,
    -- backing the management screen's list and its delete guard.
    --
    -- A FULL OUTER JOIN rather than a walk of AllowedValues, because the two sides can disagree and
    -- the screen has to tell the truth about both:
    --   * listed and used      -> a normal row with a count
    --   * listed and unused    -> count 0, so it is safe to delete
    --   * USED BUT NOT LISTED  -> IsUnlisted = 1. Import writes a resource's domain as a tag value
    --     without touching the vocabulary, so a value can be live on resources yet absent from the
    --     picker. Hiding those would make the count column lie and leave the value uneditable.

    DECLARE @Id INT, @AllowedValues NVARCHAR(2000);

    SELECT @Id = TagDefinitionId, @AllowedValues = AllowedValues
    FROM [HTResourceMapper].[TagDefinition]
    WHERE IsDomainTag = 1;

    IF @Id IS NULL
    BEGIN
        -- No domain tag designated: return the empty shape rather than an error, so the screen can
        -- render its own "nothing configured" state.
        SELECT CAST(NULL AS NVARCHAR(200)) AS DomainValue, CAST(0 AS INT) AS ResourceCount,
               CAST(0 AS BIT) AS IsUnlisted
        WHERE 1 = 0;
        RETURN;
    END

    ;WITH listed AS (
        SELECT [value] AS DomainValue
        FROM OPENJSON(NULLIF(ISNULL(@AllowedValues, N'[]'), N''))
    ),
    used AS (
        SELECT rt.TagValue AS DomainValue, COUNT(*) AS ResourceCount
        FROM [HTResourceMapper].[ResourceTag] rt
        WHERE rt.TagDefinitionId = @Id
          AND rt.TagValue IS NOT NULL
          AND rt.TagValue <> N''
        GROUP BY rt.TagValue
    )
    SELECT
        COALESCE(l.DomainValue, u.DomainValue) AS DomainValue,
        ISNULL(u.ResourceCount, 0)             AS ResourceCount,
        CASE WHEN l.DomainValue IS NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsUnlisted
    FROM listed l
    FULL OUTER JOIN used u ON UPPER(u.DomainValue) = UPPER(l.DomainValue)
    ORDER BY COALESCE(l.DomainValue, u.DomainValue);
END
