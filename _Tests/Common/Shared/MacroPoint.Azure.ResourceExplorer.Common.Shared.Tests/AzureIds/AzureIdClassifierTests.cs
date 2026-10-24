using MacroPoint.Azure.ResourceExplorer.Common.Shared.AzureIds;

// ReSharper disable InconsistentNaming

namespace MacroPoint.Azure.ResourceExplorer.Common.Shared.Tests.AzureIds
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceExplorer")]
    [Trait("Category", "ResourceExplorer/Common")]
    [Trait("Category", "ResourceExplorer/Common/Shared")]
    [Trait("Category", "ResourceExplorer/Common/Shared/AzureIds")]
    [Trait("Category", "ResourceExplorer/Common/Shared/AzureIds/AzureIdClassifier")]
    public class AzureIdClassifierTests
    {
        private const string LegacySub = "11111111-1111-1111-1111-111111111111";
        private const string NewSub = "22222222-2222-2222-2222-222222222222";
        private static readonly string[] Subs = { LegacySub };
        private static readonly string[] Prefixes = { "usspi" };

        [Fact]
        public void Classify_LegacySubscription_IsOld()
        {
            var id = $"/subscriptions/{LegacySub}/resourceGroups/rg-app/providers/Microsoft.Web/sites/app1";

            AzureIdClassifier.Classify(id, Subs, Prefixes).Should().Be(AzureIdAge.Old, "because the subscription is configured as legacy");
        }

        [Fact]
        public void Classify_LegacySubscriptionDifferentCase_IsOld()
        {
            var id = $"/SUBSCRIPTIONS/{LegacySub.ToUpperInvariant()}/resourceGroups/rg-app/providers/Microsoft.Web/sites/app1";

            AzureIdClassifier.Classify(id, Subs, Prefixes).Should().Be(AzureIdAge.Old, "because matching ignores case");
        }

        [Fact]
        public void Classify_LegacyNamePrefixOnResourceName_IsOld()
        {
            var id = $"/subscriptions/{NewSub}/resourceGroups/rg-app/providers/Microsoft.Web/sites/usspi-aps-pd-visibility";

            AzureIdClassifier.Classify(id, Subs, Prefixes).Should().Be(AzureIdAge.Old, "because the resource name starts with a legacy prefix");
        }

        [Fact]
        public void Classify_LegacyNamePrefixOnResourceGroup_IsOld()
        {
            var id = $"/subscriptions/{NewSub}/resourceGroups/USSPI-rg-prod/providers/Microsoft.Web/sites/app1";

            AzureIdClassifier.Classify(id, Subs, Prefixes).Should().Be(AzureIdAge.Old, "because the resource group starts with a legacy prefix");
        }

        [Fact]
        public void Classify_NoMarkerMatches_IsNew()
        {
            var id = $"/subscriptions/{NewSub}/resourceGroups/rg-app/providers/Microsoft.Web/sites/app1";

            AzureIdClassifier.Classify(id, Subs, Prefixes).Should().Be(AzureIdAge.New, "because nothing marks it legacy");
        }

        [Fact]
        public void Classify_PrefixInMiddleOfName_IsNew()
        {
            var id = $"/subscriptions/{NewSub}/resourceGroups/rg-app/providers/Microsoft.Web/sites/my-usspi-app";

            AzureIdClassifier.Classify(id, Subs, Prefixes).Should().Be(AzureIdAge.New, "because only a leading prefix marks a legacy name");
        }

        [Fact]
        public void Classify_NoMarkersConfigured_IsNew()
        {
            var id = $"/subscriptions/{LegacySub}/resourceGroups/usspi-rg/providers/Microsoft.Web/sites/usspi-app";

            AzureIdClassifier.Classify(id, Array.Empty<string>(), Array.Empty<string>())
                .Should().Be(AzureIdAge.New, "because with no legacy markers every id is new");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Classify_BlankValue_IsNew(string? value)
        {
            AzureIdClassifier.Classify(value, Subs, Prefixes).Should().Be(AzureIdAge.New, "because a blank value carries no legacy marker");
        }

        [Fact]
        public void Classify_BareNameWithLegacyPrefix_IsOld()
        {
            AzureIdClassifier.Classify("usspi-aps-pd-visibility", Subs, Prefixes)
                .Should().Be(AzureIdAge.Old, "because a bare name is judged by its own prefix");
        }
    }
}
