CREATE PROCEDURE [HTResourceMapper].TagDefinition_Delete
    @TagDefinitionId   INT,
    @ResourceCount     INT = NULL OUTPUT,   -- resources carrying a value for it
    @TypeTemplateCount INT = NULL OUTPUT,   -- resource types whose template lists it
    @PrimaryForCount   INT = NULL OUTPUT,   -- resources whose primary link IS this tag
    @Result            VARCHAR(10) OUTPUT   -- 'deleted' | 'system' | 'inuse' | 'notfound' | 'error'
AS
BEGIN
    SET NOCOUNT ON;

    -- Deletes a tag definition, but only when nothing points at it. Every refusal is reported with the
    -- counts, because "cannot delete" without saying what is holding it is not actionable:
    --
    --   'system' - a system-managed tag (the domain/Subscription tag is one). Its shape and existence
    --              are owned by deployment, the same reason TagDefinition_Upsert refuses to write one.
    --   'inuse'  - at least one of the three references exists. All three are reported, not just the
    --              first, so one round trip tells the user everything they have to clear. The primary
    --              one matters most: Resource.PrimaryTagDefinitionId is a NO ACTION FK, so deleting
    --              anyway would surface as a raw constraint violation instead of a sentence.
    --
    -- Deliberately NOT cascading. Removing the tag from every resource and template on the user's
    -- behalf would silently destroy catalogued metadata; clearing it is a decision they make per
    -- resource, on screens built for it.

    SET @ResourceCount = 0;
    SET @TypeTemplateCount = 0;
    SET @PrimaryForCount = 0;

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

    SELECT @ResourceCount = COUNT(*) FROM [HTResourceMapper].[ResourceTag]
    WHERE TagDefinitionId = @TagDefinitionId;

    SELECT @TypeTemplateCount = COUNT(*) FROM [HTResourceMapper].[ResourceTypeTag]
    WHERE TagDefinitionId = @TagDefinitionId;

    SELECT @PrimaryForCount = COUNT(*) FROM [HTResourceMapper].[Resource]
    WHERE PrimaryTagDefinitionId = @TagDefinitionId;

    IF @ResourceCount > 0 OR @TypeTemplateCount > 0 OR @PrimaryForCount > 0
    BEGIN
        SET @Result = 'inuse';
        RETURN;
    END

    DELETE FROM [HTResourceMapper].[TagDefinition] WHERE TagDefinitionId = @TagDefinitionId;

    SET @Result = 'deleted';
END
