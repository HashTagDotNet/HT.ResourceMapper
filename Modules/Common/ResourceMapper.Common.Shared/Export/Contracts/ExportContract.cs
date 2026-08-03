using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResourceMapper.Common.Shared.Export.Contracts
{
    /// <summary>
    /// The exported catalog document. This is deliberately the <b>same JSON shape the import
    /// endpoint consumes</b> (ImportExportApiDesign decision #4) with the addition of a per-resource
    /// <c>uid</c> (decision #5), which import ignores as an unmapped property.
    ///
    /// Round-trip contract: <c>JsonSerializer.Serialize(document, ExportJson.SerializerOptions)</c>
    /// must deserialize cleanly into <c>ImportRequest</c> and re-import without creating duplicates.
    /// Property order here is the emitted order, so keep it aligned with the design doc's sample.
    /// </summary>
    public sealed class ExportDocument
    {
        public string Version { get; set; } = "1.0";

        public ExportPolicy Policy { get; set; } = new();

        /// <summary>
        /// Optional reference section. Safe to re-import: <c>ResourceType_Upsert</c>'s UPDATE only
        /// touches AllowCustomTags, leaving ShortCode/IconKey intact.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ExportResourceTypeDefinition>? ResourceTypes { get; set; }

        /// <summary>
        /// Optional reference section, <b>off by default</b>. The import contract cannot carry
        /// DisplayName / RequirementLevel / IsDomainTag / IsSystemTag / DisplayOrder, so re-importing
        /// this section resets those columns to <c>TagDefinition_Upsert</c>'s defaults — which would
        /// clear the IsDomainTag designation the whole identity model depends on.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, ExportTagDefinitionModel>? TagDefinitions { get; set; }

        public List<ExportResourceItem> Resources { get; set; } = [];
    }

    public sealed class ExportPolicy
    {
        /// <summary>"upsert" | "skip" | "fail" — mirrors ImportPolicy.OnConflict.</summary>
        public string OnConflict { get; set; } = "upsert";
    }

    public sealed class ExportResourceTypeDefinition
    {
        public bool AllowCustomTags { get; set; } = true;
    }

    public sealed class ExportTagDefinitionModel
    {
        /// <summary>"Text" | "Link" — the seeded TagContentType codes import accepts.</summary>
        public string ContentType { get; set; } = "Text";

        public bool AllowCustomValue { get; set; } = true;

        public bool IsMultiValued { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? AllowedValues { get; set; }
    }

    public sealed class ExportResourceItem
    {
        /// <summary>
        /// Stable resource UID (decision #5). Emitted for traceability only — <c>key</c> is the
        /// match field on import, and import ignores this property.
        /// </summary>
        public string Uid { get; set; } = string.Empty;

        public string Key { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Description { get; set; }

        /// <summary>
        /// Tag map (decision #8): a bare string for a single-valued definition, an array for a
        /// multi-valued one. Includes the domain tag, which is how a re-import re-establishes each
        /// resource's (Domain + Type + Key) identity without relying on a batch-level default.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, object?>? Tags { get; set; }

        /// <summary>
        /// Flat dependency keys (out-edges). Only same-domain edges appear here: import resolves a
        /// dependency key within the dependent's own domain, so a cross-domain edge is not
        /// expressible in this format and is reported as a warning instead.
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Dependencies { get; set; }
    }

    /// <summary>Service result: the document plus anything that could not be represented faithfully.</summary>
    public sealed class ExportResult
    {
        public ExportDocument Document { get; set; } = new();

        /// <summary>Non-fatal fidelity notes (e.g. omitted cross-domain dependency edges).</summary>
        public List<string> Warnings { get; set; } = [];

        public int ResourceCount => Document.Resources.Count;

        public int DependencyCount => Document.Resources.Sum(r => r.Dependencies?.Count ?? 0);
    }

    /// <summary>Caller-selectable export switches.</summary>
    public sealed class ExportOptions
    {
        /// <summary>Emit the <c>resourceTypes</c> section. Safe to re-import.</summary>
        public bool IncludeResourceTypes { get; set; } = true;

        /// <summary>
        /// Emit the <c>tagDefinitions</c> section. Default false — see
        /// <see cref="ExportDocument.TagDefinitions"/> for why re-importing it is lossy.
        /// </summary>
        public bool IncludeTagDefinitions { get; set; }

        /// <summary>Value written to <c>policy.onConflict</c> in the exported document.</summary>
        public string OnConflict { get; set; } = "upsert";
    }

    /// <summary>Canonical serializer settings, shared by the UI download and the round-trip tests.</summary>
    public static class ExportJson
    {
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string Serialize(ExportDocument document)
            => JsonSerializer.Serialize(document, SerializerOptions);
    }
}
