/*
=============================================
Demo Data Purge Script for Resource Grid Solution
=============================================
This script removes all demo data created by Demo_Insert_ResourceItemsForGrid.sql
It safely removes data in the correct order to respect foreign key constraints

USAGE: Run this script in SQLCMD mode or execute directly against the database
=============================================
*/

-- Delete Resource Type Tags (Foreign Key Dependencies)
DELETE rtt 
FROM [HTResourceMapper].[ResourceTypeTag] rtt
INNER JOIN [HTResourceMapper].[ResourceType] rt ON rtt.ResourceTypeId = rt.ResourceTypeId
WHERE rt.ResourceTypeUid IN (
    'RT-DATABASE-001',
    'RT-WEBSERVICE-001', 
    'RT-GATEWAY-001',
    'RT-STORAGE-001',
    'RT-CACHE-001'
);

-- Delete Resource Tags (Foreign Key Dependencies)
DELETE rt 
FROM [HTResourceMapper].[ResourceTag] rt
INNER JOIN [HTResourceMapper].[Resource] r ON rt.ResourceId = r.ResourceId
WHERE r.ResourceUid IN (
    'RES-DB-PROD-001', 'RES-DB-PROD-002', 'RES-DB-PROD-003',
    'RES-DB-STAGE-001', 'RES-DB-TEST-001',
    'RES-WS-PROD-001', 'RES-WS-PROD-002', 'RES-WS-PROD-003', 'RES-WS-STAGE-001',
    'RES-GW-PROD-001', 'RES-GW-PROD-002',
    'RES-ST-PROD-001', 'RES-ST-PROD-002',
    'RES-CACHE-PROD-001', 'RES-CACHE-STAGE-001'
);

-- Delete Resources
DELETE FROM [HTResourceMapper].[Resource] 
WHERE ResourceUid IN (
    'RES-DB-PROD-001', 'RES-DB-PROD-002', 'RES-DB-PROD-003',
    'RES-DB-STAGE-001', 'RES-DB-TEST-001',
    'RES-WS-PROD-001', 'RES-WS-PROD-002', 'RES-WS-PROD-003', 'RES-WS-STAGE-001',
    'RES-GW-PROD-001', 'RES-GW-PROD-002',
    'RES-ST-PROD-001', 'RES-ST-PROD-002',
    'RES-CACHE-PROD-001', 'RES-CACHE-STAGE-001'
);

-- Delete Tag Definitions
DELETE FROM [HTResourceMapper].[TagDefinition]
WHERE TagDefinitionUid IN (
    'TD-ENV-001', 'TD-REGION-001', 'TD-OWNER-001', 'TD-COST-001',
    'TD-VERSION-001', 'TD-RUNTIME-001', 'TD-PROTOCOL-001',
    'TD-PORTAL-001', 'TD-DOCS-001', 'TD-MONITOR-001',
    'TD-PROJECT-001', 'TD-CRITICALITY-001'
);

-- Delete Resource Types
DELETE FROM [HTResourceMapper].[ResourceType] 
WHERE ResourceTypeUid IN (
    'RT-DATABASE-001', 'RT-WEBSERVICE-001', 'RT-GATEWAY-001',
    'RT-STORAGE-001', 'RT-CACHE-001'
);

-- Delete Tag Value Types (Optional - only delete if they're demo-specific)
DELETE FROM [HTResourceMapper].[TagValueType] 
WHERE TagValueTypeId IN (1, 2) 
AND Code IN ('Required', 'Optional');

-- Delete Tag Content Types (Optional - only delete if they're demo-specific)
DELETE FROM [HTResourceMapper].[TagContentType] 
WHERE TagContentTypeId IN (1, 2) 
AND TagCode IN ('Text', 'Link');GO