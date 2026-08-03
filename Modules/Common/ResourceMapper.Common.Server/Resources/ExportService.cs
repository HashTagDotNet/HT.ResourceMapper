using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;
using ResourceMapper.Common.Server.Resources.Interfaces;
using ResourceMapper.Common.Server.Resources.Models;
using ResourceMapper.Common.Shared.Export.Contracts;

namespace ResourceMapper.Common.Server.Resources
{
    /// <summary>
    /// Serializes the whole catalog into the <b>import</b> JSON format (ImportExportApiDesign #4),
    /// with each resource's <c>uid</c> included (#5). The output is designed to be fed straight back
    /// into <see cref="IImportService"/>: re-importing an unmodified export must report every
    /// resource as updated/skipped and create nothing.
    ///
    /// Three things make that round-trip hold, and each is load-bearing:
    ///  • The domain tag is emitted <i>inside</i> each resource's tag map, not as a batch-level
    ///    default, so import re-derives the same (Domain + Type + Key) identity per resource.
    ///  • A tag is emitted as an array only when its definition is multi-valued — import rejects an
    ///    array supplied for a single-valued definition.
    ///  • Only same-domain dependency edges are emitted, because import resolves a dependency key
    ///    within the dependent's own domain and would fail validation on anything else.
    /// </summary>
    public class ExportService : IExportService
    {
        private readonly IExportRepository _repo;

        public ExportService(IExportRepository repo)
        {
            _repo = repo;
        }

        public async Task<ApiServiceResponse<ExportResult>> ExportAsync(
            ExportOptions options, CancellationToken cancellationToken)
        {
            var builder = new ServiceResponseBuilder<ExportResult>();
            try
            {
                options ??= new ExportOptions();

                var resources = await _repo.GetResourcesAsync(cancellationToken);
                var tagRows = await _repo.GetResourceTagsAsync(cancellationToken);
                var dependencyRows = await _repo.GetDependenciesAsync(cancellationToken);

                var result = new ExportResult
                {
                    Document = new ExportDocument
                    {
                        Version = "1.0",
                        Policy = new ExportPolicy { OnConflict = options.OnConflict }
                    }
                };

                if (options.IncludeResourceTypes)
                    result.Document.ResourceTypes = await BuildResourceTypesAsync(cancellationToken);

                if (options.IncludeTagDefinitions)
                {
                    result.Document.TagDefinitions = await BuildTagDefinitionsAsync(cancellationToken);
                    result.Warnings.Add(
                        "tagDefinitions were included. The import contract cannot carry displayName, " +
                        "requirementLevel, isDomainTag, isSystemTag or displayOrder, so re-importing this " +
                        "document will reset those columns to their defaults — including clearing the " +
                        "domain-tag designation. Only import this section into an empty database.");
                }

                var tagsByResource = tagRows
                    .GroupBy(t => t.ResourceId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var dependenciesByResource = BuildDependencyMap(dependencyRows, result.Warnings);

                foreach (var row in resources)
                {
                    var item = new ExportResourceItem
                    {
                        Uid = row.ResourceUid,
                        Key = row.ResourceKey,
                        Name = row.ResourceName,
                        Type = row.TypeName,
                        Description = string.IsNullOrWhiteSpace(row.Description) ? null : row.Description,
                        Tags = BuildTagMap(tagsByResource, row, result.Warnings),
                        Dependencies = dependenciesByResource.TryGetValue(row.ResourceId, out var deps) && deps.Count > 0
                            ? deps
                            : null
                    };

                    result.Document.Resources.Add(item);
                }

                builder.Data.Set(result);
                return builder.BuildResponse();
            }
            catch (Exception ex)
            {
                builder.Errors.AddError(CallStatusCode.InternalError,
                    "An unexpected error occurred.", ex.Message, "InternalError");
                return builder.BuildResponse();
            }
        }

        // ---------------- reference sections ----------------

        private async Task<Dictionary<string, ExportResourceTypeDefinition>> BuildResourceTypesAsync(
            CancellationToken cancellationToken)
        {
            var types = await _repo.GetResourceTypesAsync(cancellationToken);
            var map = new Dictionary<string, ExportResourceTypeDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in types.OrderBy(t => t.TypeName, StringComparer.OrdinalIgnoreCase))
                map[t.TypeName] = new ExportResourceTypeDefinition { AllowCustomTags = t.AllowCustomTags };
            return map;
        }

