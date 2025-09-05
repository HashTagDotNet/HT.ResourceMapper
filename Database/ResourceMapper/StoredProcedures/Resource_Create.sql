CREATE PROCEDURE [HTResourceMapper].[Resource_Create]
    @ResourceUid VARCHAR(40),
    @ResourceKey NVARCHAR(250),
    @ResourceTypeCode NVARCHAR(250),
    @ResourceName NVARCHAR(250),

    @Description NVARCHAR(2000),
    
    @ResourceId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @ResourceTypeId INT;
    
    -- Lookup ResourceTypeId by ResourceTypeCode
    SELECT @ResourceTypeId = ResourceTypeId
    FROM [HTResourceMapper].[ResourceType] WITH(NOLOCK)
    WHERE TypeName = @ResourceTypeCode;
    
    -- Insert the new Resource
    INSERT INTO [HTResourceMapper].[Resource]
    (
        ResourceUid,
        ResourceKey,
        ResourceTypeId,
        ResourceName,

        [Description]
    )
    VALUES
    (
        @ResourceUid,
        @ResourceKey,
        @ResourceTypeId,
        @ResourceName,
        @Description
    );
    
    -- Return the new ResourceId in output parameter
    SET @ResourceId = SCOPE_IDENTITY();
END