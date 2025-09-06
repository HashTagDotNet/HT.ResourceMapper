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
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/MetaBuilder")]
    public class MetaBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestBuilderModel> _serviceResponseBuilder;
        private readonly MetaBuilder<TestBuilderModel> _metaBuilder;

        public MetaBuilderOfTTests()
        {
            _serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();
            _metaBuilder = new MetaBuilder<TestBuilderModel>(_serviceResponseBuilder);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalledWithValidParent_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();

            // Act
            var metaBuilder = new MetaBuilder<TestBuilderModel>(serviceResponseBuilder);

            // Assert
            metaBuilder.Should().NotBeNull("because constructor should create a valid instance");
        }

        [Fact]
        public void Constructor_WhenCalledWithNullParent_ShouldAllowNullButFailOnAccess()
        {
            // Arrange & Act
            var metaBuilder = new MetaBuilder<TestBuilderModel>(null!);

            // Assert
            metaBuilder.Should().NotBeNull("because constructor allows null parent");
            
            // But accessing properties should throw NullReferenceException
            Action accessAction = () => _ = metaBuilder.Http;
            accessAction.Should().Throw<NullReferenceException>("because null parent will cause NullReferenceException when accessing properties");
        }

        #endregion

        #region Set Method Tests

        [Fact]
        public void Set_WithValidAction_ShouldExecuteActionOnMetaData()
        {
            // Arrange
            var actionCalled = false;
            MetaData? capturedMetaData = null;

            // Act
            var result = _metaBuilder.Set(meta =>
            {
                actionCalled = true;
                capturedMetaData = meta;
                meta.ResponseId = "custom-id";
                meta.CallStatus = CallStatusCode.Ok;
            });

            // Assert
            result.Should().Be(_metaBuilder, "because Set should return the same instance for fluent API");
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedMetaData.Should().NotBeNull("because metadata should be passed to action");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.ResponseId.Should().Be("custom-id", "because response ID should be set by action");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because call status should be set by action");
        }

        [Fact]
        public void Set_WhenMetaDataIsNull_ShouldReturnWithoutExecutingAction()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData = null!;
            var actionCalled = false;

            // Act
            var result = _metaBuilder.Set(_ =>
            {
                actionCalled = true;
            });

            // Assert
            result.Should().Be(_metaBuilder, "because Set should return the same instance for fluent API");
            actionCalled.Should().BeFalse("because action should not be executed when metadata is null");
        }

        #endregion

        #region AddTag Method Tests

        [Fact]
        public void AddTag_WithValidKeyAndValue_ShouldAddTagToMetaData()
        {
            // Arrange
            const string key = "environment";
            const string value = "production";

            // Act
            var result = _metaBuilder.AddTag(key, value);

            // Assert
            result.Should().Be(_metaBuilder, "because AddTag should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags.Should().NotBeNull("because tags collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags!.Should().ContainKey(key, "because tag key should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags![key].Should().Be(value, "because tag value should be set correctly");
        }

        [Fact]
        public void AddTag_WithNullOrEmptyKey_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action actNull = () => _metaBuilder.AddTag(null!, "value");
            Action actEmpty = () => _metaBuilder.AddTag("", "value");
            Action actWhitespace = () => _metaBuilder.AddTag("   ", "value");

            actNull.Should().Throw<ArgumentNullException>("because null key should not be allowed");
            actEmpty.Should().Throw<ArgumentException>("because empty key should not be allowed"); 
            actWhitespace.Should().Throw<ArgumentException>("because whitespace key should not be allowed");
        }

        [Fact]
        public void AddTag_CalledMultipleTimes_ShouldAddMultipleTags()
        {
            // Act
            _metaBuilder
                .AddTag("environment", "production")
                .AddTag("version", "1.0.0")
                .AddTag("region", "us-west-2");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags.Should().HaveCount(3, "because three tags should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags!["environment"].Should().Be("production", "because environment tag should be set");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags!["version"].Should().Be("1.0.0", "because version tag should be set");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags!["region"].Should().Be("us-west-2", "because region tag should be set");
        }

        [Fact]
        public void AddTag_WithDuplicateKey_ShouldOverrideExistingValue()
        {
            // Arrange
            _metaBuilder.AddTag("key", "original-value");

            // Act
            var result = _metaBuilder.AddTag("key", "new-value");

            // Assert
            result.Should().Be(_metaBuilder, "because AddTag should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags!.Should().HaveCount(1, "because duplicate key should replace existing value");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Tags!["key"].Should().Be("new-value", "because new value should override original value");
        }

        #endregion

        #region SetStatus Method Tests

        [Fact]
        public void SetStatus_WithValidCallStatusCode_ShouldSetCallStatus()
        {
            // Arrange
            const CallStatusCode status = CallStatusCode.Ok;

            // Act
            var result = _metaBuilder.SetStatus(status);

            // Assert
            result.Should().Be(_metaBuilder, "because SetStatus should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.CallStatus.Should().Be(status, "because call status should be set correctly");
        }

        [Fact]
        public void SetStatus_WithNullStatus_ShouldSetNullCallStatus()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.CallStatus = CallStatusCode.Ok;

            // Act
            var result = _metaBuilder.SetStatus(null);

            // Assert
            result.Should().Be(_metaBuilder, "because SetStatus should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.CallStatus.Should().BeNull("because call status should be set to null");
        }

        [Fact]
        public void SetStatus_WhenMetaDataIsNull_ShouldReturnWithoutSettingStatus()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData = null!;

            // Act
            var result = _metaBuilder.SetStatus(CallStatusCode.Ok);

            // Assert
            result.Should().Be(_metaBuilder, "because SetStatus should return the same instance for fluent API");
        }

        #endregion

        #region AddMessage Method Tests

        [Fact]
        public void AddMessage_WithValidAction_ShouldAddMessageToMetaData()
        {
            // Arrange
            var actionCalled = false;
            Message? capturedMessage = null;

            // Act
            var result = _metaBuilder.AddMessage(msg =>
            {
                actionCalled = true;
                capturedMessage = msg;
                msg.Title = "Information";
                msg.Details = "Processing completed successfully";
                msg.SeverityCode = MessageSeverity.Info;
            });

            // Assert
            result.Should().Be(_metaBuilder, "because AddMessage should return the same instance for fluent API");
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedMessage.Should().NotBeNull("because message should be captured in action");
            
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages.Should().NotBeNull("because messages collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages!.Should().HaveCount(1, "because one message should be added");
            var addedMessage = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages![0];
            addedMessage.Title.Should().Be("Information", "because title should be set by action");
            addedMessage.Details.Should().Be("Processing completed successfully", "because details should be set by action");
            addedMessage.SeverityCode.Should().Be(MessageSeverity.Info, "because severity should be set by action");
        }

        [Fact]
        public void AddMessage_WhenMetaDataIsNull_ShouldReturnWithoutAddingMessage()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData = null!;
            var actionCalled = false;

            // Act
            var result = _metaBuilder.AddMessage(msg =>
            {
                actionCalled = true;
            });

            // Assert
            result.Should().Be(_metaBuilder, "because AddMessage should return the same instance for fluent API");
            actionCalled.Should().BeFalse("because action should not be executed when metadata is null");
        }

        [Fact]
        public void AddMessage_CalledMultipleTimes_ShouldAddMultipleMessages()
        {
            // Act
            _metaBuilder
                .AddMessage(msg => msg.Title = "First Message")
                .AddMessage(msg => msg.Title = "Second Message")
                .AddMessage(msg => msg.Title = "Third Message");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages!.Should().HaveCount(3, "because three messages should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages![0].Title.Should().Be("First Message", "because first message should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages![1].Title.Should().Be("Second Message", "because second message should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Messages![2].Title.Should().Be("Third Message", "because third message should be preserved");
        }

        #endregion

        #region SetId Method Tests

        [Fact]
        public void SetId_WithValidResponseId_ShouldSetResponseId()
        {
            // Arrange
            const string responseId = "custom-response-id";

            // Act
            var result = _metaBuilder.SetId(responseId);

            // Assert
            result.Should().Be(_metaBuilder, "because SetId should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.ResponseId.Should().Be(responseId, "because response ID should be set correctly");
        }

        [Fact]
        public void SetId_WithNullOrEmptyResponseId_ShouldGenerateRandomId()
        {
            // Act
            var result1 = _metaBuilder.SetId(null);
            var id1 = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.ResponseId;
            
            var result2 = _metaBuilder.SetId("");
            var id2 = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.ResponseId;

            // Assert
            result1.Should().Be(_metaBuilder, "because SetId should return the same instance for fluent API");
            result2.Should().Be(_metaBuilder, "because SetId should return the same instance for fluent API");
            id1.Should().NotBeNullOrEmpty("because null response ID should generate random ID");
            id1.Should().HaveLength(8, "because generated ID should be 8 characters");
            id2.Should().NotBeNullOrEmpty("because empty response ID should generate random ID");
            id2.Should().HaveLength(8, "because generated ID should be 8 characters");
            id1.Should().NotBe(id2, "because consecutive calls should generate different IDs");
        }

        [Fact]
        public void SetId_WhenMetaDataIsNull_ShouldReturnWithoutSettingId()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData = null!;

            // Act
            var result = _metaBuilder.SetId("test-id");

            // Assert
            result.Should().Be(_metaBuilder, "because SetId should return the same instance for fluent API");
        }

        #endregion

        #region SetTimestamp Method Tests

        [Fact]
        public void SetTimestamp_WithValidDateTime_ShouldSetFormattedTimestamp()
        {
            // Arrange
            var timestamp = new DateTime(2023, 12, 25, 14, 30, 45, 123, DateTimeKind.Utc);

            // Act
            var result = _metaBuilder.SetTimestamp(timestamp);

            // Assert
            result.Should().Be(_metaBuilder, "because SetTimestamp should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.Timestamp.Should().Be("2023-12-25T14:30:45.12Z", "because timestamp should be formatted correctly");
        }

        [Fact]
        public void SetTimestamp_WhenMetaDataIsNull_ShouldReturnWithoutSettingTimestamp()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData = null!;
            var timestamp = DateTime.UtcNow;

            // Act
            var result = _metaBuilder.SetTimestamp(timestamp);

            // Assert
            result.Should().Be(_metaBuilder, "because SetTimestamp should return the same instance for fluent API");
        }

        #endregion

        #region Builder Navigation Properties Tests

        [Fact]
        public void Http_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var httpBuilder = _metaBuilder.Http;

            // Assert
            httpBuilder.Should().NotBeNull("because Http property should return a valid HttpApiResponseBuilder instance");
            httpBuilder.Should().Be(_serviceResponseBuilder.Http, "because Http property should return the same instance as parent");
        }

        [Fact]
        public void Validation_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var validationBuilder = _metaBuilder.Validation;

            // Assert
            validationBuilder.Should().NotBeNull("because Validation property should return a valid ValidationBuilder instance");
            validationBuilder.Should().Be(_serviceResponseBuilder.Validation, "because Validation property should return the same instance as parent");
        }

        [Fact]
        public void Errors_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var errorBuilder = _metaBuilder.Errors;

            // Assert
            errorBuilder.Should().NotBeNull("because Errors property should return a valid ErrorBuilder instance");
            errorBuilder.Should().Be(_serviceResponseBuilder.Errors, "because Errors property should return the same instance as parent");
        }

        [Fact]
        public void Links_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var linksBuilder = _metaBuilder.Links;

            // Assert
            linksBuilder.Should().NotBeNull("because Links property should return a valid LinksBuilder instance");
            linksBuilder.Should().Be(_serviceResponseBuilder.Links, "because Links property should return the same instance as parent");
        }

        [Fact]
        public void Data_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var dataBuilder = _metaBuilder.Data;

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
            _metaBuilder.SetStatus(CallStatusCode.Ok);

            // Act
            var response = _metaBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid ApiServiceResponse");
            response.ApiResponse.Should().NotBeNull("because ApiServiceResponse should have an ApiResponse");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because call status should be preserved in the final response");
        }

        [Fact]
        public void BuildResponse_WhenCalledWithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            _metaBuilder.AddTag("test", "value");
            var actionCalled = false;
            ApiServiceResponse<TestBuilderModel>? capturedResponse = null;

            // Act
            var response = _metaBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
            response.ApiResponse.MetaData.Tags!["test"].Should().Be("value", "because tag should be preserved in the final response");
        }

        #endregion

        #region Integration Tests

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.Unauthenticated)]
        public void SetStatus_WithDifferentCallStatusCodes_ShouldSetCorrectly(CallStatusCode statusCode)
        {
            // Act
            var result = _metaBuilder.SetStatus(statusCode);

            // Assert
            result.Should().Be(_metaBuilder, "because SetStatus should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.CallStatus.Should().Be(statusCode, "because call status should be set correctly");
        }

        [Fact]
        public void FluentAPI_ComplexMetaDataConfiguration_ShouldWorkCorrectly()
        {
            // Arrange
            var timestamp = new DateTime(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

            // Act
            var result = _metaBuilder
                .SetId("response-123")
                .SetStatus(CallStatusCode.Ok)
                .SetTimestamp(timestamp)
                .AddTag("environment", "test")
                .AddTag("version", "1.0")
                .AddMessage(msg => 
                {
                    msg.Title = "Success";
                    msg.Details = "Operation completed";
                });

            // Assert
            result.Should().Be(_metaBuilder, "because fluent API should return the same instance");
            var metaData = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData;
            metaData.ResponseId.Should().Be("response-123", "because response ID should be set");
            metaData.CallStatus.Should().Be(CallStatusCode.Ok, "because call status should be set");
            metaData.Timestamp.Should().Be("2023-01-01T12:00:00.00Z", "because timestamp should be formatted correctly");
            metaData.Tags.Should().HaveCount(2, "because two tags should be added");
            metaData.Messages.Should().HaveCount(1, "because one message should be added");
            metaData.Messages![0].Title.Should().Be("Success", "because message title should be set");
        }

        [Fact]
        public void MetaBuilder_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where TData : class, new()
            
            // Arrange & Act
            var testModelBuilder = new MetaBuilder<TestBuilderModel>(_serviceResponseBuilder);
            var listBuilder = new MetaBuilder<List<string>>(new ServiceResponseBuilder<List<string>>());
            var dictBuilder = new MetaBuilder<Dictionary<string, object>>(new ServiceResponseBuilder<Dictionary<string, object>>());

            // Assert
            testModelBuilder.Should().NotBeNull("because TestBuilderModel is a valid reference type with parameterless constructor");
            listBuilder.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictBuilder.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        [Fact]
        public void FixMetaEnumerations_WithValidMetaData_ShouldSetEnumerationIds()
        {
            // This test verifies that the BuildResponse method properly processes metadata
            // We test this indirectly through the BuildResponse method since FixMetaEnumerations is internal
            
            // Arrange
            _metaBuilder.SetStatus(CallStatusCode.Ok);

            // Act
            var response = _metaBuilder.BuildResponse();

            // Assert
            response.ApiResponse.MetaData.CallStatusId.Should().Be((int)CallStatusCode.Ok, "because CallStatusId should be set from CallStatus during build");
            response.ApiResponse.MetaData.Flags.Should().NotBeNull("because Flags should be initialized during build");
            response.ApiResponse.MetaData.Flags!.ResultCategory.Should().NotBeNull("because ResultCategory should be set based on CallStatus during build");
            response.ApiResponse.MetaData.Flags.IsRetryable.Should().NotBeNull("because IsRetryable should be set based on CallStatus during build");
        }

        #endregion
    }
}