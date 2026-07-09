using System.Text;
using ResourceMapper.Common.Shared.HomePage.Contracts;

namespace ResourceMapper.Common.Shared.HomePage.Filtering
{
    /// <summary>
    /// A single active column filter held in client state. Plain POCO (no MudBlazor / UI types) so it
    /// serializes cleanly for URL and future saved-filter layers. Value-based equality is implemented so
    /// URL round-trip tests can assert equality directly; <see cref="SelectedValues"/> is compared as a set
    /// (order-independent) to match the deterministic-serialization contract.
    /// </summary>
    public class ActiveFilter : IEquatable<ActiveFilter>
    {
        /// <summary>"ResourceType" | "ResourceName" | "Description" | "Tag".</summary>
        public string Column { get; set; } = "";

        /// <summary>The tag key (e.g. "Environment") — set iff <see cref="Column"/> == "Tag".</summary>
        public string? TagKey { get; set; }

        public ResourceGridFilterKind Kind { get; set; }

        public ResourceGridFilterOperator Operator { get; set; } = ResourceGridFilterOperator.Equals;

        /// <summary>Enumerable OR-set. Iteration order is NEVER serialized — always sorted first.</summary>
        public HashSet<string> SelectedValues { get; set; } = new(StringComparer.Ordinal);

        /// <summary>True when the "(blank)"/NULL bucket is selected.</summary>
        public bool IncludeBlank { get; set; }

        /// <summary>Contains text for text filters (ResourceName / Description).</summary>
        public string? Text { get; set; }

        public bool Equals(ActiveFilter? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            return string.Equals(Column, other.Column, StringComparison.Ordinal)
                   && string.Equals(TagKey, other.TagKey, StringComparison.Ordinal)
                   && Kind == other.Kind
                   && Operator == other.Operator
                   && IncludeBlank == other.IncludeBlank
                   && string.Equals(Text, other.Text, StringComparison.Ordinal)
                   && SelectedValues.SetEquals(other.SelectedValues);
        }

        public override bool Equals(object? obj) => Equals(obj as ActiveFilter);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Column, StringComparer.Ordinal);
            hash.Add(TagKey, StringComparer.Ordinal);
            hash.Add(Kind);
            hash.Add(Operator);
            hash.Add(IncludeBlank);
            hash.Add(Text, StringComparer.Ordinal);
            // Order-independent hash of the selected-value set.
            var setHash = 0;
            foreach (var v in SelectedValues)
                setHash ^= StringComparer.Ordinal.GetHashCode(v);
            hash.Add(setHash);
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// The full home-page filter/search/sort/display-count state. All serialization is deterministic
    /// (filters ordered by column then tag key; value lists sorted ordinal) so that two independent
    /// constructions of the same logical state produce byte-identical strings — required because
    /// <see cref="ToQueryString"/> doubles as a prerender snapshot cache key.
    /// </summary>
    public class ResourceFilterState : IEquatable<ResourceFilterState>
    {
        public const int MaxFilters = 4;

        public string? SearchFor { get; set; }

        public List<ActiveFilter> Filters { get; set; } = new();

        public string OrderBy { get; set; } = "ResourceName";

        public string OrderDirection { get; set; } = "Asc";

        public int DisplayCount { get; set; } = 100;

        // ---- Column token / display constants --------------------------------------------------

        private const string ColResourceType = "ResourceType";
        private const string ColResourceName = "ResourceName";
        private const string ColDescription = "Description";
        private const string ColTag = "Tag";

        private const string TokenType = "Type";
        private const string TokenName = "Name";
        private const string TokenDescription = "Description";
        private const string TokenTagPrefix = "tag:";

        /// <summary>Sentinel token (after the payload, tilde-delimited) that marks the blank bucket.</summary>
        private const string BlankSentinel = "blank";

        private const string BlankDisplay = "(blank)";

        private static readonly int[] AllowedDisplayCounts = { 50, 100, 500 };

        /// <summary>Sort columns accepted from the URL (mirrors the sproc @OrderBy whitelist).</summary>
        private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "ResourceId", "ResourceUid", "ResourceKey", "ResourceName",
            "Description", "CreatedOn", "UpdatedOn", "TypeName"
        };

        // ---- Pure logic ------------------------------------------------------------------------

        /// <summary>
        /// A filter constrains the result set iff it is a text filter with non-blank text, or an
        /// enumerable filter with at least one selected value or the blank bucket selected.
        /// </summary>
        public static bool IsConstraining(ActiveFilter f)
        {
            if (f.Kind == ResourceGridFilterKind.Text)
                return !string.IsNullOrWhiteSpace(f.Text);

            return f.SelectedValues.Count > 0 || f.IncludeBlank;
        }

