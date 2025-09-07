// ReSharper disable InconsistentNaming
using HT.Api.Service.Contracts.BuildersOfT;
using System.Net;

namespace HT.Api.Service.Contracts.Tests.BuildersOfTTests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Service")]
    [Trait("Category", "HT/Api/Service/Contracts")]
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT")]
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/HttpApiResponseBuilder")]
    public class HttpApiResponseBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestBuilderModel> _serviceResponseBuilder;
        private readonly HttpApiResponseBuilder<TestBuilderModel> _httpBuilder;

        public HttpApiResponseBuilderOfTTests()
        {
            _serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();
            _httpBuilder = new HttpApiResponseBuilder<TestBuilderModel>(_serviceResponseBuilder);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalledWithValidParent_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();

            // Act
            var httpBuilder = new HttpApiResponseBuilder<TestBuilderModel>(serviceResponseBuilder);

            // Assert
            httpBuilder.Should().NotBeNull("because constructor should create a valid instance");
        }

        #endregion

        #region AddHeader Method Tests

        [Fact]
        public void AddHeader_WithValidKeyAndValue_ShouldAddHeaderToHttpResponse()
        {
            // Arrange
            const string key = "X-Custom-Header";
            const string value = "custom-value";

            // Act
            var result = _httpBuilder.AddHeader(key, value);

            // Assert
            result.Should().Be(_httpBuilder, "because AddHeader should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Should().NotBeNull("because HttpResponse should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.Headers.Should().NotBeNull("because Headers collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers!.Should().HaveCount(1, "because one header should be added");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![0].Key.Should().Be(key, "because header key should be set correctly");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![0].Value.Should().Be(value, "because header value should be set correctly");
        }

        [Fact]
        public void AddHeader_WithNullOrEmptyKey_ShouldThrowArgumentException()
        {
            // Act & Assert
            Action actNull = () => _httpBuilder.AddHeader(null!, "value");
            Action actEmpty = () => _httpBuilder.AddHeader("", "value");
            Action actWhitespace = () => _httpBuilder.AddHeader("   ", "value");

            actNull.Should().Throw<ArgumentException>("because null key should not be allowed");
            actEmpty.Should().Throw<ArgumentException>("because empty key should not beallowed");
            actWhitespace.Should().Throw<ArgumentException>("because whitespace key should not be allowed");
        }

        [Fact]
        public void AddHeader_CalledMultipleTimes_ShouldAddMultipleHeaders()
        {
            // Act
            _httpBuilder
                .AddHeader("Header1", "Value1")
                .AddHeader("Header2", "Value2")
                .AddHeader("Header3", "Value3");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.Headers.Should().HaveCount(3, "because three headers should be added");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![0].Key.Should().Be("Header1", "because first header should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![1].Key.Should().Be("Header2", "because second header should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![2].Key.Should().Be("Header3", "because third header should be preserved");
        }

        #endregion

        #region AppendHeader Method Tests

        [Fact]
        public void AppendHeader_WhenHeaderDoesNotExist_ShouldAddNewHeader()
        {
            // Arrange
            const string key = "Content-Type";
            const string value = "application/json";

            // Act
            var result = _httpBuilder.AppendHeader(key, value);

            // Assert
            result.Should().Be(_httpBuilder, "because AppendHeader should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.Headers.Should().HaveCount(1, "because one header should be added");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![0].Value.Should().Be(value, "because header value should be set correctly");
        }

        [Fact]
        public void AppendHeader_WhenHeaderExists_ShouldAppendToExistingHeader()
        {
            // Arrange
            const string key = "Accept";
            _httpBuilder.AddHeader(key, "application/json");

            // Act
            var result = _httpBuilder.AppendHeader(key, "application/xml");

            // Assert
            result.Should().Be(_httpBuilder, "because AppendHeader should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.Headers.Should().HaveCount(1, "because header should be merged, not duplicated");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![0].Value.Should().Be("application/json,application/xml", "because values should be comma-separated");
        }

        [Fact]
        public void AppendHeader_WithNullOrEmptyKeyOrValue_ShouldThrowArgumentException()
        {
            // Act & Assert
            Action actNullKey = () => _httpBuilder.AppendHeader(null!, "value");
            Action actEmptyKey = () => _httpBuilder.AppendHeader("", "value");
            Action actNullValue = () => _httpBuilder.AppendHeader("key", null!);
            Action actEmptyValue = () => _httpBuilder.AppendHeader("key", "");

            actNullKey.Should().Throw<ArgumentException>("because null key should not be allowed");
            actEmptyKey.Should().Throw<ArgumentException>("because empty key should not be allowed");
            actNullValue.Should().Throw<ArgumentException>("because null value should not be allowed");
            actEmptyValue.Should().Throw<ArgumentException>("because empty value should not be allowed");
        }

        #endregion

        #region RemoveHeader Method Tests

        [Fact]
        public void RemoveHeader_WhenHeaderExists_ShouldRemoveHeader()
        {
            // Arrange
            _httpBuilder.AddHeader("X-Test-Header", "test-value");

            // Act
            var result = _httpBuilder.RemoveHeader("X-Test-Header");

            // Assert
            result.Should().Be(_httpBuilder, "because RemoveHeader should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Should().BeNull("because HttpResponse should be null when all properties are null");
        }

        [Fact]
        public void RemoveHeader_WhenHeaderDoesNotExist_ShouldHandleGracefully()
        {
            // Act
            var result = _httpBuilder.RemoveHeader("NonExistent-Header");

            // Assert
            result.Should().Be(_httpBuilder, "because RemoveHeader should return the same instance for fluent API");
        }

        [Fact]
        public void RemoveHeader_CaseInsensitive_ShouldRemoveHeader()
        {
            // Arrange
            _httpBuilder.AddHeader("Content-Type", "application/json");

            // Act
            var result = _httpBuilder.RemoveHeader("content-type");

            // Assert
            result.Should().Be(_httpBuilder, "because RemoveHeader should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Should().BeNull("because HttpResponse should be null when all properties are null");
        }

        [Fact]
        public void RemoveHeader_WithNullOrEmptyKey_ShouldThrowArgumentException()
        {
            // Act & Assert
            Action actNull = () => _httpBuilder.RemoveHeader(null!);
            Action actEmpty = () => _httpBuilder.RemoveHeader("");

            actNull.Should().Throw<ArgumentException>("because null key should not be allowed");
            actEmpty.Should().Throw<ArgumentException>("because empty key should not be allowed");
        }

        #endregion

        #region GetHeaderValue Method Tests

        [Fact]
        public void GetHeaderValue_WhenHeaderExists_ShouldReturnValue()
        {
            // Arrange
            const string key = "Authorization";
            const string value = "Bearer token123";
            _httpBuilder.AddHeader(key, value);

            // Act
            var result = _httpBuilder.GetHeaderValue(key);

            // Assert
            result.Should().Be(value, "because the header value should be returned");
        }

        [Fact]
        public void GetHeaderValue_WhenHeaderDoesNotExist_ShouldReturnNull()
        {
            // Act
            var result = _httpBuilder.GetHeaderValue("NonExistent-Header");

            // Assert
            result.Should().BeNull("because non-existent header should return null");
        }

        [Fact]
        public void GetHeaderValue_CaseInsensitive_ShouldReturnValue()
        {
            // Arrange
            _httpBuilder.AddHeader("Content-Type", "application/json");

            // Act
            var result = _httpBuilder.GetHeaderValue("content-type");

            // Assert
            result.Should().Be("application/json", "because header lookup should be case-insensitive");
        }

        #endregion

        #region HasHeader Method Tests

        [Fact]
        public void HasHeader_WhenHeaderExists_ShouldReturnTrue()
        {
            // Arrange
            _httpBuilder.AddHeader("X-Custom", "value");

            // Act
            var result = _httpBuilder.HasHeader("X-Custom");

            // Assert
            result.Should().BeTrue("because header exists");
        }

        [Fact]
        public void HasHeader_WhenHeaderDoesNotExist_ShouldReturnFalse()
        {
            // Act
            var result = _httpBuilder.HasHeader("NonExistent-Header");

            // Assert
            result.Should().BeFalse("because header does not exist");
        }

        [Fact]
        public void HasHeader_CaseInsensitive_ShouldReturnTrue()
        {
            // Arrange
            _httpBuilder.AddHeader("Content-Type", "application/json");

            // Act
            var result = _httpBuilder.HasHeader("content-type");

            // Assert
            result.Should().BeTrue("because header check should be case-insensitive");
        }

        #endregion

        #region SetStatusCode Method Tests

        [Fact]
        public void SetStatusCode_WithValidStatusCode_ShouldSetHttpStatusCode()
        {
            // Arrange
            const HttpStatusCode statusCode = HttpStatusCode.Created;

            // Act
            var result = _httpBuilder.SetStatusCode(statusCode);

            // Assert
            result.Should().Be(_httpBuilder, "because SetStatusCode should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.HttpStatusCode.Should().Be(statusCode, "because HTTP status code should be set correctly");
        }

        [Fact]
        public void SetStatusCode_WithStatusCodeAndMessage_ShouldSetBothValues()
        {
            // Arrange
            const HttpStatusCode statusCode = HttpStatusCode.BadRequest;
            const string statusMessage = "Custom Bad Request";

            // Act
            var result = _httpBuilder.SetStatusCode(statusCode, statusMessage);

            // Assert
            result.Should().Be(_httpBuilder, "because SetStatusCode should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.HttpStatusCode.Should().Be(statusCode, "because HTTP status code should be set correctly");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.HttpStatusMessage.Should().Be(statusMessage, "because HTTP status message should be set correctly");
        }

        [Fact]
        public void SetStatusCode_WithNullOrEmptyMessage_ShouldThrowArgumentException()
        {
            // Act & Assert
            Action actNull = () => _httpBuilder.SetStatusCode(HttpStatusCode.OK, null!);
            Action actEmpty = () => _httpBuilder.SetStatusCode(HttpStatusCode.OK, "");

            actNull.Should().Throw<ArgumentException>("because null status message should not be allowed");
            actEmpty.Should().Throw<ArgumentException>("because empty status message should not be allowed");
        }

        #endregion

        #region SetStatusMessage Method Tests

        [Fact]
        public void SetStatusMessage_WithValidMessage_ShouldSetHttpStatusMessage()
        {
            // Arrange
            const string statusMessage = "Custom Status Message";

            // Act
            var result = _httpBuilder.SetStatusMessage(statusMessage);

            // Assert
            result.Should().Be(_httpBuilder, "because SetStatusMessage should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.HttpStatusMessage.Should().Be(statusMessage, "because HTTP status message should be set correctly");
        }

        [Fact]
        public void SetStatusMessage_WithNullOrEmptyMessage_ShouldThrowArgumentException()
        {
            // Act & Assert
            Action actNull = () => _httpBuilder.SetStatusMessage(null!);
            Action actEmpty = () => _httpBuilder.SetStatusMessage("");

            actNull.Should().Throw<ArgumentException>("because null status message should not be allowed");
            actEmpty.Should().Throw<ArgumentException>("because empty status message should not be allowed");
        }

        #endregion

        #region Builder Navigation Properties Tests

        [Fact]
        public void Http_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var httpBuilder = _httpBuilder.Http;

            // Assert
            httpBuilder.Should().NotBeNull("because Http property should return a valid HttpApiResponseBuilder instance");
            ReferenceEquals(httpBuilder, _serviceResponseBuilder.Http).Should().BeTrue("because Http property should return the parent's Http builder instance");
        }

        [Fact]
        public void Validation_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var validationBuilder = _httpBuilder.Validation;

            // Assert
            validationBuilder.Should().NotBeNull("because Validation property should return a valid ValidationBuilder instance");
            validationBuilder.Should().Be(_serviceResponseBuilder.Validation, "because Validation property should return the same instance as parent");
        }

        [Fact]
        public void Errors_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var errorBuilder = _httpBuilder.Errors;

            // Assert
            errorBuilder.Should().NotBeNull("because Errors property should return a valid ErrorBuilder instance");
            errorBuilder.Should().Be(_serviceResponseBuilder.Errors, "because Errors property should return the same instance as parent");
        }

        [Fact]
        public void Links_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var linksBuilder = _httpBuilder.Links;

            // Assert
            linksBuilder.Should().NotBeNull("because Links property should return a valid LinksBuilder instance");
            linksBuilder.Should().Be(_serviceResponseBuilder.Links, "because Links property should return the same instance as parent");
        }

        [Fact]
        public void Meta_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var metaBuilder = _httpBuilder.Meta;

            // Assert
            metaBuilder.Should().NotBeNull("because Meta property should return a valid MetaBuilder instance");
            metaBuilder.Should().Be(_serviceResponseBuilder.Meta, "because Meta property should return the same instance as parent");
        }

        [Fact]
        public void Data_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var dataBuilder = _httpBuilder.Data;

            // Assert
            dataBuilder.Should().NotBeNull("because Data property should return a valid DataBuilder instance");
            dataBuilder.Should().Be(_serviceResponseBuilder.Data, "because Data property should return the same instance as parent");
        }

        #endregion

        #region BuildResponse Method Tests

        [Fact]
        public void BuildResponse_WhenCalledWithoutAction_ShouldReturnCorrectApiServiceResponse()
        {
            // Arrange
            _httpBuilder.SetStatusCode(HttpStatusCode.OK);

            // Act
            var response = _httpBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid ApiServiceResponse");
            response.HttpResponse.Should().NotBeNull("because HttpResponse should be initialized");
            response.HttpResponse!.HttpStatusCode.Should().Be(HttpStatusCode.OK, "because HTTP status code should be preserved");
        }

        [Fact]
        public void BuildResponse_WhenCalledWithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            _httpBuilder.SetStatusCode(HttpStatusCode.Accepted);
            var actionCalled = false;
            ApiServiceResponse<TestBuilderModel>? capturedResponse = null;

            // Act
            var response = _httpBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData(HttpStatusCode.OK)]
        [InlineData(HttpStatusCode.Created)]
        [InlineData(HttpStatusCode.BadRequest)]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        public void SetStatusCode_WithDifferentStatusCodes_ShouldSetCorrectly(HttpStatusCode statusCode)
        {
            // Act
            var result = _httpBuilder.SetStatusCode(statusCode);

            // Assert
            result.Should().Be(_httpBuilder, "because SetStatusCode should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.HttpStatusCode.Should().Be(statusCode, "because status code should be set correctly");
        }

        [Fact]
        public void FluentAPI_ComplexHeaderManagement_ShouldWorkCorrectly()
        {
            // Act
            var result = _httpBuilder
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Cache-Control", "no-cache")
                .AppendHeader("Cache-Control", "no-store")
                .SetStatusCode(HttpStatusCode.OK, "Success")
                .RemoveHeader("Cache-Control");

            // Assert
            result.Should().Be(_httpBuilder, "because fluent API should return the same instance");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse!.Headers.Should().HaveCount(1, "because only Content-Type should remain");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.Headers![0].Key.Should().Be("Content-Type", "because Content-Type should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.HttpStatusCode.Should().Be(HttpStatusCode.OK, "because status code should be set");
            _serviceResponseBuilder.BackingServiceResponse.HttpResponse.HttpStatusMessage.Should().Be("Success", "because status message should be set");
        }

        #endregion
    }
}