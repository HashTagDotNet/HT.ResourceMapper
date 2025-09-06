// ReSharper disable InconsistentNaming
using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;

namespace HT.Api.Service.Contracts.Tests.BuildersOfTTests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Service")]
    [Trait("Category", "HT/Api/Service/Contracts")]
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT")]
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/LinksBuilder")]
    public class LinksBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestBuilderModel> _serviceResponseBuilder;
        private readonly LinksBuilder<TestBuilderModel> _linksBuilder;

        public LinksBuilderOfTTests()
        {
            _serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();
            _linksBuilder = new LinksBuilder<TestBuilderModel>(_serviceResponseBuilder);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalledWithValidParent_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();

            // Act
            var linksBuilder = new LinksBuilder<TestBuilderModel>(serviceResponseBuilder);

            // Assert
            linksBuilder.Should().NotBeNull("because constructor should create a valid instance");
        }

        [Fact]
        public void Constructor_WhenCalledWithNullParent_ShouldAllowNullButFailOnAccess()
        {
            // Arrange & Act
            var linksBuilder = new LinksBuilder<TestBuilderModel>(null!);

            // Assert
            linksBuilder.Should().NotBeNull("because constructor allows null parent");
            
            // But accessing properties should throw NullReferenceException
            Action accessAction = () => _ = linksBuilder.Http;
            accessAction.Should().Throw<NullReferenceException>("because null parent will cause NullReferenceException when accessing properties");
        }

        #endregion

        #region AddLink Method Tests

        [Fact]
        public void AddLink_WithHrefOnly_ShouldAddLinkToApiResponse()
        {
            // Arrange
            const string href = "https://api.example.com/users/123";

            // Act
            var result = _linksBuilder.AddLink(href);

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links.Should().NotBeNull("because links collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links!.Should().HaveCount(1, "because one link should be added");
            
            var addedLink = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0];
            addedLink.Href.Should().Be(href, "because href should be set correctly");
            addedLink.Rel.Should().BeNull("because rel was not specified");
            addedLink.Title.Should().BeNull("because title was not specified");
            addedLink.Method.Should().Be("GET", "because default method should be GET");
        }

        [Fact]
        public void AddLink_WithAllParameters_ShouldAddLinkWithAllPropertiesSet()
        {
            // Arrange
            const string href = "https://api.example.com/users/123";
            const string rel = "self";
            const string title = "User Details";
            const string method = "PUT";

            // Act
            var result = _linksBuilder.AddLink(href, rel, title, method);

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links!.Should().HaveCount(1, "because one link should be added");
            
            var addedLink = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0];
            addedLink.Href.Should().Be(href, "because href should be set correctly");
            addedLink.Rel.Should().Be(rel, "because rel should be set correctly");
            addedLink.Title.Should().Be(title, "because title should be set correctly");
            addedLink.Method.Should().Be(method, "because method should be set correctly");
        }

        [Fact]
        public void AddLink_WithNullOptionalParameters_ShouldAddLinkWithNullValues()
        {
            // Arrange
            const string href = "https://api.example.com/users";

            // Act
            var result = _linksBuilder.AddLink(href, null, null, null);

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links!.Should().HaveCount(1, "because one link should be added");
            
            var addedLink = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0];
            addedLink.Href.Should().Be(href, "because href should be set correctly");
            addedLink.Rel.Should().BeNull("because rel was passed as null");
            addedLink.Title.Should().BeNull("because title was passed as null");
            addedLink.Method.Should().Be("GET", "because default method should be GET when null is passed");
        }

        [Fact]
        public void AddLink_CalledMultipleTimes_ShouldAddMultipleLinks()
        {
            // Act
            _linksBuilder
                .AddLink("https://api.example.com/users/123", "self", "User Details")
                .AddLink("https://api.example.com/users/123/edit", "edit", "Edit User")
                .AddLink("https://api.example.com/users/123", "delete", "Delete User", "DELETE");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links!.Should().HaveCount(3, "because three links should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0].Rel.Should().Be("self", "because first link should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![1].Rel.Should().Be("edit", "because second link should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![2].Rel.Should().Be("delete", "because third link should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![2].Method.Should().Be("DELETE", "because DELETE method should be preserved");
        }

        [Fact]
        public void AddLink_WhenLinksCollectionIsNull_ShouldInitializeCollection()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links = null;

            // Act
            var result = _linksBuilder.AddLink("https://api.example.com/test");

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links.Should().NotBeNull("because links collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links!.Should().HaveCount(1, "because one link should be added");
        }

        #endregion

        #region Builder Navigation Properties Tests

        [Fact]
        public void Http_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var httpBuilder = _linksBuilder.Http;

            // Assert
            httpBuilder.Should().NotBeNull("because Http property should return a valid HttpApiResponseBuilder instance");
            httpBuilder.Should().Be(_serviceResponseBuilder.Http, "because Http property should return the same instance as parent");
        }

        [Fact]
        public void Validation_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var validationBuilder = _linksBuilder.Validation;

            // Assert
            validationBuilder.Should().NotBeNull("because Validation property should return a valid ValidationBuilder instance");
            validationBuilder.Should().Be(_serviceResponseBuilder.Validation, "because Validation property should return the same instance as parent");
        }

        [Fact]
        public void Errors_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var errorBuilder = _linksBuilder.Errors;

            // Assert
            errorBuilder.Should().NotBeNull("because Errors property should return a valid ErrorBuilder instance");
            errorBuilder.Should().Be(_serviceResponseBuilder.Errors, "because Errors property should return the same instance as parent");
        }

        [Fact]
        public void Data_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var dataBuilder = _linksBuilder.Data;

            // Assert
            dataBuilder.Should().NotBeNull("because Data property should return a valid DataBuilder instance");
            dataBuilder.Should().Be(_serviceResponseBuilder.Data, "because Data property should return the same instance as parent");
        }

        [Fact]
        public void Meta_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var metaBuilder = _linksBuilder.Meta;

            // Assert
            metaBuilder.Should().NotBeNull("because Meta property should return a valid MetaBuilder instance");
            metaBuilder.Should().Be(_serviceResponseBuilder.Meta, "because Meta property should return the same instance as parent");
        }

        #endregion

        #region BuildResponse Method Tests

        [Fact]
        public void BuildResponse_WhenCalledWithoutAction_ShouldReturnCorrectApiServiceResponse()
        {
            // Arrange
            _linksBuilder.AddLink("https://api.example.com/users/123", "self", "User Details");

            // Act
            var response = _linksBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid ApiServiceResponse");
            response.ApiResponse.Should().NotBeNull("because ApiServiceResponse should have an ApiResponse");
            response.ApiResponse.Links.Should().HaveCount(1, "because link should be preserved in the final response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because successful response should have Ok status");
        }

        [Fact]
        public void BuildResponse_WhenCalledWithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            _linksBuilder.AddLink("https://api.example.com/test", "test", "Test Link");
            var actionCalled = false;
            ApiServiceResponse<TestBuilderModel>? capturedResponse = null;

            // Act
            var response = _linksBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
            response.ApiResponse.Links.Should().HaveCount(1, "because link should be preserved in the final response");
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Theory]
        [InlineData("https://api.example.com/users")]
        [InlineData("https://example.com/resource/123")]
        [InlineData("/relative/path")]
        [InlineData("mailto:test@example.com")]
        public void AddLink_WithDifferentHrefFormats_ShouldAcceptValidUrls(string href)
        {
            // Act
            var result = _linksBuilder.AddLink(href);

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0].Href.Should().Be(href, "because href should be accepted regardless of format");
        }

        [Theory]
        [InlineData("self")]
        [InlineData("edit")]
        [InlineData("delete")]
        [InlineData("next")]
        [InlineData("prev")]
        [InlineData("first")]
        [InlineData("last")]
        public void AddLink_WithCommonRelValues_ShouldSetCorrectly(string rel)
        {
            // Act
            var result = _linksBuilder.AddLink("https://api.example.com/test", rel);

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0].Rel.Should().Be(rel, "because rel value should be set correctly");
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("PATCH")]
        [InlineData("DELETE")]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public void AddLink_WithDifferentHttpMethods_ShouldSetCorrectly(string method)
        {
            // Act
            var result = _linksBuilder.AddLink("https://api.example.com/test", "test", "Test", method);

            // Assert
            result.Should().Be(_linksBuilder, "because AddLink should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0].Method.Should().Be(method, "because HTTP method should be set correctly");
        }

        [Fact]
        public void FluentAPI_AddMultipleLinksWithDifferentProperties_ShouldWorkCorrectly()
        {
            // Act
            var result = _linksBuilder
                .AddLink("https://api.example.com/users/123", "self", "User Details")
                .AddLink("https://api.example.com/users/123/edit", "edit")
                .AddLink("https://api.example.com/users/123", "delete", null, "DELETE");

            // Assert
            result.Should().Be(_linksBuilder, "because fluent API should return the same instance");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links!.Should().HaveCount(3, "because three links should be added");
            
            // Verify first link
            var firstLink = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![0];
            firstLink.Href.Should().Be("https://api.example.com/users/123", "because first link href should be correct");
            firstLink.Rel.Should().Be("self", "because first link rel should be correct");
            firstLink.Title.Should().Be("User Details", "because first link title should be correct");
            firstLink.Method.Should().Be("GET", "because first link should have default method");

            // Verify third link
            var thirdLink = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links![2];
            thirdLink.Method.Should().Be("DELETE", "because third link method should be set");
            thirdLink.Title.Should().BeNull("because third link title was set to null");
        }

        [Fact]
        public void LinksBuilder_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where T : class, new()
            
            // Arrange & Act
            var testModelBuilder = new LinksBuilder<TestBuilderModel>(_serviceResponseBuilder);
            var listBuilder = new LinksBuilder<List<string>>(new ServiceResponseBuilder<List<string>>());
            var dictBuilder = new LinksBuilder<Dictionary<string, object>>(new ServiceResponseBuilder<Dictionary<string, object>>());

            // Assert
            testModelBuilder.Should().NotBeNull("because TestBuilderModel is a valid reference type with parameterless constructor");
            listBuilder.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictBuilder.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        #endregion
    }
}