        /// <summary>
        /// Projects the current state to a grid request. Skip=0, Take=DisplayCount; only constraining
        /// filters are emitted (non-constraining ones are omitted). Enumerable values are ordinal-sorted.
        /// </summary>
        public ResourceGridRequest ToGridRequest()
        {
            var definitions = new List<ResourceGridFilterDefinition>();

            foreach (var f in Filters)
            {
                if (!IsConstraining(f))
                    continue;

                var def = new ResourceGridFilterDefinition
                {
                    Column = f.Column,
                    TagKey = f.TagKey,
                    Kind = f.Kind,
                    Operator = f.Operator
                };

                if (f.Kind == ResourceGridFilterKind.Text)
                {
                    def.Text = f.Text?.Trim();
                }
                else
                {
                    def.Values = f.SelectedValues.OrderBy(v => v, StringComparer.Ordinal).ToList();
                    def.IncludeBlank = f.IncludeBlank;
                }

                definitions.Add(def);
            }

            return new ResourceGridRequest
            {
                Skip = 0,
                Take = DisplayCount,
                SearchFor = SearchFor,
                OrderBy = OrderBy,
                OrderDirection = OrderDirection,
                Filters = definitions
            };
        }

        /// <summary>Display name for a filter's column (tag filters use the tag key).</summary>
        public static string ColumnDisplay(ActiveFilter f) => f.Column switch
        {
            ColTag => f.TagKey ?? "",
            ColResourceType => TokenType,
            ColResourceName => TokenName,
            ColDescription => TokenDescription,
            _ => f.Column
        };

        /// <summary>Chip label per the locked chip-label rules (§1).</summary>
        public static string ChipLabel(ActiveFilter f)
        {
            var display = ColumnDisplay(f);

            if (f.Kind == ResourceGridFilterKind.Text)
                return $"{display} contains \"{f.Text}\"";

            var tokenCount = f.SelectedValues.Count + (f.IncludeBlank ? 1 : 0);

            if (tokenCount == 0)
                return $"{display}: any";

            var isNot = f.Operator == ResourceGridFilterOperator.NotEquals;

            if (tokenCount == 1)
            {
                var single = f.SelectedValues.Count == 1
                    ? f.SelectedValues.First()
                    : BlankDisplay; // the sole token is the blank bucket
                return isNot ? $"{display}: not {single}" : $"{display}: {single}";
            }

            return isNot ? $"{display}: not {tokenCount} selected" : $"{display}: {tokenCount} selected";
        }

        /// <summary>
        /// Tri-state resolver for an "All" checkbox. Returns true (all selected), false (none), or null
        /// (indeterminate). The blank bucket participates as one extra selectable item when available.
        /// </summary>
        public static bool? AllCheckboxState(int selectedCount, int totalCount, bool includeBlankSelected, bool blankAvailable)
        {
            var total = totalCount + (blankAvailable ? 1 : 0);
            var selected = selectedCount + (includeBlankSelected ? 1 : 0);

            if (total > 0 && selected == total) return true;
            if (selected == 0) return false;
            return null;
        }

        // ---- URL serialization (§5.1) ----------------------------------------------------------

