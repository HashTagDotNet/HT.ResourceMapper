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
    /// Validates an import document, then writes resource types, tag definitions, resources
    /// (with their tags), and DependsOn relationship edges into the database. Validation runs
    /// fully before any write; on a validation error nothing is written and a 400 with the error
    /// list is returned. There is no cross-call transaction (write volume is low; per-resource tag
    /// replacement is atomic in its sproc).
    ///
    /// Identity is (Domain + ResourceType + ResourceKey) when a domain tag is defined
    /// (IsDomainTag=1); falls back to (ResourceType + ResourceKey) otherwise. Domain resolves per
    /// resource as: the resource's own Tags[domainKey] value, else Defaults.Domain.
    /// </summary>
    public class ImportService : IImportService
    {
        private static readonly string[] AllowedPolicies = { "upsert", "skip", "fail" };
        private static readonly string[] AllowedContentTypes = { "Text", "Link" };

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

                // 2. Load DB reference data (read-only).
                var existingTypeNames = (await _resourceRepo.GetAllResourceTypesAsync(cancellationToken))
                    .Select(t => t.TypeName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var existingTagDefs = (await _importRepo.GetAllTagDefinitionsAsync(cancellationToken))
                    .GroupBy(d => d.TagDefinitionKey, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var existingIdentities = await _importRepo.GetAllResourceIdentitiesAsync(cancellationToken);

                var domain = new DomainResolver(existingTagDefs);

                // Import-declared sections, case-insensitive (schema validation already ruled out case-only dups).
                var importTypeNames = ToKeySet(request.ResourceTypes);
                var importTagDefs = ToKeyDict(request.TagDefinitions);

                // 3. Reference validation (needs DB reference data + resolved domain concept).
                ValidateReferences(request, importTypeNames, existingTypeNames, importTagDefs, existingTagDefs, domain, errors);
                if (errors.Count > 0) return Fail(builder, errors);

                // 4. Identity validation: triple dedup within the payload + (only for onConflict=fail)
                //    conflict against existing identities.
                ValidateResourceIdentities(request, domain, existingIdentities, onConflict, errors);
                if (errors.Count > 0) return Fail(builder, errors);

                // 5. Dependency resolution/validation — runs BEFORE any write so an unresolved or
                //    ambiguous dependency fails the whole import cleanly (nothing written).
                var pendingDependencies = ValidateAndResolveDependencies(request, domain, existingIdentities, errors);
                if (errors.Count > 0) return Fail(builder, errors);

                // 6. Write phase. 'fail' already cleared conflict validation; map it to 'skip'
                //    defensively so a row created concurrently mid-import is skipped, not overwritten.
                var effectiveOnConflict = onConflict == "fail" ? "skip" : onConflict;
                var summary = await ExecuteWritesAsync(request, effectiveOnConflict, domain, existingIdentities, pendingDependencies, cancellationToken);
                builder.Data.Set(new ImportResponse { Success = true, Summary = summary });
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError, "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        // ---------------- domain resolution ----------------

        /// <summary>
        /// Resolves the single IsDomainTag=1 definition (if any) and computes each resource's
        /// effective domain: its own Tags[domainKey] value, else the batch Defaults.Domain,
        /// canonicalized to the definition's AllowedValues casing. Returns null (skip all domain
        /// logic) when no domain tag is defined (design §6 "Unused" state).
        /// </summary>
        private sealed class DomainResolver
        {
            private readonly TagDefinition? _def;
            private readonly List<string> _allowedValues;

            public DomainResolver(Dictionary<string, TagDefinition> existingTagDefs)
            {
                _def = existingTagDefs.Values.FirstOrDefault(d => d.IsDomainTag);
                _allowedValues = _def != null ? ParseAllowedValues(_def.AllowedValues) : new List<string>();
            }

            public bool IsDefined => _def != null;
            public string? Key => _def?.TagDefinitionKey;
            public bool Required => string.Equals(_def?.RequirementLevel, "Error", StringComparison.OrdinalIgnoreCase);
            public bool AllowCustomValue => _def?.AllowCustomValue ?? true;
            public IReadOnlyList<string> AllowedValues => _allowedValues;

            /// <summary>The resource's own per-resource override value, if its Tags declare the domain key.</summary>
            public string? RawOverride(ImportResourceItem r)
            {
                if (!IsDefined || Key == null || r.Tags == null) return null;
                if (!r.Tags.TryGetValue(Key, out var element)) return null;
                return ExtractValues(element, out _).FirstOrDefault();
            }

            /// <summary>Override ?? batch default; null if neither is set or no domain tag is defined.</summary>
            public string? EffectiveRaw(ImportResourceItem r, ImportRequest request)
                => IsDefined ? (RawOverride(r) ?? request.Defaults?.Domain) : null;

            /// <summary>The canonicalized effective domain used for matching/storing.</summary>
            public string? Canonical(ImportResourceItem r, ImportRequest request)
            {
                var raw = EffectiveRaw(r, request);
                if (string.IsNullOrWhiteSpace(raw)) return null;
                var canon = _allowedValues.FirstOrDefault(v => string.Equals(v, raw, StringComparison.OrdinalIgnoreCase));
                return canon ?? raw;
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
            // Resources dedup moves to the (domain+type+key) triple (ValidateResourceIdentities) —
            // a bare-key dedup here would wrongly reject the valid same-key/different-domain case.

            foreach (var r in request.Resources ?? new List<ImportResourceItem>())
            {
                if (string.IsNullOrWhiteSpace(r.Key))
                    errors.Add(Err("resources", r.Key, "key", "Resource key is required"));
                if (string.IsNullOrWhiteSpace(r.Name))
                    errors.Add(Err("resources", r.Key, "name", "Resource name is required"));
                if (string.IsNullOrWhiteSpace(r.Type))
                    errors.Add(Err("resources", r.Key, "type", "Resource type is required (identity-bearing)"));
            }

            foreach (var kv in request.TagDefinitions ?? new Dictionary<string, ImportTagDefinitionModel>())
            {
                // Legacy compat: the old default was "string"; treat that (or blank) as Text.
                // Content type is otherwise a constrained set (Text|Link) — no auto-registration.
                if (string.IsNullOrWhiteSpace(kv.Value.ContentType) || string.Equals(kv.Value.ContentType, "string", StringComparison.OrdinalIgnoreCase))
                    kv.Value.ContentType = "Text";

                if (Array.IndexOf(AllowedContentTypes, kv.Value.ContentType) < 0)
                    errors.Add(Err("tagDefinitions", kv.Key, "contentType", $"contentType must be 'Text' or 'Link' (got '{kv.Value.ContentType}')"));

                if (kv.Value.ContentType.Length > 50)
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
            DomainResolver domain,
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

                // Domain required / vocab validation (only when a domain tag is defined).
                if (domain.IsDefined)
                {
                    var raw = domain.EffectiveRaw(r, request);
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        if (domain.Required)
                            errors.Add(Err("resources", r.Key, "domain", "Domain is required"));
                    }
                    else if (!domain.AllowCustomValue && domain.AllowedValues.Count > 0
                             && !domain.AllowedValues.Any(v => string.Equals(v, raw, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add(Err("resources", r.Key, "domain",
                            $"Domain '{raw}' is not in the allowed values ({string.Join(", ", domain.AllowedValues)})"));
                    }
                }

                if (r.Tags == null) continue;

                foreach (var tag in r.Tags)
                {
                    var key = tag.Key;

                    // The domain tag itself is validated above (required/vocab), not as a generic tag.
                    if (domain.IsDefined && string.Equals(key, domain.Key, StringComparison.OrdinalIgnoreCase))
                        continue;

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

        /// <summary>
        /// Triple-based ((Domain + Type + Key)) duplicate detection within the payload, and (only
        /// for onConflict=fail) conflict detection against existing resources. Replaces the old
        /// bare-key checks, which would wrongly reject a valid same-key/different-domain pair.
        /// </summary>
        private static void ValidateResourceIdentities(
            ImportRequest request,
            DomainResolver domain,
            List<ResourceIdentity> existingIdentities,
            string onConflict,
            List<ImportError> errors)
        {
            var resources = request.Resources ?? new List<ImportResourceItem>();

            var seen = new HashSet<(string Domain, string Type, string Key)>();
            var reported = new HashSet<(string Domain, string Type, string Key)>();
            foreach (var r in resources)
            {
                if (string.IsNullOrWhiteSpace(r.Key) || string.IsNullOrWhiteSpace(r.Type)) continue; // already flagged as required
                var triple = (NormKey(domain.Canonical(r, request)), NormKey(r.Type), NormKey(r.Key));
                if (!seen.Add(triple) && reported.Add(triple))
                    errors.Add(Err("resources", r.Key, "key",
                        $"Duplicate resource identity (domain+type+key) for '{r.Key}'"));
            }

            if (onConflict != "fail") return;

            var existingTripleSet = new HashSet<(string Domain, string Type, string Key)>(
                existingIdentities.Select(id => (NormKey(id.Domain), NormKey(id.TypeName), NormKey(id.ResourceKey))));

            foreach (var r in resources)
            {
                if (string.IsNullOrWhiteSpace(r.Key) || string.IsNullOrWhiteSpace(r.Type)) continue;
                var triple = (NormKey(domain.Canonical(r, request)), NormKey(r.Type), NormKey(r.Key));
                if (existingTripleSet.Contains(triple))
                    errors.Add(Err("resources", r.Key, null, $"Resource '{r.Key}' already exists (onConflict=fail)"));
            }
        }

        /// <summary>One dependency edge validated as resolvable, ready to write once its endpoints exist.</summary>
        private sealed record PendingDependency(ImportResourceItem Source, string TargetKey, string TargetType, string? Domain);

        /// <summary>
        /// Resolves each resource's Dependencies[] (flat keys) against the combined identity set
        /// (existing DB rows ∪ payload resources), scoped to the dependent's own effective domain
        /// (OQ1 default). Exactly one (domain+key) match required; zero → unresolved, more than
        /// one (same key, different types) → ambiguous. Self-references are rejected outright.
        /// Runs entirely before any write.
        /// </summary>
        private static List<PendingDependency> ValidateAndResolveDependencies(
            ImportRequest request,
            DomainResolver domain,
            List<ResourceIdentity> existingIdentities,
            List<ImportError> errors)
        {
            var resources = request.Resources ?? new List<ImportResourceItem>();
            var pending = new List<PendingDependency>();

            // (domain,key) -> distinct type names seen, across existing rows AND payload resources.
            var typeIndex = new Dictionary<(string Domain, string Key), List<string>>();

            void Index(string? d, string? type, string? key)
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(type)) return;
                var idxKey = (NormKey(d), NormKey(key));
                if (!typeIndex.TryGetValue(idxKey, out var list))
                {
                    list = new List<string>();
                    typeIndex[idxKey] = list;
                }
                if (!list.Contains(type, StringComparer.OrdinalIgnoreCase))
                    list.Add(type);
            }

            foreach (var id in existingIdentities)
                Index(id.Domain, id.TypeName, id.ResourceKey);
            foreach (var r in resources)
                Index(domain.Canonical(r, request), r.Type, r.Key);

            foreach (var r in resources)
            {
                if (r.Dependencies == null || string.IsNullOrWhiteSpace(r.Key)) continue;
                var effectiveDomain = domain.Canonical(r, request);

                foreach (var depKey in r.Dependencies)
                {
                    if (string.IsNullOrWhiteSpace(depKey)) continue;

                    if (string.Equals(depKey, r.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(Err("resources", r.Key, "dependencies", $"Resource '{r.Key}' cannot depend on itself"));
                        continue;
                    }

                    var idxKey = (NormKey(effectiveDomain), NormKey(depKey));
                    if (!typeIndex.TryGetValue(idxKey, out var matchedTypes) || matchedTypes.Count == 0)
                    {
                        errors.Add(Err("resources", r.Key, "dependencies",
                            $"Dependency '{depKey}' could not be resolved in domain '{effectiveDomain}'"));
                        continue;
                    }

                    if (matchedTypes.Count > 1)
                    {
                        errors.Add(Err("resources", r.Key, "dependencies",
                            $"Dependency '{depKey}' is ambiguous in domain '{effectiveDomain}' — matches types: {string.Join(", ", matchedTypes)}"));
                        continue;
                    }

                    pending.Add(new PendingDependency(r, depKey.Trim(), matchedTypes[0], effectiveDomain));
                }
            }

            return pending;
        }

        // ---------------- write phase ----------------

        private async Task<ImportSummary> ExecuteWritesAsync(
            ImportRequest request,
            string onConflict,
            DomainResolver domain,
            List<ResourceIdentity> existingIdentities,
            List<PendingDependency> pendingDependencies,
            CancellationToken ct)
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

            // Seed resolved identities from pre-existing rows so a dependency target not
            // re-declared in this payload can still be linked.
            var resolvedIds = new Dictionary<(string Domain, string Key, string Type), int>();
            foreach (var id in existingIdentities)
                resolvedIds[(NormKey(id.Domain), NormKey(id.ResourceKey), NormKey(id.TypeName))] = id.ResourceId;

            var skippedSourceKeys = new HashSet<(string Domain, string Key)>();

            foreach (var r in request.Resources ?? new List<ImportResourceItem>())
            {
                var effectiveDomain = domain.Canonical(r, request);

                var (result, resourceId) = await _importRepo.UpsertResourceAsync(
                    r.Key, NewUid("res-"), r.Type, r.Name, r.Description, effectiveDomain, onConflict, ct);
                TallyResult(summary.Resources, result);

                resolvedIds[(NormKey(effectiveDomain), NormKey(r.Key), NormKey(r.Type))] = resourceId;

                // skip-means-skip: do not touch tags or dependency edges for a skipped resource.
                if (result == "skipped")
                {
                    skippedSourceKeys.Add((NormKey(effectiveDomain), NormKey(r.Key)));
                    continue;
                }

                await _importRepo.SetResourceTagsAsync(resourceId, NormalizeTags(r.Tags, domain.Key), ct);
            }

            // Dependency edges are written only after every resource in this run has a resolved
            // id (including pre-existing targets seeded above). Additive-only: AddRelationshipAsync
            // is idempotent, so a re-import never removes an edge dropped from a later Dependencies[].
            foreach (var dep in pendingDependencies)
            {
                if (skippedSourceKeys.Contains((NormKey(dep.Domain), NormKey(dep.Source.Key))))
                    continue;

                if (!resolvedIds.TryGetValue((NormKey(dep.Domain), NormKey(dep.Source.Key), NormKey(dep.Source.Type)), out var fromId))
                    continue;
                if (!resolvedIds.TryGetValue((NormKey(dep.Domain), NormKey(dep.TargetKey), NormKey(dep.TargetType)), out var toId))
                    continue;

                await _resourceRepo.AddRelationshipAsync(fromId, toId, ct);
                summary.ResourceRelationships.Created++;
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

        private static List<(string TagKey, string TagValue)> NormalizeTags(Dictionary<string, JsonElement>? tags, string? excludeKey)
        {
            var result = new List<(string, string)>();
            if (tags == null) return result;
            foreach (var tag in tags)
            {
                if (excludeKey != null && string.Equals(tag.Key, excludeKey, StringComparison.OrdinalIgnoreCase))
                    continue; // the domain tag is written/owned by Resource_Upsert, not SetForResource.

                foreach (var v in ExtractValues(tag.Value, out _))
                    result.Add((tag.Key, v));
            }
            return result;
        }

        // ---------------- helpers ----------------

        private static string NormKey(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

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
