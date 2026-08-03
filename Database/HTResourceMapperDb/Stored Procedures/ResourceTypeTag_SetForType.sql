-- Replaces a resource type's entry-point template with exactly the supplied set.
--
-- "Replace", not "merge": the screen edits the whole list, so an absent row means removed. An empty
-- TVP therefore clears the template, which is a legitimate edit (a type that prompts for nothing).
--
-- Set-based, and it does NOT delete-then-reinsert unchanged rows: ResourceTypeTagId is referenced by
-- nothing today, but churning identities on every save would make any future audit or FK pointless.
-- MERGE keeps untouched rows untouched.
CREATE PROCEDURE [HTResourceMapper].ResourceTypeTag_SetForType
    @ResourceTypeId INT,
    @Tags [HTResourceMapper].[ResourceTypeTagList] READONLY,
    @Result VARCHAR(10) OUTPUT   -- 'ok' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM [HTResourceMapper].[ResourceType] WHERE ResourceTypeId = @ResourceTypeId)
    BEGIN
        SET @Result = 'error';
        RETURN;
    END

    -- UX_ResourceTypeTag_DefaultPrimary allows at most one default-primary row per type. Rather than
    -- letting a caller trip that index, keep the lowest-id flagged row and clear the rest: the UI
    -- offers a radio group, so more than one arriving here is a caller bug, not a user choice.
    DECLARE @Normalized TABLE
    (
        TagDefinitionId  INT NOT NULL PRIMARY KEY,
        IsDefaultPrimary BIT NOT NULL,
        RequirementLevel NVARCHAR(20) NULL
    );

    DECLARE @PrimaryTagId INT =
        (SELECT MIN(TagDefinitionId) FROM @Tags WHERE IsDefaultPrimary = 1);

    INSERT INTO @Normalized (TagDefinitionId, IsDefaultPrimary, RequirementLevel)
    SELECT
         t.TagDefinitionId
        ,CASE WHEN t.TagDefinitionId = @PrimaryTagId THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END
        ,t.RequirementLevel
    FROM (
        -- A duplicated TagDefinitionId would violate UK_ResourceTypeTag_Type_Tag; collapse first.
        SELECT
             TagDefinitionId
            ,IsDefaultPrimary = MAX(CAST(IsDefaultPrimary AS TINYINT))
            ,RequirementLevel = MIN(RequirementLevel)
        FROM @Tags
        GROUP BY TagDefinitionId
    ) t;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Clear the flag across the type FIRST. Without this, promoting tag B while tag A still
        -- holds the flag violates the filtered unique index mid-MERGE.
        UPDATE [HTResourceMapper].[ResourceTypeTag]
        SET IsDefaultPrimary = 0
        WHERE ResourceTypeId = @ResourceTypeId
          AND IsDefaultPrimary = 1;

        MERGE [HTResourceMapper].[ResourceTypeTag] AS target
        USING (SELECT TagDefinitionId, IsDefaultPrimary, RequirementLevel FROM @Normalized) AS source
            ON target.ResourceTypeId = @ResourceTypeId
           AND target.TagDefinitionId = source.TagDefinitionId
        WHEN MATCHED THEN
            UPDATE SET
                 target.IsDefaultPrimary = source.IsDefaultPrimary
                ,target.RequirementLevel = source.RequirementLevel
        WHEN NOT MATCHED BY TARGET THEN
            INSERT (ResourceTypeId, TagDefinitionId, IsDefaultPrimary, RequirementLevel)
            VALUES (@ResourceTypeId, source.TagDefinitionId, source.IsDefaultPrimary, source.RequirementLevel)
        -- Scoped to this type: without the predicate, MERGE would delete every other type's rows.
        WHEN NOT MATCHED BY SOURCE AND target.ResourceTypeId = @ResourceTypeId THEN
            DELETE;

        COMMIT TRANSACTION;
        SET @Result = 'ok';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
        SET @Result = 'error';
    END CATCH
END
