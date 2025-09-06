// ReSharper disable InconsistentNaming
using System.Net;
using HT.Api.Client.Contracts;
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
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/ServiceResponseBuilder")]
    public class ApiServiceResponseBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestBuilderModel> _serviceResponseBuilder;

        public ApiServiceResponseBuilderOfTTests()
        {
            _serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalled_ShouldInitializeAllBuilders()
        {
            // Act
            var builder = new ServiceResponseBuilder<TestBuilderModel>();

            // Assert
            builder.Should().NotBeNull("because constructor should create a valid instance");
            builder.Http.Should().NotBeNull("because Http builder should be initialized");
            builder.Data.Should().NotBeNull("because Data builder should be initialized");
            builder.Meta.Should().NotBeNull("because Meta builder should be initialized");
            builder.Validation.Should().NotBeNull("because Validation builder should be initialized");
            builder.Errors.Should().NotBeNull("because Errors builder should be initialized");
            builder.Links.Should().NotBeNull("because Links builder should be initialized");
            builder.BackingServiceResponse.Should().NotBeNull("because backing service response should be initialized");
            builder.BackingServiceResponse.ApiResponse.Should().NotBeNull("because API response should be initialized");
            builder.BackingServiceResponse.ApiResponse.MetaData.Should().NotBeNull("because metadata should be initialized");
        }

        #endregion

        #region BackingServiceResponse Property Tests

        [Fact]
        public void BackingServiceResponse_ShouldReturnSameInstanceAlways()
        {
            // Act
            var backing1 = _serviceResponseBuilder.BackingServiceResponse;
            var backing2 = _serviceResponseBuilder.BackingServiceResponse;

            // Assert
            backing1.Should().Be(backing2, "because BackingServiceResponse should return the same instance");
            backing1.Should().NotBeNull("because backing service response should be initialized");
        }

        #endregion

        #region Builder Property Tests

        [Fact]
        public void Http_ShouldReturnSameInstanceAlways()
        {
            // Act
            var http1 = _serviceResponseBuilder.Http;
            var http2 = _serviceResponseBuilder.Http;

            // Assert
            http1.Should().Be(http2, "because Http should return the same instance");
            http1.Should().NotBeNull("because Http builder should be initialized");
        }

        [Fact]
        public void Data_ShouldReturnSameInstanceAlways()
        {
            // Act
            var data1 = _serviceResponseBuilder.Data;
            var data2 = _serviceResponseBuilder.Data;

            // Assert
            data1.Should().Be(data2, "because Data should return the same instance");
            data1.Should().NotBeNull("because Data builder should be initialized");
        }

        [Fact]
        public void Meta_ShouldReturnSameInstanceAlways()
        {
            // Act
            var meta1 = _serviceResponseBuilder.Meta;
            var meta2 = _serviceResponseBuilder.Meta;

            // Assert
            meta1.Should().Be(meta2, "because Meta should return the same instance");
            meta1.Should().NotBeNull("because Meta builder should be initialized");
        }

        [Fact]
        public void Validation_ShouldReturnSameInstanceAlways()
        {
            // Act
            var validation1 = _serviceResponseBuilder.Validation;
            var validation2 = _serviceResponseBuilder.Validation;

            // Assert
            validation1.Should().Be(validation2, "because Validation should return the same instance");
            validation1.Should().NotBeNull("because Validation builder should be initialized");
        }

        [Fact]
        public void Errors_ShouldReturnSameInstanceAlways()
        {
            // Act
            var errors1 = _serviceResponseBuilder.Errors;
            var errors2 = _serviceResponseBuilder.Errors;

            // Assert
            errors1.Should().Be(errors2, "because Errors should return the same instance");
            errors1.Should().NotBeNull("because Errors builder should be initialized");
        }

        [Fact]
        public void Links_ShouldReturnSameInstanceAlways()
        {
            // Act
            var links1 = _serviceResponseBuilder.Links;
            var links2 = _serviceResponseBuilder.Links;

            // Assert
            links1.Should().Be(links2, "because Links should return the same instance");
            links1.Should().NotBeNull("because Links builder should be initialized");
        }

        #endregion

        #region BuildResponse Method Tests (Without Action)

        [Fact]
        public void BuildResponse_WithSuccessfulData_ShouldSetOkCallStatus()
        {
            // Arrange
            var testData = new TestBuilderModel { Name = "Test", Value = 123 };
            _serviceResponseBuilder.Data.Set(testData);

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because successful response should have Ok status");
            response.ApiResponse.Data.Should().Be(testData, "because data should be preserved");
            response.HttpResponse!.HttpStatusCode.Should().Be(HttpStatusCode.OK, "because HTTP status should be derived from call status");
        }

        [Fact]
        public void BuildResponse_WithErrors_ShouldSetErrorCallStatus()
        {
            // Arrange
            _serviceResponseBuilder.Errors.AddError(CallStatusCode.NotFound, "Resource not found");

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.NotFound, "because call status should be set based on first error");
            response.ApiResponse.Errors.Should().HaveCount(1, "because error should be preserved");
            response.HttpResponse!.HttpStatusCode.Should().Be(HttpStatusCode.NotFound, "because HTTP status should be derived from call status");
        }

        [Fact]
        public void BuildResponse_WithMultipleErrors_ShouldSetCallStatusFromFirstError()
        {
            // Arrange
            _serviceResponseBuilder.Errors
                .AddError(CallStatusCode.NotFound, "First Error")
                .AddError(CallStatusCode.InvalidArgument, "Second Error")
                .AddError(CallStatusCode.InternalError, "Third Error");

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.NotFound, "because call status should be set based on first error");
            response.ApiResponse.Errors.Should().HaveCount(3, "because all errors should be preserved");
        }

        [Fact]
        public void BuildResponse_WithEmptyErrorsCollection_ShouldRemoveErrorsCollection()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors = new List<Message>();

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.ApiResponse.Errors.Should().BeNull("because empty errors collection should be removed");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because no errors should result in Ok status");
        }

        [Fact]
        public void BuildResponse_WithEmptyLinksCollection_ShouldRemoveLinksCollection()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Links = new List<Link>();

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.ApiResponse.Links.Should().BeNull("because empty links collection should be removed");
        }

        [Fact]
        public void BuildResponse_WithMetadataPresetCallStatus_ShouldNotOverrideCallStatus()
        {
            // Arrange
            _serviceResponseBuilder.Meta.SetStatus(CallStatusCode.Ok);
            _serviceResponseBuilder.Errors.AddError(CallStatusCode.NotFound, "Error");

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because preset call status should not be overridden");
        }

        #endregion

        #region BuildResponse Method Tests (With Action)

        [Fact]
        public void BuildResponse_WithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            var actionCalled = false;
            ApiServiceResponse<TestBuilderModel>? capturedResponse = null;
            _serviceResponseBuilder.Data.Set(new TestBuilderModel { Name = "Test" });

            // Act
            var response = _serviceResponseBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
                r.ApiResponse.MetaData.ResponseId = "custom-id";
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
            response.ApiResponse.MetaData.ResponseId.Should().Be("custom-id", "because action should be able to modify the response");
        }

        [Fact]
        public void BuildResponse_WithNullAction_ShouldNotThrowException()
        {
            // Arrange
            _serviceResponseBuilder.Data.Set(new TestBuilderModel { Name = "Test" });

            // Act
            var response = _serviceResponseBuilder.BuildResponse(null);

            // Assert
            response.Should().NotBeNull("because BuildResponse should handle null action gracefully");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because response should still be built correctly");
        }

        [Fact]
        public void BuildResponse_WithActionThatThrows_ShouldAllowExceptionToPropagate()
        {
            // Arrange
            _serviceResponseBuilder.Data.Set(new TestBuilderModel { Name = "Test" });

            // Act
            Action act = () => _serviceResponseBuilder.BuildResponse(r =>
            {
                throw new InvalidOperationException("Test exception");
            });

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Test exception", "because action exceptions should propagate");
        }

        [Fact]
        public void BuildResponse_WithActionModifyingMultipleProperties_ShouldApplyAllChanges()
        {
            // Arrange
            _serviceResponseBuilder.Data.Set(new TestBuilderModel { Name = "Original" });

            // Act
            var response = _serviceResponseBuilder.BuildResponse(r =>
            {
                r.ApiResponse.MetaData.ResponseId = "action-modified-id";
                r.ApiResponse.Data!.Name = "Action Modified";
                r.HttpResponse!.HttpStatusCode = HttpStatusCode.Accepted;
            });

            // Assert
            response.ApiResponse.MetaData.ResponseId.Should().Be("action-modified-id", "because action should modify response ID");
            response.ApiResponse.Data!.Name.Should().Be("Action Modified", "because action should modify data");
            response.HttpResponse!.HttpStatusCode.Should().Be(HttpStatusCode.Accepted, "because action should modify HTTP status");
        }

        #endregion

        #region Integration Tests

        [Fact]
        public void FluentAPI_ComplexResponseBuilding_ShouldWorkCorrectly()
        {
            // Arrange
            var testData = new TestBuilderModel { Name = "Integration Test", Value = 999 };

            // Act
            var response = _serviceResponseBuilder
                .Data.Set(testData)
                .Meta.SetId("integration-test-123")
                    .AddTag("environment", "test")
                    .AddMessage(msg => msg.Title = "Processing Complete")
                .Http.SetStatusCode(HttpStatusCode.Created)
                    .AddHeader("X-Custom-Header", "test-value")
                .Links.AddLink("https://api.example.com/test/999", "self", "Test Resource")
                .BuildResponse();

            // Assert
            response.Should().NotBeNull("because fluent API should build complete response");
            response.ApiResponse.Data.Should().Be(testData, "because data should be preserved");
            response.ApiResponse.MetaData.ResponseId.Should().Be("integration-test-123", "because custom response ID should be set");
            response.ApiResponse.MetaData.Tags!["environment"].Should().Be("test", "because tag should be set");
            response.ApiResponse.MetaData.Messages.Should().HaveCount(1, "because message should be added");
            response.ApiResponse.Links.Should().HaveCount(1, "because link should be added");
            response.HttpResponse!.HttpStatusCode.Should().Be(HttpStatusCode.Created, "because custom HTTP status should be set");
            response.HttpResponse.Headers.Should().HaveCount(1, "because custom header should be added");
        }

        [Fact]
        public void FluentAPI_ErrorResponseBuilding_ShouldWorkCorrectly()
        {
            // Act
            var response = _serviceResponseBuilder
                .Errors.AddError(CallStatusCode.NotFound, "Resource Not Found", "The requested resource was not found")
                .Validation.AddValidation(PropertyLocation.Path, "id", "Invalid ID", "ID must be a positive integer")
                .Http.SetStatusCode(HttpStatusCode.NotFound)
                .Meta.AddTag("error-category", "client-error")
                .BuildResponse();

            // Assert
            response.Should().NotBeNull("because fluent API should build error response");
            response.ApiResponse.Errors.Should().HaveCount(2, "because both error and validation error should be added");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.NotFound, "because call status should be set from first error");
            response.ApiResponse.MetaData.Tags!["error-category"].Should().Be("client-error", "because error tag should be set");
            response.HttpResponse!.HttpStatusCode.Should().Be(HttpStatusCode.NotFound, "because HTTP status should match error status");
        }

        [Fact]
        public void BuildResponse_WithPostBuildAction_ShouldExecuteAfterProcessing()
        {
            // Arrange
            var originalResponseId = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.MetaData.ResponseId;

            // Act
            var response = _serviceResponseBuilder
                .Data.Set(new TestBuilderModel { Name = "Test" })
                .BuildResponse(r =>
                {
                    // Verify that processing has already occurred
                    r.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because processing should be complete");
                    r.ApiResponse.MetaData.CallStatusId.Should().Be((int)CallStatusCode.Ok, "because enumeration IDs should be set");
                    
                    // Make post-build modifications
                    r.ApiResponse.MetaData.ResponseId = "post-build-modified";
                });

            // Assert
            response.ApiResponse.MetaData.ResponseId.Should().Be("post-build-modified", "because post-build action should modify response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because call status should be set during processing");
            response.ApiResponse.MetaData.CallStatusId.Should().Be((int)CallStatusCode.Ok, "because enumeration IDs should be set during processing");
        }

        [Theory]
        [InlineData(CallStatusCode.Ok, HttpStatusCode.OK)]
        [InlineData(CallStatusCode.InvalidArgument, HttpStatusCode.BadRequest)]
        [InlineData(CallStatusCode.NotFound, HttpStatusCode.NotFound)]
        [InlineData(CallStatusCode.InternalError, HttpStatusCode.InternalServerError)]
        [InlineData(CallStatusCode.Unauthenticated, HttpStatusCode.Unauthorized)]
        public void BuildResponse_WithDifferentCallStatusCodes_ShouldMapToCorrectHttpStatusCodes(CallStatusCode callStatus, HttpStatusCode expectedHttpStatus)
        {
            // Arrange
            if (callStatus == CallStatusCode.Ok)
            {
                _serviceResponseBuilder.Data.Set(new TestBuilderModel { Name = "Test" });
                _serviceResponseBuilder.Meta.SetStatus(callStatus);
            }
            else
            {
                _serviceResponseBuilder.Errors.AddError(callStatus, "Test Error");
            }

            // Act
            var response = _serviceResponseBuilder.BuildResponse();

            // Assert
            response.ApiResponse.MetaData.CallStatus.Should().Be(callStatus, "because call status should be set correctly");
            response.HttpResponse!.HttpStatusCode.Should().Be(expectedHttpStatus, "because HTTP status should map correctly from call status");
        }

        [Fact]
        public void ServiceResponseBuilder_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where T : class, new()
            
            // Arrange & Act
            var testModelBuilder = new ServiceResponseBuilder<TestBuilderModel>();
            var listBuilder = new ServiceResponseBuilder<List<string>>();
            var dictBuilder = new ServiceResponseBuilder<Dictionary<string, object>>();

            // Assert
            testModelBuilder.Should().NotBeNull("because TestBuilderModel is a valid reference type with parameterless constructor");
            listBuilder.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictBuilder.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        #endregion
    }
}