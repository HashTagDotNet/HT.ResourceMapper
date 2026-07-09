/*
Post-deployment reference/system seed. Runs on every publish; must be idempotent (MERGE).
Introduced in slice #1 (Data Foundation).
*/

-- Content types: constrained set with fixed ids (matches TagContentType CHECK: Text, Link).
MERGE [HTResourceMapper].[TagContentType] AS target
USING (VALUES (1, 'Text'), (2, 'Link')) AS source ([TagContentTypeId], [TagCode])
    ON target.[TagContentTypeId] = source.[TagContentTypeId]
WHEN MATCHED THEN
    UPDATE SET target.[TagCode] = source.[TagCode]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([TagContentTypeId], [TagCode]) VALUES (source.[TagContentTypeId], source.[TagCode]);

-- Domain (boundary) tag. System-managed, required, single-valued, restricted vocabulary.
-- Displayed as "Subscription" in this deployment; values are the prod/non-prod tiers.
MERGE [HTResourceMapper].[TagDefinition] AS target
USING (SELECT
        [TagDefinitionUid] = 'b1d0c0de-0000-4000-8000-000000000001'
       ,[TagDefinitionKey] = 'Domain'
       ,[DisplayName]      = 'Subscription'
       ,[TagContentTypeId] = 1               -- Text
       ,[AllowCustomValue] = CAST(0 AS BIT)
       ,[IsMultiValued]    = CAST(0 AS BIT)
       ,[AllowedValues]    = '["prod","non-prod"]'
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
        ,target.[AllowedValues]    = source.[AllowedValues]
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
