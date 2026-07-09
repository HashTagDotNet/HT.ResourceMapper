using ResourceMapper.Common.Shared.HomePage.Contracts;
using ResourceMapper.Common.Shared.HomePage.Filtering;

// ReSharper disable InconsistentNaming

namespace ResourceMapper.Common.Shared.Tests.HomePage.Filtering
{
    [Trait("Category", "Unit")]
    [Trait("Category", "ResourceMapper")]
    [Trait("Category", "ResourceMapper/Common")]
    [Trait("Category", "ResourceMapper/Common/Shared")]
    [Trait("Category", "ResourceMapper/Common/Shared/HomePage")]
    [Trait("Category", "ResourceMapper/Common/Shared/HomePage/Filtering")]
    [Trait("Category", "ResourceMapper/Common/Shared/HomePage/Filtering/ResourceFilterModels")]
    public class ResourceFilterModelsTests
    {
        #region ToGridRequest

        [Fact]
        public void ToGridRequest_EnumerableNoSelectionNoBlank_FilterOmitted()
        {
            // Arrange
            var state = new ResourceFilterState();
            state.Filters.Add(Enumerable("ResourceType"));

            // Act
            var request = state.ToGridRequest();

            // Assert
            request.Filters.Should().BeEmpty("because an enumerable with no values and no blank bucket does not constrain");
        }

        [Fact]
        public void ToGridRequest_IncludeBlankOnly_FilterIncluded()
        {
            // Arrange
            var state = new ResourceFilterState();
            var f = Enumerable("ResourceType");
            f.IncludeBlank = true;
            state.Filters.Add(f);

            // Act
            var request = state.ToGridRequest();

            // Assert
            request.Filters.Should().HaveCount(1, "because selecting the blank bucket is a constraint even with no values");
            request.Filters![0].IncludeBlank.Should().BeTrue("because the blank bucket was selected");
            request.Filters![0].Values.Should().BeEmpty("because no explicit values were selected");
        }

        [Fact]
        public void ToGridRequest_TextBlank_FilterOmitted()
        {
            // Arrange
            var state = new ResourceFilterState();
            state.Filters.Add(Text("ResourceName", "   "));

            // Act
            var request = state.ToGridRequest();

            // Assert
            request.Filters.Should().BeEmpty("because a text filter with only whitespace does not constrain");
        }

        [Fact]
        public void ToGridRequest_TextNonBlank_MapsToContainsTrimmed()
        {
            // Arrange
            var state = new ResourceFilterState();
            state.Filters.Add(Text("ResourceName", "  api  "));

            // Act
            var request = state.ToGridRequest();

            // Assert
            request.Filters.Should().HaveCount(1, "because non-blank text constrains");
            request.Filters![0].Operator.Should().Be(ResourceGridFilterOperator.Contains, "because text filters use Contains");
            request.Filters![0].Text.Should().Be("api", "because the text value is trimmed");
        }

        [Fact]
        public void ToGridRequest_EnumerableValues_AreSortedOrdinal()
        {
            // Arrange
            var state = new ResourceFilterState();
            var f = Enumerable("ResourceType", "Storage", "Compute", "Analytics");
            state.Filters.Add(f);

            // Act
            var request = state.ToGridRequest();

            // Assert
            request.Filters![0].Values.Should().ContainInOrder("Analytics", "Compute", "Storage")
                .And.HaveCount(3, "because enumerable values are emitted ordinal-sorted");
        }

        [Fact]
        public void ToGridRequest_State_CarriesSearchOrderAndDisplayCount()
        {
            // Arrange
            var state = new ResourceFilterState
            {
                SearchFor = "api",
                OrderBy = "TypeName",
                OrderDirection = "Desc",
                DisplayCount = 500
            };

            // Act
            var request = state.ToGridRequest();

            // Assert
            request.Skip.Should().Be(0, "because the top-N model always starts at zero");
            request.Take.Should().Be(500, "because Take reflects the display count");
            request.SearchFor.Should().Be("api", "because the search text is carried through");
            request.OrderBy.Should().Be("TypeName", "because the sort column is carried through");
            request.OrderDirection.Should().Be("Desc", "because the sort direction is carried through");
        }

        #endregion

        #region ChipLabel

