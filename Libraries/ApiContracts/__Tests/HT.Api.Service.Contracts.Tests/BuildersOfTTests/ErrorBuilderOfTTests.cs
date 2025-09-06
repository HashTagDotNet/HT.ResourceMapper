// ReSharper disable InconsistentNaming
using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts;
using HT.Api.Service.Contracts.BuildersOfT;

namespace HT.Api.Service.Contracts.Tests.BuildersOfTTests
{
    /// <summary>
    /// Test model for builder tests that satisfies the generic constraint
    /// </summary>
    public class TestBuilderModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Service")]
    [Trait("Category", "HT/Api/Service/Contracts")]
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT")]
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/ErrorBuilder")]
    public class ErrorBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestBuilderModel> _serviceResponseBuilder;
        private readonly ErrorBuilder<TestBuilderModel> _errorBuilder;

        public ErrorBuilderOfTTests()
        {
            _serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();
            _errorBuilder = new ErrorBuilder<TestBuilderModel>(_serviceResponseBuilder);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalledWithValidParent_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();

            // Act
            var errorBuilder = new ErrorBuilder<TestBuilderModel>(serviceResponseBuilder);

            // Assert
            errorBuilder.Should().NotBeNull("because constructor should create a valid instance");
        }

        [Fact]
        public void Constructor_WhenCalledWithNullParent_ShouldAllowNullButFailOnAccess()
        {
            // Arrange & Act
            var errorBuilder = new ErrorBuilder<TestBuilderModel>(null!);

            // Assert
            errorBuilder.Should().NotBeNull("because constructor allows null parent");

            // But accessing properties should throw NullReferenceException
            Action accessAction = () => _ = errorBuilder.Http;
            accessAction.Should().Throw<NullReferenceException>("because null parent will cause NullReferenceException when accessing properties");
        }

        #endregion

        #region AddError Method Tests

        [Fact]
        public void AddError_WithCallStatusCodeAndDetails_ShouldAddErrorToApiResponse()
        {
            // Arrange
            var callStatus = CallStatusCode.InvalidArgument;
            const string title = "Test Error";
            const string detail = "Test error details";
            const string code = "TEST001";

            // Act
            var result = _errorBuilder.AddError(callStatus, title, detail, code);

            // Assert
            result.Should().Be(_errorBuilder, "because AddError should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().NotBeNull("because errors collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(1, "because one error should be added");

            var addedError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0];
            addedError.CallStatus.Should().Be(callStatus, "because call status should be set correctly");
            addedError.Title.Should().Be(title, "because title should be set correctly");
            addedError.Details.Should().Be(detail, "because details should be set correctly");
            addedError.MessageCode.Should().Be(code, "because message code should be set correctly");
        }

        [Fact]
        public void AddError_WithNullParameters_ShouldAddErrorwithNullValues()
        {
            // Arrange
            var callStatus = CallStatusCode.Error;

            // Act
            var result = _errorBuilder.AddError(callStatus, null, null, null);

            // Assert
            result.Should().Be(_errorBuilder, "because AddError should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().HaveCount(1, "because one error should be added");

            var addedError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0];
            addedError.CallStatus.Should().Be(callStatus, "because call status should be set correctly");
            addedError.Title.Should().BeNull("because title was passed as null");
            addedError.Details.Should().BeNull("because details was passed as null");
            addedError.MessageCode.Should().BeNull("because message code was passed as null");
        }

        [Fact]
        public void AddError_WithMessageObject_ShouldAddErrorToApiResponse()
        {
            // Arrange
            var message = new Message
            {
                Title = "Custom Error",
                Details = "Custom error details",
                CallStatus = CallStatusCode.NotFound,
                MessageCode = "CUSTOM001"
            };

            // Act
            var result = _errorBuilder.AddError(message);

            // Assert
            result.Should().Be(_errorBuilder, "because AddError should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().HaveCount(1, "because one error should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].Should().Be(message, "because the exact message object should be added");
        }

        [Fact]
        public void AddError_WithNullMessage_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _errorBuilder.AddError((Message)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>("because null message should not be allowed")
                .And.ParamName.Should().Be("error", "because the parameter name should match");
        }