        private async Task<Dictionary<string, ExportTagDefinitionModel>> BuildTagDefinitionsAsync(
            CancellationToken cancellationToken)
        {
            var defs = await _repo.GetTagDefinitionsAsync(cancellationToken);
            var map = new Dictionary<string, ExportTagDefinitionModel>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in defs.OrderBy(d => d.DisplayOrder).ThenBy(d => d.TagDefinitionKey, StringComparer.OrdinalIgnoreCase))
            {
                var allowed = ParseAllowedValues(d.AllowedValues);
                map[d.TagDefinitionKey] = new ExportTagDefinitionModel
                {
                    // Import only accepts the seeded Text|Link codes; fall back to Text when the
                    // denormalized code is missing rather than emitting something import rejects.
                    ContentType = string.IsNullOrWhiteSpace(d.ContentType) ? "Text" : d.ContentType,
                    AllowCustomValue = d.AllowCustomValue,
                    IsMultiValued = d.IsMultiValued,
                    AllowedValues = allowed.Count > 0 ? allowed : null
                };
            }
            return map;
        }

        // ---------------- per-resource projection ----------------

        /// <summary>
        /// Builds the tag map for one resource. Multi-valued definitions emit an array (even for a
        /// single value, matching the definition rather than the current row count); single-valued
        /// definitions emit a bare string. A single-valued definition holding more than one row is a
        /// data anomaly — ResourceTag has no unique constraint on (ResourceId, TagDefinitionId) — so
        /// the first value wins and the rest are reported, because emitting an array there would
        /// fail import validation.
        /// </summary>
        private static Dictionary<string, object?>? BuildTagMap(
            Dictionary<int, List<ExportTagRow>> tagsByResource,
            ExportResourceRow row,
            List<string> warnings)
        {
            if (!tagsByResource.TryGetValue(row.ResourceId, out var rows) || rows.Count == 0)
                return null;

            var map = new Dictionary<string, object?>();

            foreach (var group in rows.GroupBy(t => t.TagDefinitionKey, StringComparer.OrdinalIgnoreCase))
            {
                var values = group
                    .Select(t => t.TagValue)
                    .Where(v => v is not null)
                    .Select(v => v!)
                    .ToList();

                if (values.Count == 0) continue;

                var isMultiValued = group.First().IsMultiValued;

                if (isMultiValued)
                {
                    map[group.Key] = values;
                    continue;
                }

                if (values.Count > 1)
                {
                    warnings.Add(
                        $"Resource '{row.ResourceKey}' has {values.Count} values for single-valued tag " +
                        $"'{group.Key}'. Only the first ('{values[0]}') was exported — the import format " +
                        "cannot express extra values for a single-valued tag definition.");
                }

                map[group.Key] = values[0];
            }

            return map.Count > 0 ? map : null;
        }

        /// <summary>
        /// Groups dependency edges by dependent resource, dropping any edge whose endpoints are in
        /// different domains. Import resolves <c>dependencies[]</c> as bare keys scoped to the
        /// dependent's own effective domain, so a cross-domain key is unresolvable there and would
        /// fail the whole import; the edge is reported as a warning instead of being emitted.
        /// </summary>
        private static Dictionary<int, List<string>> BuildDependencyMap(
            List<ExportDependencyRow> rows, List<string> warnings)
        {
            var map = new Dictionary<int, List<string>>();

            foreach (var row in rows)
            {
                if (!DomainsMatch(row.FromDomain, row.ToDomain))
                {
                    warnings.Add(
                        $"Dependency '{row.FromResourceKey}' ({DomainLabel(row.FromDomain)}) -> " +
                        $"'{row.ToResourceKey}' ({DomainLabel(row.ToDomain)}) crosses domains and was " +
                        "omitted: the import format resolves dependency keys within the dependent's " +
                        "own domain and cannot express this edge.");
                    continue;
                }

                if (!map.TryGetValue(row.FromResourceId, out var list))
                {
                    list = new List<string>();
                    map[row.FromResourceId] = list;
                }

                if (!list.Contains(row.ToResourceKey, StringComparer.OrdinalIgnoreCase))
                    list.Add(row.ToResourceKey);
            }

            return map;
        }

        // ---------------- helpers ----------------

        private static bool DomainsMatch(string? a, string? b)
            => string.Equals(NormKey(a), NormKey(b), StringComparison.Ordinal);

        private static string NormKey(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

        private static string DomainLabel(string? domain)
            => string.IsNullOrWhiteSpace(domain) ? "no domain" : domain;

        private static List<string> ParseAllowedValues(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