        [Fact]
        public void ChipLabel_TextFilter_ShowsContains()
        {
            var f = Text("ResourceName", "slug");
            ResourceFilterState.ChipLabel(f).Should().Be("Name contains \"slug\"", "because text filters read as contains");
        }

        [Fact]
        public void ChipLabel_EnumerableUnconstrained_ShowsAny()
        {
            var f = Enumerable("ResourceType");
            ResourceFilterState.ChipLabel(f).Should().Be("Type: any", "because no value and no blank means unconstrained");
        }

        [Fact]
        public void ChipLabel_EnumerableEqualsSingleValue_ShowsInline()
        {
            var f = Enumerable("ResourceType", "Storage");
            ResourceFilterState.ChipLabel(f).Should().Be("Type: Storage", "because exactly one value shows inline");
        }

        [Fact]
        public void ChipLabel_EnumerableEqualsMultipleValues_ShowsCount()
        {
            var f = Enumerable("ResourceType", "Storage", "Compute", "Analytics");
            ResourceFilterState.ChipLabel(f).Should().Be("Type: 3 selected", "because more than one token shows a count");
        }

        [Fact]
        public void ChipLabel_EnumerableNotEqualsSingle_ShowsNotValue()
        {
            var f = Enumerable("ResourceType", "Deprecated");
            f.Operator = ResourceGridFilterOperator.NotEquals;
            ResourceFilterState.ChipLabel(f).Should().Be("Type: not Deprecated", "because single NotEquals reads as 'not value'");
        }

        [Fact]
        public void ChipLabel_EnumerableNotEqualsMultiple_ShowsNotCount()
        {
            var f = Enumerable("ResourceType", "Deprecated", "Legacy");
            f.Operator = ResourceGridFilterOperator.NotEquals;
            ResourceFilterState.ChipLabel(f).Should().Be("Type: not 2 selected", "because multi NotEquals reads as 'not N selected'");
        }

        [Fact]
        public void ChipLabel_BlankBucketOnly_ShowsBlankToken()
        {
            var f = Enumerable("ResourceType");
            f.IncludeBlank = true;
            ResourceFilterState.ChipLabel(f).Should().Be("Type: (blank)", "because the sole selected token is the blank bucket");
        }

        [Fact]
        public void ChipLabel_TagFilter_UsesTagKeyAsDisplay()
        {
            var f = Enumerable("Tag", "prod");
            f.TagKey = "Environment";
            ResourceFilterState.ChipLabel(f).Should().Be("Environment: prod", "because tag chips use the tag key as the display name");
        }

        [Fact]
        public void ChipLabel_DescriptionText_UsesDescriptionDisplay()
        {
            var f = Text("Description", "cache");
            ResourceFilterState.ChipLabel(f).Should().Be("Description contains \"cache\"", "because Description keeps its own name");
        }

        [Fact]
        public void ChipLabel_ValuePlusBlank_CountsBlankAsToken()
        {
            var f = Enumerable("ResourceType", "Storage");
            f.IncludeBlank = true;
            ResourceFilterState.ChipLabel(f).Should().Be("Type: 2 selected", "because the blank bucket counts as a token alongside the value");
        }

        #endregion

        #region AllCheckboxState

        [Fact]
        public void AllCheckboxState_AllSelected_ReturnsTrue()
        {
            ResourceFilterState.AllCheckboxState(3, 3, false, false).Should().Be(true, "because every available value is selected");
        }

        [Fact]
        public void AllCheckboxState_NoneSelected_ReturnsFalse()
        {
            ResourceFilterState.AllCheckboxState(0, 3, false, false).Should().Be(false, "because nothing is selected");
        }

        [Fact]
        public void AllCheckboxState_PartialSelected_ReturnsIndeterminate()
        {
            ResourceFilterState.AllCheckboxState(1, 3, false, false).Should().BeNull("because a partial selection is indeterminate");
        }

        [Fact]
        public void AllCheckboxState_AllValuesPlusBlank_ReturnsTrue()
        {
            ResourceFilterState.AllCheckboxState(3, 3, true, true).Should().Be(true, "because all values and the blank bucket are selected");
        }

        [Fact]
        public void AllCheckboxState_AllValuesButBlankUnselected_ReturnsIndeterminate()
        {
            ResourceFilterState.AllCheckboxState(3, 3, false, true).Should().BeNull("because the available blank bucket is not selected");
        }

