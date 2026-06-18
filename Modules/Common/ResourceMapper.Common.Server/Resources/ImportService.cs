using System.Text.Json;
using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Import.Contracts;

namespace ResourceMapper.Common.Server.Resources
{
    /// <summary>
    /// Validates an import document, then writes resource types, tag definitions, and resources
    /// (with their tags) into the database. Validation runs fully before any write; on a validation
    /// error nothing is written and a 400 with the error list is returned. There is no cross-call
    /// transaction (write volume is low; per-resource tag replacement is atomic in its sproc).
    /// Dependencies are intentionally not handled this phase (relationships are a later phase).
    /// </summary>
    public class ImportService : IImportService
    {
        private static readonly string[] AllowedPolicies = { "upsert", "skip", "fail" };

        private readonly IResourceRepository _resourceRepo;
        private readonly IImportRepository _importRepo;

        public ImportService(IResourceRepository resourceRepo, IImportRepository importRepo)
        {
            _resourceRepo = resourceRepo;
            _importRepo = importRepo;
        }

        public async Task<ApiServiceResponse<ImportResponse>> ImportAsync(
            ImportRequest request, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ImportResponse>();
            try
            {
                if (request == null)
                {
                    builder.Validation.AddValidation("request", "Is required");
                    return builder.BuildResponse();
                }

                var errors = new List<ImportError>();

                // 1. Schema validation (no DB calls). Collect ALL errors before returning.
                ValidateSchema(request, errors);
                if (errors.Count > 0) return Fail(builder, errors);

                var onConflict = request.Policy.OnConflict.ToLowerInvariant();

                // 2. Load DB reference data (read-only). Reuse the existing resource-type lookup.
                var existingTypeNames = (await _resourceRepo.GetAllResourceTypesAsync(cancellationToken))
                    .Select(t => t.TypeName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var existingTagDefs = (await _importRepo.GetAllTagDefinitionsAsync(cancellationToken))
                    .GroupBy(d => d.TagDefinitionKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var existingResourceKeys = await _importRepo.GetExistingResourceKeysAsync(cancellationToken);

                // Import-declared sections, case-insensitive (schema validation already ruled out case-only dups).
                var importTypeNames = ToKeySet(request.ResourceTypes);
                var importTagDefs = ToKeyDict(request.TagDefinitions);

                // 3. Reference validation.
                ValidateReferences(request, importTypeNames, existingTypeNames, importTagDefs, existingTagDefs, errors);
                if (errors.Count > 0) return Fail(builder, errors);

                // 4. Conflict validation (only when policy is "fail").
                if (onConflict == "fail")
                {
                    ValidateConflicts(request, existingTypeNames, existingTagDefs, existingResourceKeys, errors);
                    if (errors.Count > 0) return Fail(builder, errors);
                }

                // 5. Write phase. 'fail' already cleared conflict validation; map it to 'skip'
                //    defensively so a row created concurrently mid-import is skipped, not overwritten.
                var effectiveOnConflict = onConflict == "fail" ? "skip" : onConflict;
                var summary = await ExecuteWritesAsync(request, effectiveOnConflict, cancellationToken);
                builder.Data.Set(new ImportResponse { Success = true, Summary = summary });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        // ---------------- validation ----------------

        private static void ValidateSchema(ImportRequest request, List<ImportError> errors)
        {
            if (request.Version != "1.0")
                errors.Add(Err("document", null, "version", $"Unsupported version '{request.Version}'; expected '1.0'"));

            var policy = request.Policy?.OnConflict?.ToLowerInvariant();
            if (policy == null || Array.IndexOf(AllowedPolicies, policy) < 0)
                errors.Add(Err("document", null, "policy.onConflict",
                    $"Invalid onConflict '{request.Policy?.OnConflict}'; expected upsert|skip|fail"));

            AddDuplicateKeyErrors("resourceTypes", request.ResourceTypes?.Keys, errors);
            AddDuplicateKeyErrors("tagDefinitions", request.TagDefinitions?.Keys, errors);
            AddDuplicateKeyErrors("resources", request.Resources?.Select(r => r.Key), errors);

            foreach (var r in request.Resources ?? new List<ImportResourceItem>())
            {
                if (string.IsNullOrWhiteSpace(r.Key))
                    errors.Add(Err("resources", r.Key, "key", "Resource key is required"));
                if (string.IsNullOrWhiteSpace(r.Name))
                    errors.Add(Err("resources", r.Key, "name", "Resource name is required"));
            }

            foreach (var kv in request.TagDefinitions ?? new Dictionary<string, ImportTagDefinitionModel>())
            {
                if ((kv.Value.ContentType?.Length ?? 0) > 50)
                    errors.Add(Err("tagDefinitions", kv.Key, "contentType", "contentType exceeds 50 characters"));

                if (kv.Value.AllowedValues != null
                    && JsonSerializer.Serialize(kv.Value.AllowedValues).Length > 2000)
                    errors.Add(Err("tagDefinitions", kv.Key, "allowedValues",
                        "allowedValues exceeds 2000 characters when serialized"));
            }
        }

        private static void ValidateReferences(
            ImportRequest request,
            HashSet<string> importTypeNames,
            HashSet<string> existingTypeNames,
            Dictionary<string, ImportTagDefinitionModel> importTagDefs,
            Dictionary<string, TagDefinition> existingTagDefs,
            List<ImportError> errors)
        {
            foreach (var r in request.Resources ?? new List<ImportResourceItem>())
            {
                if (!string.IsNullOrWhiteSpace(r.Type)
                    && !importTypeNames.Contains(r.Type)
                    && !existingTypeNames.Contains(r.Type))
                {
                    errors.Add(Err("resources", r.Key, "type",
                        $"ResourceType '{r.Type}' not found in payload or database"));
                }

                if (r.Tags == null) continue;

                foreach (var tag in r.Tags)
                {
                    var key = tag.Key;
                    var hasImportDef = importTagDefs.TryGetValue(key, out var importDef);
                    var hasDbDef = existingTagDefs.TryGetValue(key, out var dbDef);

                    if (!hasImportDef && !hasDbDef)
                    {
                        errors.Add(Err("resources", r.Key, $"tags.{key}",
                            $"Tag key '{key}' has no definition in payload or database"));
                        continue;
                    }

                    var values = ExtractValues(tag.Value, out var isArray);

                    var isMultiValued = hasImportDef ? importDef!.IsMultiValued : dbDef!.IsMultiValued;
                    if (isArray && !isMultiValued)
                        errors.Add(Err("resources", r.Key, $"tags.{key}",
                            $"Tag '{key}' received an array value but is not multi-valued"));

                    var allowCustom = hasImportDef ? importDef!.AllowCustomValue : dbDef!.AllowCustomValue;
                    if (!allowCustom)
                    {
                        var allowed = hasImportDef ? importDef!.AllowedValues : ParseAllowedValues(dbDef!.AllowedValues);
                        var allowedSet = new HashSet<string>(allowed ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        foreach (var v in values)
                        {
                            if (!allowedSet.Contains(v))
                                errors.Add(Err("resources", r.Key, $"tags.{key}",
                                    $"Value '{v}' is not in allowedValues for tag '{key}'"));
                        }
                    }
                }
            }
        }

        private static void ValidateConflicts(
            ImportRequest request,
            HashSet<string> existingTypeNames,
            Dictionary<string, TagDefinition> existingTagDefs,
            HashSet<string> existingResourceKeys,
            List<ImportError> errors)
        {
            foreach (var typeName in request.ResourceTypes?.Keys ?? Enumerable.Empty<string>())
                if (existingTypeNames.Contains(typeName))
                    errors.Add(Err("resourceTypes", typeName, null, $"ResourceType '{typeName}' already exists (onConflict=fail)"));

            foreach (var key in request.TagDefinitions?.Keys ?? Enumerable.Empty<string>())
                if (existingTagDefs.ContainsKey(key))
                    errors.Add(Err("tagDefinitions", key, null, $"TagDefinition '{key}' already exists (onConflict=fail)"));

            foreach (var r in request.Resources ?? new List<ImportResourceItem>())
                if (!string.IsNullOrWhiteSpace(r.Key) && existingResourceKeys.Contains(r.Key))
                    errors.Add(Err("resources", r.Key, null, $"Resource '{r.Key}' already exists (onConflict=fail)"));
        }

        // ---------------- write phase ----------------

        private async Task<ImportSummary> ExecuteWritesAsync(ImportRequest request, string onConflict, CancellationToken ct)
        {
            var summary = new ImportSummary();

            if (request.ResourceTypes != null)
            {
                foreach (var kv in request.ResourceTypes)
                {
                    var result = await _importRepo.UpsertResourceTypeAsync(
                        kv.Key, NewUid("rt-"), kv.Value.AllowCustomTags, onConflict, ct);
                    TallyResult(summary.ResourceTypes, result);
                }
            }

            if (request.TagDefinitions != null)
            {
                foreach (var kv in request.TagDefinitions)
                {
                    var allowedJson = kv.Value.AllowedValues != null
                        ? JsonSerializer.Serialize(kv.Value.AllowedValues)
                        : null;
                    var result = await _importRepo.UpsertTagDefinitionAsync(
                        kv.Key, NewUid("td-"), kv.Value.ContentType,
                        kv.Value.AllowCustomValue, kv.Value.IsMultiValued, allowedJson, onConflict, ct);
                    TallyResult(summary.TagDefinitions, result);
                }
            }

            foreach (var r in request.Resources ?? new List<ImportResourceItem>())
            {
                var (result, resourceId) = await _importRepo.UpsertResourceAsync(
                    r.Key, NewUid("res-"), r.Type, r.Name, r.Description, onConflict, ct);
                TallyResult(summary.Resources, result);

                // skip-means-skip: do not touch tags for a skipped resource.
                if (result != "skipped")
                    await _importRepo.SetResourceTagsAsync(resourceId, NormalizeTags(r.Tags), ct);
            }

            return summary;
        }

        private static void TallyResult(ImportSectionSummary section, string result)
        {
            switch (result)
            {
                case "created": section.Created++; break;
                case "updated": section.Updated++; break;
                default: section.Skipped++; break;
            }
        }

        private static string NewUid(string prefix) => prefix + Guid.NewGuid().ToString("N");

        private static List<(string TagKey, string TagValue)> NormalizeTags(Dictionary<string, JsonElement>? tags)
        {
            var result = new List<(string, string)>();
            if (tags == null) return result;
            foreach (var tag in tags)
            {
                foreach (var v in ExtractValues(tag.Value, out _))
                    result.Add((tag.Key, v));
            }
            return result;
        }

        // ---------------- helpers ----------------

        private static HashSet<string> ToKeySet(Dictionary<string, ImportResourceTypeDefinition>? dict)
            => new(dict?.Keys ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, ImportTagDefinitionModel> ToKeyDict(Dictionary<string, ImportTagDefinitionModel>? dict)
        {
            var result = new Dictionary<string, ImportTagDefinitionModel>(StringComparer.OrdinalIgnoreCase);
            if (dict != null)
                foreach (var kv in dict)
                    result[kv.Key] = kv.Value;
            return result;
        }

        private static List<string> ExtractValues(JsonElement element, out bool isArray)
        {
            isArray = element.ValueKind == JsonValueKind.Array;
            var result = new List<string>();
            if (isArray)
            {
                foreach (var item in element.EnumerateArray())
                    result.Add(item.ToString());
            }
            else
            {
                result.Add(element.ToString());
            }
            return result;
        }

        private static List<string> ParseAllowedValues(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static void AddDuplicateKeyErrors(string section, IEnumerable<string>? keys, List<ImportError> errors)
        {
            if (keys == null) return;
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var k in keys)
            {
                if (string.IsNullOrEmpty(k)) continue;
                if (!seen.Add(k) && reported.Add(k))
                    errors.Add(Err(section, k, "key", $"Duplicate key '{k}' (keys are case-insensitive)"));
            }
        }

        private static ApiServiceResponse<ImportResponse> Fail(ServiceResponseBuilder<ImportResponse> builder, List<ImportError> errors)
        {
            builder.Data.Set(new ImportResponse { Success = false, Errors = errors });
            builder.Validation.AddValidation("import", "Import failed validation; see data.errors");
            return builder.BuildResponse();
        }

        private static ImportError Err(string section, string? key, string? field, string message)
            => new() { Section = section, Key = key, Field = field, Message = message };
    }
}
