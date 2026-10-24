/*
Post-deployment reference/system seed. Runs on every publish; must be idempotent (MERGE).
Introduced in slice #1 (Data Foundation).
*/

-- Content types: constrained set with fixed ids (matches TagContentType CHECK: Text, Link).
MERGE [ResourceExplorer].[TagContentType] AS target
USING (VALUES (1, 'Text'), (2, 'Link')) AS source ([TagContentTypeId], [TagCode])
    ON target.[TagContentTypeId] = source.[TagContentTypeId]
WHEN MATCHED THEN
    UPDATE SET target.[TagCode] = source.[TagCode]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TagContentTypeId], [TagCode]) VALUES (source.[TagContentTypeId], source.[TagCode]);

-- Domain (boundary) tag. System-managed, required, single-valued, restricted vocabulary.
-- Displayed as "Tier" in this deployment. 'shared' is carried by logical resources
-- (System, Application) that exist in both tiers and are deployed to neither.
MERGE [ResourceExplorer].[TagDefinition] AS target
USING (SELECT
        [TagDefinitionUid] = 'b1d0c0de-0000-4000-8000-000000000001'
       ,[TagDefinitionKey] = 'Domain'
       ,[DisplayName]      = 'Tier'
       ,[TagContentTypeId] = 1               -- Text
       ,[AllowCustomValue] = CAST(0 AS BIT)
       ,[IsMultiValued]    = CAST(0 AS BIT)
       ,[AllowedValues]    = '["prod","non-prod","shared"]'
       ,[RequirementLevel] = 'Error'
       ,[IsDomainTag]      = CAST(1 AS BIT)
       ,[IsSystemTag]      = CAST(1 AS BIT)
       ,[DisplayOrder]     = 10
      ) AS source
    ON target.[TagDefinitionKey] = source.[TagDefinitionKey]
WHEN MATCHED THEN
    UPDATE SET
         target.[DisplayName]      = source.[DisplayName]
        ,target.[TagContentTypeId] = source.[TagContentTypeId]
        ,target.[AllowCustomValue] = source.[AllowCustomValue]
        ,target.[IsMultiValued]    = source.[IsMultiValued]
        -- AllowedValues is seeded only on insert: DomainValue_Add owns the list afterwards.
        ,target.[RequirementLevel] = source.[RequirementLevel]
        ,target.[IsDomainTag]      = source.[IsDomainTag]
        ,target.[IsSystemTag]      = source.[IsSystemTag]
        ,target.[DisplayOrder]     = source.[DisplayOrder]
        ,target.[UpdatedOn]        = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TagDefinitionUid], [TagDefinitionKey], [DisplayName], [TagContentTypeId]
           ,[AllowCustomValue], [IsMultiValued], [AllowedValues], [RequirementLevel]
           ,[IsDomainTag], [IsSystemTag], [DisplayOrder])
    VALUES (source.[TagDefinitionUid], source.[TagDefinitionKey], source.[DisplayName], source.[TagContentTypeId]
           ,source.[AllowCustomValue], source.[IsMultiValued], source.[AllowedValues], source.[RequirementLevel]
           ,source.[IsDomainTag], source.[IsSystemTag], source.[DisplayOrder]);


-- Azure resource id tag. System-managed, multi-valued Text, free-form values (a resource can carry
-- several ARM ids). Old vs new is derived per value from configured legacy markers, not stored here.
MERGE [ResourceExplorer].[TagDefinition] AS target
USING (SELECT
        [TagDefinitionUid] = 'b1d0c0de-0000-4000-8000-000000000002'
       ,[TagDefinitionKey] = 'AzureResourceId'
       ,[DisplayName]      = 'Azure Resource Id'
       ,[TagContentTypeId] = 1               -- Text
       ,[AllowCustomValue] = CAST(1 AS BIT)
       ,[IsMultiValued]    = CAST(1 AS BIT)
       ,[RequirementLevel] = 'Optional'
       ,[IsDomainTag]      = CAST(0 AS BIT)
       ,[IsSystemTag]      = CAST(0 AS BIT)   -- values are user-entered
       ,[DisplayOrder]     = 15
      ) AS source
    ON target.[TagDefinitionKey] = source.[TagDefinitionKey]
WHEN MATCHED THEN
    UPDATE SET
         target.[DisplayName]      = source.[DisplayName]
        ,target.[TagContentTypeId] = source.[TagContentTypeId]
        ,target.[AllowCustomValue] = source.[AllowCustomValue]
        ,target.[IsMultiValued]    = source.[IsMultiValued]
        ,target.[IsDomainTag]      = source.[IsDomainTag]
        ,target.[IsSystemTag]      = source.[IsSystemTag]
        ,target.[UpdatedOn]        = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TagDefinitionUid], [TagDefinitionKey], [DisplayName], [TagContentTypeId]
           ,[AllowCustomValue], [IsMultiValued], [AllowedValues], [RequirementLevel]
           ,[IsDomainTag], [IsSystemTag], [DisplayOrder])
    VALUES (source.[TagDefinitionUid], source.[TagDefinitionKey], source.[DisplayName], source.[TagContentTypeId]
           ,source.[AllowCustomValue], source.[IsMultiValued], NULL, source.[RequirementLevel]
           ,source.[IsDomainTag], source.[IsSystemTag], source.[DisplayOrder]);

-- Entry-point template: AzureResourceId on every Azure resource type that exists. Insert-only, so a
-- template an admin has since edited is left alone; types absent from this database are skipped.
INSERT INTO [ResourceExplorer].[ResourceTypeTag] ([ResourceTypeId], [TagDefinitionId], [IsDefaultPrimary], [RequirementLevel])
SELECT rt.[ResourceTypeId], td.[TagDefinitionId], 0, NULL
FROM [ResourceExplorer].[ResourceType] rt
INNER JOIN [ResourceExplorer].[TagDefinition] td ON td.[TagDefinitionKey] = 'AzureResourceId'
WHERE rt.[TypeName] IN ('App Configuration', 'App Insights', 'App Service', 'Redis', 'Managed Redis',
                        'Service Bus', 'Service Fabric', 'Storage Account', 'Queue')
  AND NOT EXISTS (SELECT 1 FROM [ResourceExplorer].[ResourceTypeTag] x
                  WHERE x.[ResourceTypeId] = rt.[ResourceTypeId] AND x.[TagDefinitionId] = td.[TagDefinitionId]);