        [Fact]
        public void AllCheckboxState_OnlyBlankSelected_ReturnsIndeterminate()
        {
            ResourceFilterState.AllCheckboxState(0, 3, true, true).Should().BeNull("because only the blank bucket is selected");
        }

        [Fact]
        public void AllCheckboxState_NothingAvailable_ReturnsFalse()
        {
            ResourceFilterState.AllCheckboxState(0, 0, false, false).Should().Be(false, "because there is nothing available or selected");
        }

        #endregion

        #region IsConstraining

        [Fact]
        public void IsConstraining_TextWithValue_True()
        {
            ResourceFilterState.IsConstraining(Text("ResourceName", "x")).Should().BeTrue("because non-blank text constrains");
        }

        [Fact]
        public void IsConstraining_TextBlank_False()
        {
            ResourceFilterState.IsConstraining(Text("ResourceName", "  ")).Should().BeFalse("because whitespace text does not constrain");
        }

        [Fact]
        public void IsConstraining_EnumerableEmpty_False()
        {
            ResourceFilterState.IsConstraining(Enumerable("ResourceType")).Should().BeFalse("because no values and no blank does not constrain");
        }

        #endregion

        #region URL round-trip

        [Fact]
        public void RoundTrip_EmptyState_Equal()
        {
            AssertRoundTrip(new ResourceFilterState());
        }

        [Fact]
        public void RoundTrip_SearchOnly_Equal()
        {
            AssertRoundTrip(new ResourceFilterState { SearchFor = "api gateway" });
        }

        [Fact]
        public void RoundTrip_EnumerableEquals_Equal()
        {
            var state = new ResourceFilterState();
            state.Filters.Add(Enumerable("ResourceType", "Compute", "Storage"));
            AssertRoundTrip(state);
        }

        [Fact]
        public void RoundTrip_TextFilter_Equal()
        {
            var state = new ResourceFilterState();
            state.Filters.Add(Text("Description", "cache node"));
            AssertRoundTrip(state);
        }

        [Fact]
        public void RoundTrip_TagFilter_Equal()
        {
            var state = new ResourceFilterState();
            var f = Enumerable("Tag", "prod", "dev");
            f.TagKey = "Environment";
            state.Filters.Add(f);
            AssertRoundTrip(state);
        }

        [Fact]
        public void RoundTrip_NotEquals_Equal()
        {
            var state = new ResourceFilterState();
            var f = Enumerable("ResourceType", "Deprecated");
            f.Operator = ResourceGridFilterOperator.NotEquals;
            state.Filters.Add(f);
            AssertRoundTrip(state);
        }

        [Fact]
        public void RoundTrip_IncludeBlank_Equal()
        {
            var state = new ResourceFilterState();
            var f = Enumerable("ResourceType", "Storage");
            f.IncludeBlank = true;
            state.Filters.Add(f);
            AssertRoundTrip(state);
        }

        [Fact]
        public void RoundTrip_IncludeBlankOnly_Equal()
        {
            var state = new ResourceFilterState();
            var f = Enumerable("Tag");
            f.TagKey = "Owner";
            f.IncludeBlank = true;
            state.Filters.Add(f);
            AssertRoundTrip(state);
        }

        [Fact]
        public void RoundTrip_MultipleFilters_Equal()
        {
            var state = new ResourceFilterState
            {
                SearchFor = "api",
                OrderBy = "TypeName",
                OrderDirection = "Desc",
                DisplayCount = 500
            };
            state.Filters.Add(Enumerable("ResourceType", "Compute", "Storage"));
            state.Filters.Add(Text("ResourceName", "gateway"));
            var tag1 = Enumerable("Tag", "prod");
            tag1.TagKey = "Environment";
            state.Filters.Add(tag1);
            var tag2 = Enumerable("Tag", "ana@x.com");
            tag2.TagKey = "Owner";
            tag2.Operator = ResourceGridFilterOperator.NotEquals;
            state.Filters.Add(tag2);
            AssertRoundTrip(state);
        }