        /// <summary>
        /// Serializes to the query string scheme: <c>?q=&amp;s=col:dir&amp;n=N&amp;f=col~op~payload[~blank]</c>.
        /// Deterministic: filters are ordered by (Column, TagKey) ordinal and value lists are ordinal-sorted,
        /// so two independent constructions of the same logical state produce byte-identical output.
        /// Every atom (search, tag key, each value, text) is escaped with <see cref="EscapeAtom"/> before it
        /// is joined with literal delimiters, so no atom can ever contain a structural delimiter.
        /// </summary>
        public string ToQueryString()
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(SearchFor))
                parts.Add("q=" + EscapeAtom(SearchFor));

            parts.Add("n=" + DisplayCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            parts.Add("s=" + OrderBy + ":" + OrderDirection.ToLowerInvariant());

            // Deterministic filter order: by Column then TagKey (ordinal). Only constraining filters emit.
            var ordered = Filters
                .Where(IsConstraining)
                .OrderBy(f => f.Column, StringComparer.Ordinal)
                .ThenBy(f => f.TagKey ?? "", StringComparer.Ordinal);

            foreach (var f in ordered)
            {
                var col = ColumnToken(f);
                if (col is null)
                    continue;

                var op = OperatorToken(f.Operator);
                string payload;

                if (f.Kind == ResourceGridFilterKind.Text)
                {
                    payload = EscapeAtom((f.Text ?? "").Trim());
                }
                else
                {
                    var values = f.SelectedValues
                        .OrderBy(v => v, StringComparer.Ordinal)
                        .Select(EscapeAtom);
                    payload = string.Join(",", values);
                    if (f.IncludeBlank)
                        payload = payload.Length == 0 ? "~" + BlankSentinel : payload + "~" + BlankSentinel;
                }

                parts.Add("f=" + col + "~" + op + "~" + payload);
            }

            return "?" + string.Join("&", parts);
        }

        /// <summary>
        /// Parses a query string (accepts either a full URI or a bare query string) into state.
        /// Fail-safe: never throws; malformed / unknown tokens are skipped and best-effort state is returned.
        /// </summary>
        public static ResourceFilterState FromQueryString(string uri)
        {
            var state = new ResourceFilterState();
            if (string.IsNullOrWhiteSpace(uri))
                return state;

            var query = ExtractQuery(uri);
            if (query.Length == 0)
                return state;

            var seenFilterKeys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = pair.IndexOf('=');
                var key = eq < 0 ? pair : pair.Substring(0, eq);
                var value = eq < 0 ? "" : pair.Substring(eq + 1);

                switch (key)
                {
                    case "q":
                        var search = DecodeAtom(value);
                        state.SearchFor = string.IsNullOrEmpty(search) ? null : search;
                        break;

                    case "n":
                        state.DisplayCount = ClampDisplayCount(value);
                        break;

                    case "s":
                        ApplySort(state, value);
                        break;

                    case "f":
                        if (state.Filters.Count >= MaxFilters)
                            break; // keep first MaxFilters valid filters
                        var filter = ParseFilter(value);
                        if (filter is null)
                            break;
                        var dedupeKey = filter.Column + "" + (filter.TagKey ?? "");
                        if (!seenFilterKeys.Add(dedupeKey))
                            break; // dedupe by (column, tag key), keeping first
                        state.Filters.Add(filter);
                        break;
                }
            }

            return state;
        }

        // ---- URL helpers -----------------------------------------------------------------------

        private static string ExtractQuery(string uri)
        {
            var q = uri.IndexOf('?');
            var slice = q >= 0 ? uri.Substring(q + 1) : uri;
            // Drop any fragment.
            var hash = slice.IndexOf('#');
            if (hash >= 0)
                slice = slice.Substring(0, hash);
            return slice;
        }

        private static int ClampDisplayCount(string value)
        {
            if (int.TryParse(value, System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out var n)
                && Array.IndexOf(AllowedDisplayCounts, n) >= 0)
            {
                return n;
            }
            return 100;
        }

        private static void ApplySort(ResourceFilterState state, string value)
        {
            var idx = value.IndexOf(':');
            if (idx < 0)
                return; // leave default ResourceName:Asc

            var col = value.Substring(0, idx);
            var dir = value.Substring(idx + 1);

            if (!AllowedSortColumns.Contains(col))
                return; // invalid column → keep default

            // Canonicalize to the whitelisted casing.
            foreach (var allowed in AllowedSortColumns)
            {
                if (string.Equals(allowed, col, StringComparison.OrdinalIgnoreCase))
                {
                    state.OrderBy = allowed;
                    break;
                }
            }

            state.OrderDirection = string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase) ? "Desc" : "Asc";
        }

        private static ActiveFilter? ParseFilter(string value)
        {
            // col~op~payload  (payload may itself end with ~blank for the enumerable blank bucket).
            var segments = value.Split('~');
            if (segments.Length < 3)
                return null;

            var colToken = segments[0];
            var opToken = segments[1];

            // Everything from index 2 onward is the payload; a trailing standalone "blank" segment is the
            // blank sentinel. Atoms never contain a literal '~' (EscapeAtom escapes it), so any '~' here is
            // structural.
            var includeBlank = false;
            int payloadEnd = segments.Length; // exclusive
            if (segments.Length >= 4 && segments[segments.Length - 1] == BlankSentinel)
            {
                includeBlank = true;
                payloadEnd = segments.Length - 1;
            }
            var payload = string.Join("~", segments.Skip(2).Take(payloadEnd - 2));

            if (!TryResolveColumn(colToken, out var column, out var tagKey, out var kind))
                return null;

            if (!TryResolveOperator(opToken, out var op))
                return null;

            // Enforce operator/kind consistency.
            if (kind == ResourceGridFilterKind.Enumerable
                && op != ResourceGridFilterOperator.Equals && op != ResourceGridFilterOperator.NotEquals)
                return null;
            if (kind == ResourceGridFilterKind.Text && op != ResourceGridFilterOperator.Contains)
                return null;

            var filter = new ActiveFilter
            {
                Column = column,
                TagKey = tagKey,
                Kind = kind,
                Operator = op
            };

            if (kind == ResourceGridFilterKind.Text)
            {
                var text = DecodeAtom(payload).Trim();
                if (text.Length == 0)
                    return null; // drop empty payload
                filter.Text = text;
            }
            else
            {
                if (payload.Length > 0)
                {
                    foreach (var raw in payload.Split(','))
                    {
                        var v = DecodeAtom(raw);
                        if (v.Length > 0)
                            filter.SelectedValues.Add(v);
                    }
                }
                filter.IncludeBlank = includeBlank;

                if (filter.SelectedValues.Count == 0 && !filter.IncludeBlank)
                    return null; // drop empty payload
            }

            return filter;
        }

        private static bool TryResolveColumn(string token, out string column, out string? tagKey, out ResourceGridFilterKind kind)
        {
            column = "";
            tagKey = null;
            kind = ResourceGridFilterKind.Enumerable;

            switch (token)
            {
                case TokenType:
                    column = ColResourceType;
                    kind = ResourceGridFilterKind.Enumerable;
                    return true;
                case TokenName:
                    column = ColResourceName;
                    kind = ResourceGridFilterKind.Text;
                    return true;
                case TokenDescription:
                    column = ColDescription;
                    kind = ResourceGridFilterKind.Text;
                    return true;
            }

            if (token.StartsWith(TokenTagPrefix, StringComparison.Ordinal))
            {
                var key = DecodeAtom(token.Substring(TokenTagPrefix.Length));
                if (key.Length == 0)
                    return false;
                column = ColTag;
                tagKey = key;
                kind = ResourceGridFilterKind.Enumerable;
                return true;
            }

            return false;
        }

        private static bool TryResolveOperator(string token, out ResourceGridFilterOperator op)
        {
            switch (token)
            {
                case "eq": op = ResourceGridFilterOperator.Equals; return true;
                case "ne": op = ResourceGridFilterOperator.NotEquals; return true;
                case "ct": op = ResourceGridFilterOperator.Contains; return true;
                default: op = ResourceGridFilterOperator.Equals; return false;
            }
        }

        private static string? ColumnToken(ActiveFilter f) => f.Column switch
        {
            ColResourceType => TokenType,
            ColResourceName => TokenName,
            ColDescription => TokenDescription,
            ColTag => string.IsNullOrEmpty(f.TagKey) ? null : TokenTagPrefix + EscapeAtom(f.TagKey!),
            _ => null
        };

        private static string OperatorToken(ResourceGridFilterOperator op) => op switch
        {
            ResourceGridFilterOperator.Equals => "eq",
            ResourceGridFilterOperator.NotEquals => "ne",
            ResourceGridFilterOperator.Contains => "ct",
            _ => "eq"
        };

        /// <summary>
        /// Escapes a single atom. <see cref="Uri.EscapeDataString"/> follows RFC 3986 and does NOT escape the
        /// unreserved tilde ('~'), which is our structural delimiter — so we additionally percent-escape it.
        /// Commas and colons are already escaped by EscapeDataString, so the ',' and ':' delimiters are safe.
        /// </summary>
        private static string EscapeAtom(string atom)
            => Uri.EscapeDataString(atom).Replace("~", "%7E");

        private static string DecodeAtom(string atom)
            => Uri.UnescapeDataString(atom);

        // ---- Value equality --------------------------------------------------------------------

        public bool Equals(ResourceFilterState? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!string.Equals(SearchFor, other.SearchFor, StringComparison.Ordinal)) return false;
            if (!string.Equals(OrderBy, other.OrderBy, StringComparison.Ordinal)) return false;
            if (!string.Equals(OrderDirection, other.OrderDirection, StringComparison.Ordinal)) return false;
            if (DisplayCount != other.DisplayCount) return false;
            if (Filters.Count != other.Filters.Count) return false;

            // Order-independent filter comparison (serialization order is deterministic but construction
            // order is not, so equality must not depend on list order).
            var mine = Filters.OrderBy(f => f.Column, StringComparer.Ordinal)
                              .ThenBy(f => f.TagKey ?? "", StringComparer.Ordinal)
                              .ThenBy(f => f.Operator).ToList();
            var theirs = other.Filters.OrderBy(f => f.Column, StringComparer.Ordinal)
                              .ThenBy(f => f.TagKey ?? "", StringComparer.Ordinal)
                              .ThenBy(f => f.Operator).ToList();

            for (var i = 0; i < mine.Count; i++)
                if (!mine[i].Equals(theirs[i]))
                    return false;

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as ResourceFilterState);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(SearchFor, StringComparer.Ordinal);
            hash.Add(OrderBy, StringComparer.Ordinal);
            hash.Add(OrderDirection, StringComparer.Ordinal);
            hash.Add(DisplayCount);
            var filterHash = 0;
            foreach (var f in Filters)
                filterHash ^= f.GetHashCode();
            hash.Add(filterHash);
            return hash.ToHashCode();
        }
    }
}
