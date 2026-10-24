namespace MacroPoint.Azure.ResourceExplorer.Common.Shared.AzureIds
{
    public enum AzureIdAge
    {
        New,
        Old
    }

    /// <summary>
    /// Classifies an Azure resource id value as Old or New against configured legacy markers.
    /// Old when the id's subscription is a legacy subscription, or its resource group or resource name
    /// (last segment) starts with a legacy prefix. Comparisons are case-insensitive.
    /// </summary>
    public static class AzureIdClassifier
    {
        private const string SubscriptionsSegment = "subscriptions";
        private const string ResourceGroupsSegment = "resourcegroups";

        public static AzureIdAge Classify(
            string? azureId,
            IReadOnlyCollection<string> legacySubscriptionIds,
            IReadOnlyCollection<string> legacyNamePrefixes)
        {
            if (string.IsNullOrWhiteSpace(azureId))
                return AzureIdAge.New;

            var segments = azureId.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var subscription = SegmentAfter(segments, SubscriptionsSegment);
            if (subscription is not null
                && legacySubscriptionIds.Any(s => string.Equals(s, subscription, StringComparison.OrdinalIgnoreCase)))
                return AzureIdAge.Old;

            var resourceGroup = SegmentAfter(segments, ResourceGroupsSegment);
            var name = segments.Length > 0 ? segments[^1] : null;
            var hasLegacyPrefix = legacyNamePrefixes.Any(p =>
                !string.IsNullOrEmpty(p)
                && ((resourceGroup?.StartsWith(p, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (name?.StartsWith(p, StringComparison.OrdinalIgnoreCase) ?? false)));

            return hasLegacyPrefix ? AzureIdAge.Old : AzureIdAge.New;
        }

        private static string? SegmentAfter(string[] segments, string marker)
        {
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (string.Equals(segments[i], marker, StringComparison.OrdinalIgnoreCase))
                    return segments[i + 1];
            }
            return null;
        }
    }
}