        [Theory]
        [InlineData("has~tilde")]
        [InlineData("has,comma")]
        [InlineData("has:colon")]
        [InlineData("has@at")]
        [InlineData("has%percent")]
        [InlineData("has space")]
        [InlineData("blank")]      // equals the blank sentinel text — must not be confused with the bucket
        [InlineData("(blank)")]    // equals the blank display text
        [InlineData("a&b=c#d?e")]
        public void RoundTrip_TrickyValues_Equal(string trickyValue)
        {
            // Arrange — a value AND the blank bucket, so the sentinel disambiguation is exercised.
            var state = new ResourceFilterState();
            var f = Enumerable("ResourceType", trickyValue, "Normal");
            f.IncludeBlank = true;
            state.Filters.Add(f);

            // Act
            var parsed = ResourceFilterState.FromQueryString(state.ToQueryString());

            // Assert
            parsed.Should().Be(state, $"because the value '{trickyValue}' must survive escaping alongside the blank bucket");
            parsed.Filters[0].SelectedValues.Should().Contain(trickyValue, "because the tricky value decodes back exactly");
            parsed.Filters[0].IncludeBlank.Should().BeTrue("because the blank bucket must remain distinct from a literal 'blank' value");
        }

        [Fact]
        public void RoundTrip_TrickyTagKey_Equal()
        {
            var state = new ResourceFilterState();
            var f = Enumerable("Tag", "v1");
            f.TagKey = "weird:key~with,chars";
            state.Filters.Add(f);
            AssertRoundTrip(state);
            var parsed = ResourceFilterState.FromQueryString(state.ToQueryString());
            parsed.Filters[0].TagKey.Should().Be("weird:key~with,chars", "because a tag key with delimiters round-trips");
        }

        [Fact]
        public void FromQueryString_AcceptsFullUri()
        {
            var state = new ResourceFilterState { SearchFor = "api" };
            var full = "https://host/app/page" + state.ToQueryString();
            ResourceFilterState.FromQueryString(full).Should().Be(state, "because a full URI is accepted as well as a bare query string");
        }

        #endregion

        #region Determinism

        [Fact]
        public void ToQueryString_SameStateDifferentValueOrder_Identical()
        {
            // Arrange — same logical state, values added in different order.
            var a = new ResourceFilterState();
            a.Filters.Add(Enumerable("ResourceType", "Storage", "Compute", "Analytics"));

            var b = new ResourceFilterState();
            b.Filters.Add(Enumerable("ResourceType", "Analytics", "Storage", "Compute"));

            // Act & Assert
            a.ToQueryString().Should().Be(b.ToQueryString(), "because value lists are ordinal-sorted before serialization");
        }

        [Fact]
        public void ToQueryString_SameStateDifferentFilterOrder_Identical()
        {
            // Arrange — same filters, added in different order.
            var a = new ResourceFilterState();
            a.Filters.Add(Enumerable("ResourceType", "Compute"));
            var aTag = Enumerable("Tag", "prod");
            aTag.TagKey = "Environment";
            a.Filters.Add(aTag);

            var b = new ResourceFilterState();
            var bTag = Enumerable("Tag", "prod");
            bTag.TagKey = "Environment";
            b.Filters.Add(bTag);
            b.Filters.Add(Enumerable("ResourceType", "Compute"));

            // Act & Assert
            a.ToQueryString().Should().Be(b.ToQueryString(), "because filters are ordered by column then tag key before serialization");
        }

        #endregion

        #region Robustness

        [Fact]
        public void FromQueryString_UnknownColumn_Dropped()
        {
            var state = ResourceFilterState.FromQueryString("?f=Bogus~eq~x");
            state.Filters.Should().BeEmpty("because an unknown column token is dropped");
        }

        [Fact]
        public void FromQueryString_UnknownTagKey_Kept()
        {
            var state = ResourceFilterState.FromQueryString("?f=tag:Whatever~eq~v1");
            state.Filters.Should().HaveCount(1, "because tag keys are arbitrary and must be preserved");
            state.Filters[0].TagKey.Should().Be("Whatever", "because the arbitrary tag key decodes through");
        }

        [Fact]
        public void FromQueryString_MoreThanFourFilters_ClampedToFour()
        {
            var q = "?f=Type~eq~a&f=Name~ct~b&f=Description~ct~c&f=tag:Env~eq~d&f=tag:Owner~eq~e";
            var state = ResourceFilterState.FromQueryString(q);
            state.Filters.Should().HaveCount(4, "because at most four filters are kept");
        }

