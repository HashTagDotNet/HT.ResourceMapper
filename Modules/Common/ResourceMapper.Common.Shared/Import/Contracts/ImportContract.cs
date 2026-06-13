using System.Text.Json;

namespace ResourceMapper.Common.Shared.Import.Contracts
{
    public class ImportRequest
    {
        public string Version { get; set; } = "1.0";
        public ImportPolicy Policy { get; set; } = new();
        public Dictionary<string, ImportResourceTypeDefinition>? ResourceTypes { get; set; }
        public Dictionary<string, ImportTagDefinitionModel>? TagDefinitions { get; set; }
        public List<ImportResourceItem> Resources { get; set; } = [];
    }

    public class ImportPolicy
    {
        public string OnConflict { get; set; } = "upsert"; // "upsert" | "skip" | "fail"
    }

    public class ImportResourceTypeDefinition
    {
        public bool AllowCustomTags { get; set; } = true;
    }

    public class ImportTagDefinitionModel
    {
        /// <summary>
        /// Free-form content type, max 50 chars. Auto-registered in TagContentType on import.
        /// </summary>
        public string ContentType { get; set; } = "string";
        public bool AllowCustomValue { get; set; } = true;
        public bool IsMultiValued { get; set; } = false;
        public List<string>? AllowedValues { get; set; }
    }

    public class ImportResourceItem
    {
        public string Key { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// Raw JSON tag values: string = single value, array = multi-value.
        /// Normalized to List&lt;string&gt; by the import service.
        /// </summary>
        public Dictionary<string, JsonElement>? Tags { get; set; }
        public List<string>? Dependencies { get; set; }
    }

    public class ImportResponse
    {
        public bool Success { get; set; }
        public ImportSummary? Summary { get; set; }
        public List<ImportError>? Errors { get; set; }
    }

    public class ImportSummary
    {
        public ImportSectionSummary ResourceTypes { get; set; } = new();
        public ImportSectionSummary TagDefinitions { get; set; } = new();
        public ImportSectionSummary Resources { get; set; } = new();
    }

    public class ImportSectionSummary
    {
        public int Created { get; set; }
        public int Updated { get; set; }
        public int Skipped { get; set; }
    }

    public class ImportError
    {
        public string Section { get; set; } = "";
        public string? Key { get; set; }
        public string? Field { get; set; }
        public string Message { get; set; } = "";
    }
}
