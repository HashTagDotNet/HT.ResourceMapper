SET QUOTED_IDENTIFIER ON;

-- E2E fixture (idempotent — safe to run repeatedly): a dedicated resource type + two tag
-- definitions + entry-point templates + a read-only picker-candidate baseline (2 non-prod +
-- 1 prod). References the SHARED domain TagDefinition (IsDomainTag=1, seeded permanently by
-- Script.PostDeployment1.sql) by lookup only — never creates or deletes it (RD15).

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.ResourceType WHERE TypeName = 'E2eDepType')
    INSERT INTO ResourceExplorer.ResourceType (ResourceTypeUid, TypeName, AllowCustomTags)
    VALUES ('e2e-dep-type-uid', 'E2eDepType', 1);

DECLARE @TypeId INT = (SELECT ResourceTypeId FROM ResourceExplorer.ResourceType WHERE TypeName = 'E2eDepType');
DECLARE @DomainTagDefId INT = (SELECT TagDefinitionId FROM ResourceExplorer.TagDefinition WHERE IsDomainTag = 1);

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.TagDefinition WHERE TagDefinitionKey = 'E2eOverview')
    INSERT INTO ResourceExplorer.TagDefinition
        (TagDefinitionUid, TagDefinitionKey, DisplayName, TagContentTypeId, AllowCustomValue, IsMultiValued, AllowedValues, RequirementLevel, IsDomainTag, IsSystemTag, DisplayOrder)
    VALUES
        ('e2e-tag-overview-uid', 'E2eOverview', 'Overview', 2, 1, 0, NULL, 'Suggested', 0, 0, 30);

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.TagDefinition WHERE TagDefinitionKey = 'E2eEnvironment')
    INSERT INTO ResourceExplorer.TagDefinition
        (TagDefinitionUid, TagDefinitionKey, DisplayName, TagContentTypeId, AllowCustomValue, IsMultiValued, AllowedValues, RequirementLevel, IsDomainTag, IsSystemTag, DisplayOrder)
    VALUES
        ('e2e-tag-environment-uid', 'E2eEnvironment', 'Environment', 1, 0, 1, '["prod","preprod","dev","test"]', 'Suggested', 0, 0, 20);

DECLARE @OverviewId INT = (SELECT TagDefinitionId FROM ResourceExplorer.TagDefinition WHERE TagDefinitionKey = 'E2eOverview');
DECLARE @EnvId INT = (SELECT TagDefinitionId FROM ResourceExplorer.TagDefinition WHERE TagDefinitionKey = 'E2eEnvironment');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.ResourceTypeTag WHERE ResourceTypeId = @TypeId AND TagDefinitionId = @OverviewId)
    INSERT INTO ResourceExplorer.ResourceTypeTag (ResourceTypeId, TagDefinitionId, IsDefaultPrimary, RequirementLevel)
    VALUES (@TypeId, @OverviewId, 1, 'Suggested');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.ResourceTypeTag WHERE ResourceTypeId = @TypeId AND TagDefinitionId = @EnvId)
    INSERT INTO ResourceExplorer.ResourceTypeTag (ResourceTypeId, TagDefinitionId, IsDefaultPrimary, RequirementLevel)
    VALUES (@TypeId, @EnvId, 0, 'Suggested');

-- Picker candidates: a read-only baseline seeded once (mutating specs create their own subject
-- resource via the UI's Create flow instead of touching these).
IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.Resource WHERE ResourceUid = 'e2e-candidate-1')
    INSERT INTO ResourceExplorer.Resource (ResourceUid, ResourceKey, ResourceTypeId, ResourceName)
    VALUES ('e2e-candidate-1', 'e2e-candidate-1', @TypeId, 'E2E Candidate One');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.Resource WHERE ResourceUid = 'e2e-candidate-2')
    INSERT INTO ResourceExplorer.Resource (ResourceUid, ResourceKey, ResourceTypeId, ResourceName)
    VALUES ('e2e-candidate-2', 'e2e-candidate-2', @TypeId, 'E2E Candidate Two');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.Resource WHERE ResourceUid = 'e2e-candidate-prod')
    INSERT INTO ResourceExplorer.Resource (ResourceUid, ResourceKey, ResourceTypeId, ResourceName)
    VALUES ('e2e-candidate-prod', 'e2e-candidate-prod', @TypeId, 'E2E Candidate Prod');

DECLARE @Cand1Id INT = (SELECT ResourceId FROM ResourceExplorer.Resource WHERE ResourceUid = 'e2e-candidate-1');
DECLARE @Cand2Id INT = (SELECT ResourceId FROM ResourceExplorer.Resource WHERE ResourceUid = 'e2e-candidate-2');
DECLARE @CandProdId INT = (SELECT ResourceId FROM ResourceExplorer.Resource WHERE ResourceUid = 'e2e-candidate-prod');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.ResourceTag WHERE ResourceId = @Cand1Id AND TagDefinitionId = @DomainTagDefId)
    INSERT INTO ResourceExplorer.ResourceTag (ResourceId, TagDefinitionId, TagValue) VALUES (@Cand1Id, @DomainTagDefId, 'non-prod');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.ResourceTag WHERE ResourceId = @Cand2Id AND TagDefinitionId = @DomainTagDefId)
    INSERT INTO ResourceExplorer.ResourceTag (ResourceId, TagDefinitionId, TagValue) VALUES (@Cand2Id, @DomainTagDefId, 'non-prod');

IF NOT EXISTS (SELECT 1 FROM ResourceExplorer.ResourceTag WHERE ResourceId = @CandProdId AND TagDefinitionId = @DomainTagDefId)
    INSERT INTO ResourceExplorer.ResourceTag (ResourceId, TagDefinitionId, TagValue) VALUES (@CandProdId, @DomainTagDefId, 'prod');