        [Fact]
        public void FromQueryString_BadDisplayCount_ClampedToDefault()
        {
            ResourceFilterState.FromQueryString("?n=37").DisplayCount.Should().Be(100, "because an out-of-set display count falls back to the default");
            ResourceFilterState.FromQueryString("?n=abc").DisplayCount.Should().Be(100, "because a non-numeric display count falls back to the default");
        }

        [Theory]
        [InlineData(50)]
        [InlineData(100)]
        [InlineData(500)]
        public void FromQueryString_ValidDisplayCount_Preserved(int n)
        {
            ResourceFilterState.FromQueryString($"?n={n}").DisplayCount.Should().Be(n, "because a whitelisted display count is preserved");
        }

        [Fact]
        public void FromQueryString_BadSortColumn_DefaultsToResourceNameAsc()
        {
            var state = ResourceFilterState.FromQueryString("?s=DROP TABLE:desc");
            state.OrderBy.Should().Be("ResourceName", "because an unknown sort column falls back to the default");
            state.OrderDirection.Should().Be("Asc", "because the direction falls back with the default column");
        }

        [Fact]
        public void FromQueryString_ValidSort_Parsed()
        {
            var state = ResourceFilterState.FromQueryString("?s=TypeName:desc");
            state.OrderBy.Should().Be("TypeName", "because a whitelisted sort column is accepted");
            state.OrderDirection.Should().Be("Desc", "because the direction is normalized to PascalCase");
        }

        [Fact]
        public void FromQueryString_OperatorKindMismatch_Dropped()
        {
            // Text column with an enumerable operator.
            ResourceFilterState.FromQueryString("?f=Name~eq~x").Filters
                .Should().BeEmpty("because a text column with a non-contains operator is dropped");

            // Enumerable column with a contains operator.
            ResourceFilterState.FromQueryString("?f=Type~ct~x").Filters
                .Should().BeEmpty("because an enumerable column with a contains operator is dropped");
        }

        [Fact]
        public void FromQueryString_DuplicateColumn_KeepsFirst()
        {
            var state = ResourceFilterState.FromQueryString("?f=Type~eq~a&f=Type~eq~b");
            state.Filters.Should().HaveCount(1, "because filters are deduped by column keeping the first");
            state.Filters[0].SelectedValues.Should().Contain("a").And.NotContain("b", "because the first occurrence wins");
        }

        [Fact]
        public void FromQueryString_EmptyPayload_Dropped()
        {
            ResourceFilterState.FromQueryString("?f=Type~eq~").Filters
                .Should().BeEmpty("because an enumerable with an empty payload and no blank bucket is dropped");
            ResourceFilterState.FromQueryString("?f=Name~ct~").Filters
                .Should().BeEmpty("because a text filter with an empty payload is dropped");
        }

        [Theory]
        [InlineData("")]
        [InlineData("?")]
        [InlineData("not a query at all")]
        [InlineData("?f=&f=~~&f=Type&n=&s=")]
        [InlineData("?=&==&&&")]
        public void FromQueryString_GarbageInput_YieldsDefaultStateNoThrow(string input)
        {
            // Act
            var act = () => ResourceFilterState.FromQueryString(input);

            // Assert
            act.Should().NotThrow("because malformed input must never throw");
            var state = act();
            state.Filters.Should().BeEmpty("because garbage produces no filters");
            state.DisplayCount.Should().Be(100, "because garbage leaves the default display count");
            state.OrderBy.Should().Be("ResourceName", "because garbage leaves the default sort column");
        }

        #endregion

        #region Helpers

        private static ActiveFilter Enumerable(string column, params string[] values)
        {
            var f = new ActiveFilter
            {
                Column = column,
                Kind = ResourceGridFilterKind.Enumerable,
                Operator = ResourceGridFilterOperator.Equals
            };
            foreach (var v in values)
                f.SelectedValues.Add(v);
            return f;
        }

        private static ActiveFilter Text(string column, string text)
        {
            return new ActiveFilter
            {
                Column = column,
                Kind = ResourceGridFilterKind.Text,
                Operator = ResourceGridFilterOperator.Contains,
                Text = text
            };
        }

        private static void AssertRoundTrip(ResourceFilterState state)
        {
            var parsed = ResourceFilterState.FromQueryString(state.ToQueryString());
            parsed.Should().Be(state, "because FromQueryString(ToQueryString(state)) must reproduce the original state");
        }

        #endregion
    }
}
