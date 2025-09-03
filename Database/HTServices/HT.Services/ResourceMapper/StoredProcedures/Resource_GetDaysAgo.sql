-- Please edit the Procedure name accordingly.
CREATE PROCEDURE [HT.ResourceMapper].Resource_GetDaysAgo (
  @DaysAgo INT
) AS
BEGIN
	SET NOCOUNT ON;
	SELECT
		DATEADD(DAY, -@DaysAgo, CAST(SYSUTCDATETIME() AS DATETIME2(7))) AS DaysAgo
END
