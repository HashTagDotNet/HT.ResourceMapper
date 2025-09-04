-- Create stored procedure in HTResourceMapper schema
CREATE PROCEDURE [HTResourceMapper].Resource_GetDaysAgo (
  @DaysAgo INT
) AS
BEGIN
	SET NOCOUNT ON;
	SELECT
		DATEADD(DAY, @DaysAgo, CAST(SYSUTCDATETIME() AS DATETIME2(7))) AS DaysAgo
END
