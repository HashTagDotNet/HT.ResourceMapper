CREATE SCHEMA [HTResourceMapper] AUTHORIZATION [dbo];
--go
---- Grant permissions to htuser on the HTResourceMapper schema
--GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[HTResourceMapper] TO [htuser];
--GRANT EXECUTE ON SCHEMA::[HTResourceMapper] TO [htuser];

---- Additional permissions for object creation within the schema
--GRANT CREATE PROCEDURE ON DATABASE::[HT.Services] TO [htuser];
--GRANT CREATE FUNCTION ON DATABASE::[HT.Services] TO [htuser];
--GRANT CREATE TABLE ON DATABASE::[HT.Services] TO [htuser];
--GRANT CREATE VIEW ON DATABASE::[HT.Services] TO [htuser];

---- Verify schema creation and ownership
--SELECT 
--    'HTResourceMapper Schema Info' AS InfoType,
--    s.name AS SchemaName,
--    u.name AS SchemaOwner
--FROM sys.schemas s
--JOIN sys.database_principals u ON s.principal_id = u.principal_id
--WHERE s.name = 'HTResourceMapper';