        [Fact]
        public void AddError_WithActionMessage_ShouldAddConfiguredErrorToApiResponse()
        {
            // Arrange
            var actionCalled = false;
            Message? capturedMessage = null;

            // Act
            var result = _errorBuilder.AddError(msg =>
            {
                actionCalled = true;
                capturedMessage = msg;
                msg.Title = "Action Error";
                msg.Details = "Action error details";
                msg.CallStatus = CallStatusCode.InternalError;
            });

            // Assert
            result.Should().Be(_errorBuilder, "because AddError should return the same instance for fluent API");
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedMessage.Should().NotBeNull("because message should be captured in action");

            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().HaveCount(1, "because one error should be added");
            var addedError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0];
            addedError.Title.Should().Be("Action Error", "because title should be set by action");
            addedError.Details.Should().Be("Action error details", "because details should be set by action");
            addedError.CallStatus.Should().Be(CallStatusCode.InternalError, "because call status should be set by action");
        }

        [Fact]
        public void AddError_WithNullAction_ShouldThrowArgumentNullException()
        {
            // Act
            Action act = () => _errorBuilder.AddError((Action<Message>)null!);

            // Assert
            act.Should().Throw<ArgumentNullException>("because null action should not be allowed")
                .And.ParamName.Should().Be("message", "because the parameter name should match");
        }

        [Fact]
        public void AddError_CalledMultipleTimes_ShouldAddMultipleErrors()
        {
            // Act
            _errorBuilder
                .AddError(CallStatusCode.InvalidArgument, "First Error")
                .AddError(CallStatusCode.NotFound, "Second Error")
                .AddError(CallStatusCode.InternalError, "Third Error");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().HaveCount(3, "because three errors should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].Title.Should().Be("First Error", "because first error should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![1].Title.Should().Be("Second Error", "because second error should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![2].Title.Should().Be("Third Error", "because third error should be preserved");
        }

        #endregion

        #region ClearErrors Method Tests

        [Fact]
        public void ClearErrors_WhenErrorsExist_ShouldClearErrorsCollection()
        {
            // Arrange
            _errorBuilder.AddError(CallStatusCode.InvalidArgument, "Test Error");

            // Act
            var result = _errorBuilder.ClearErrors();

            // Assert
            result.Should().Be(_errorBuilder, "because ClearErrors should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().BeNull("because errors collection should be set to null after clearing");
        }

        [Fact]
        public void ClearErrors_WhenNoErrorsExist_ShouldHandleGracefully()
        {
            // Act
            var result = _errorBuilder.ClearErrors();

            // Assert
            result.Should().Be(_errorBuilder, "because ClearErrors should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().BeNull("because errors collection should remain null");
        }

        [Fact]
        public void ClearErrors_AfterClearingCanAddNewErrors_ShouldWork()
        {
            // Arrange
            _errorBuilder.AddError(CallStatusCode.InvalidArgument, "Initial Error");

            // Act
            _errorBuilder.ClearErrors();
            var result = _errorBuilder.AddError(CallStatusCode.NotFound, "New Error");

            // Assert
            result.Should().Be(_errorBuilder, "because fluent API should continue to work");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().HaveCount(1, "because new error should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].Title.Should().Be("New Error", "because new error should replace cleared errors");
        }

        #endregion

        #region Builder Navigation Properties Tests

        [Fact]
        public void Http_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var httpBuilder = _errorBuilder.Http;

            // Assert
            httpBuilder.Should().NotBeNull("because Http property should return a valid HttpApiResponseBuilder instance");
            httpBuilder.Should().Be(_serviceResponseBuilder.Http, "because Http property should return the same instance as parent");
        }

        [Fact]
        public void Validation_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var validationBuilder = _errorBuilder.Validation;

            // Assert
            validationBuilder.Should().NotBeNull("because Validation property should return a valid ValidationBuilder instance");
            validationBuilder.Should().Be(_serviceResponseBuilder.Validation, "because Validation property should return the same instance as parent");
        }

        [Fact]
        public void Data_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var dataBuilder = _errorBuilder.Data;

            // Assert
            dataBuilder.Should().NotBeNull("because Data property should return a valid DataBuilder instance");
            dataBuilder.Should().Be(_serviceResponseBuilder.Data, "because Data property should return the same instance as parent");
        }

        [Fact]
        public void Links_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var linksBuilder = _errorBuilder.Links;

            // Assert
            linksBuilder.Should().NotBeNull("because Links property should return a valid LinksBuilder instance");
            linksBuilder.Should().Be(_serviceResponseBuilder.Links, "because Links property should return the same instance as parent");
        }

        [Fact]
        public void Meta_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var metaBuilder = _errorBuilder.Meta;

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
            _errorBuilder.AddError(CallStatusCode.NotFound, "Resource not found");

            // Act
            var response = _errorBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid ApiServiceResponse");
            response.ApiResponse.Should().NotBeNull("because ApiServiceResponse should have an ApiResponse");
            response.ApiResponse.Errors.Should().HaveCount(1, "because error should be preserved in the final response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.NotFound, "because call status should be set based on first error");
        }

        [Fact]
        public void BuildResponse_WhenCalledWithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            _errorBuilder.AddError(CallStatusCode.InvalidArgument, "Validation failed");
            var actionCalled = false;
            ApiServiceResponse<TestBuilderModel>? capturedResponse = null;

            // Act
            var response = _errorBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
            response.ApiResponse.Errors.Should().HaveCount(1, "because error should be preserved in the final response");
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Theory]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.PermissionDenied)]
        [InlineData(CallStatusCode.Unauthenticated)]
        public void AddError_WithDifferentCallStatusCodes_ShouldSetCorrectly(CallStatusCode statusCode)
        {
            // Act
            var result = _errorBuilder.AddError(statusCode, "Test Error");

            // Assert
            result.Should().Be(_errorBuilder, "because AddError should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].CallStatus.Should().Be(statusCode, "because call status should be set correctly");
        }

        [Fact]
        public void FluentAPI_AddMultipleErrorsThenClear_ShouldWorkCorrectly()
        {
            // Act
            var result = _errorBuilder
                .AddError(CallStatusCode.InvalidArgument, "Error 1")
                .AddError(CallStatusCode.NotFound, "Error 2")
                .ClearErrors();

            // Assert
            result.Should().Be(_errorBuilder, "because fluent API should return the same instance");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().BeNull("because errors should be cleared");
        }

        [Fact]
        public void ErrorBuilder_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where T : class, new()

            // Arrange & Act
            var testModelBuilder = new ErrorBuilder<TestBuilderModel>(_serviceResponseBuilder);
            var listBuilder = new ErrorBuilder<List<string>>(new ServiceResponseBuilder<List<string>>());
            var dictBuilder = new ErrorBuilder<Dictionary<string, object>>(new ServiceResponseBuilder<Dictionary<string, object>>());

            // Assert
            testModelBuilder.Should().NotBeNull("because TestBuilderModel is a valid reference type with parameterless constructor");
            listBuilder.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictBuilder.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        #endregion
    }
}
