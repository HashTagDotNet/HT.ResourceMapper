CREATE SCHEMA [ResourceExplorer] AUTHORIZATION [dbo];
--go
---- Grant permissions to htuser on the ResourceExplorer schema
--GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[ResourceExplorer] TO [htuser];
--GRANT EXECUTE ON SCHEMA::[ResourceExplorer] TO [htuser];

---- Verify schema creation and ownership
--SELECT 
--    'ResourceExplorer Schema Info' AS InfoType,
--    s.name AS SchemaName,
--    u.name AS SchemaOwner
--FROM sys.schemas s
--JOIN sys.database_principals u ON s.principal_id = u.principal_id
--WHERE s.name = 'ResourceExplorer';
