CREATE PROCEDURE [ResourceExplorer].ResourceType_GetAll 
AS
BEGIN
	SELECT 
		*
	FROM
		ResourceExplorer.ResourceType
	ORDER BY 
		TypeName ASC
END
