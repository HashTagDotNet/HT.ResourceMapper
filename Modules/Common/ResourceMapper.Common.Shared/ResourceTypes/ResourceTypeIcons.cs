namespace ResourceMapper.Common.Shared.ResourceTypes
{
    /// <summary>
    /// The icon keys the explorer canvas actually knows how to draw.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>ICON_PATHS</c> / <c>TYPE_COLORS</c> in
    /// <c>wwwroot/js/explorer/explorer-canvas.js</c>. Kept here so the resource-type editor can
    /// offer a picklist instead of a free-text box — an unrecognised key silently renders as a
    /// neutral dot on the default blue, which looks like a rendering bug rather than a data
    /// problem. Free text is still accepted by the service (the column is just VARCHAR(40)); this
    /// list only drives the UI's suggestions.
    /// </remarks>
    public static class ResourceTypeIcons
    {
        public static readonly IReadOnlyList<string> KnownKeys = new[]
        {
            "web", "apps", "settings", "insights", "memory",
            "database", "bus", "queue", "hub", "folder", "dns"
        };

        /// <summary>Node fill colour for a key, matching TYPE_COLORS. Unknown keys get the
        /// canvas default so the preview never lies about what the graph will show.</summary>
        public static string ColorFor(string? iconKey) => iconKey switch
        {
            "web" or "apps" => "#2563EB",
            "insights" => "#7c3aed",
            "memory" => "#dc2626",
            "database" => "#4338ca",
            "bus" or "queue" => "#ea580c",
            "settings" or "dns" => "#0d9488",
            "hub" => "#0891b2",
            "folder" => "#64748b",
            _ => "#2563EB"
        };
    }
}
