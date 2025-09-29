CREATE PROCEDURE [HTResourceMapper].ResourceType_GetAll 
AS
BEGIN
	SELECT 
		*
	FROM
		HTResourceMapper.ResourceType
	ORDER BY 
		TypeName ASC
END
