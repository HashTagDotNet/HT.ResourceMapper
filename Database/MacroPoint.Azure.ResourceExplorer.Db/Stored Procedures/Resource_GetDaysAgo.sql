-- Create stored procedure in ResourceExplorer schema
CREATE PROCEDURE [ResourceExplorer].Resource_GetDaysAgo (
  @DaysAgo INT
) AS
BEGIN
	SET NOCOUNT ON;
	SELECT
		DATEADD(DAY, @DaysAgo, CAST(SYSUTCDATETIME() AS DATETIME2(7))) AS DaysAgo
END
