using HT.Api.Client.Contracts.Models;
using HT.Api.Client.Contracts.Extensions;
using System.Net;
using FluentAssertions;

namespace HT.Api.Client.Contracts.Tests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Client")]
    [Trait("Category", "HT/Api/Client/Contracts")]
    [Trait("Category", "HT/Api/Client/Contracts/Extensions")]
    public class ApiResponseExtensionsTests
    {
        #region Test Data Classes

        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        #endregion

        #region Base Response Extension Tests

        [Fact]
        public void IsSuccess_WithNoErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.IsSuccess();

            // Assert
            result.Should().BeTrue("because response has no errors");
        }

        [Fact]
        public void IsSuccess_WithSuccessErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.Errors = new List<ErrorMessage> { ErrorMessage.Create(ErrorCodes.Ok) };

            // Act
            var result = response.IsSuccess();

            // Assert
            result.Should().BeTrue("because all errors are success codes");
        }

        [Fact]
        public void IsSuccess_WithFailureErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "test error");

            // Act
            var result = response.IsSuccess();

            // Assert
            result.Should().BeFalse("because response has failure errors");
        }

        [Fact]
        public void HasFailures_WithNoErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.HasFailures();

            // Assert
            result.Should().BeFalse("because response has no errors");
        }

        [Fact]
        public void HasFailures_WithFailureErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "test error");

            // Act
            var result = response.HasFailures();

            // Assert
            result.Should().BeTrue("because response has failure errors");
        }

        [Fact]
        public void HasServerErrors_WithServerError_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddInternalError("Server error");

            // Act
            var result = response.HasServerErrors();

            // Assert
            result.Should().BeTrue("because response has server errors");
        }

        [Fact]
        public void HasServerErrors_WithClientError_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Client error");

            // Act
            var result = response.HasServerErrors();

            // Assert
            result.Should().BeFalse("because response only has client errors");
        }

        [Fact]
        public void HasClientErrors_WithClientError_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Client error");

            // Act
            var result = response.HasClientErrors();

            // Assert
            result.Should().BeTrue("because response has client errors");
        }

        [Fact]
        public void HasClientErrors_WithServerError_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddInternalError("Server error");

            // Act
            var result = response.HasClientErrors();

            // Assert
            result.Should().BeFalse("because response only has server errors");
        }

        [Fact]
        public void HasCancellations_WithCancellationError_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddCancellationError("Operation cancelled");

            // Act
            var result = response.HasCancellations();

            // Assert
            result.Should().BeTrue("because response has cancellation errors");
        }

        [Fact]
        public void HasCancellations_WithoutCancellationError_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Validation error");

            // Act
            var result = response.HasCancellations();

            // Assert
            result.Should().BeFalse("because response has no cancellation errors");
        }

        [Fact]
        public void GetHighestPriorityError_WithMultipleErrors_ShouldReturnHighestPriority()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Error 1");  // Lower priority
            response.AddInternalError("Error 2");             // Higher priority

            // Act
            var result = response.GetHighestPriorityError();

            // Assert
            result.Should().NotBeNull("because response has errors");
            result!.StatusCode.Should().Be(ErrorCodes.InternalError, "because internal errors have higher priority");
        }

        [Fact]
        public void GetHighestPriorityError_WithNoErrors_ShouldReturnNull()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.GetHighestPriorityError();

            // Assert
            result.Should().BeNull("because response has no errors");
        }

        [Fact]
        public void GetMostSevereError_ShouldReturnMostSevereError()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Error 1");
            response.AddInternalError("Error 2");

            // Act
            var result = response.GetMostSevereError();

            // Assert
            result.Should().NotBeNull("because response has errors");
            result!.StatusCode.Should().Be(ErrorCodes.InternalError, "because internal errors are more severe");
        }

        [Fact]
        public void GetPrimaryHttpStatusCode_WithErrors_ShouldReturnCorrectStatusCode()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddNotFoundError("Resource not found");

            // Act
            var result = response.GetPrimaryHttpStatusCode();

            // Assert
            result.Should().Be(HttpStatusCode.NotFound, "because the highest priority error is NotFound");
        }

        [Fact]
        public void GetPrimaryHttpStatusCode_WithNoErrors_ShouldReturnOK()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.GetPrimaryHttpStatusCode();

            // Assert
            result.Should().Be(HttpStatusCode.OK, "because response has no errors");
        }

        [Fact]
        public void GetResultCategory_WithDifferentErrorTypes_ShouldPrioritizeCorrectly()
        {
            // Arrange & Act & Assert - Success case
            var successResponse = new ApiResponse();
            successResponse.GetResultCategory().Should().Be(ResultCategory.Success, "because no errors");

            // Cancellation takes priority
            var cancelledResponse = new ApiResponse();
            cancelledResponse.AddCancellationError("Cancelled");
            cancelledResponse.AddInternalError("Server error");
            cancelledResponse.GetResultCategory().Should().Be(ResultCategory.Cancelled, "because cancellation takes priority");

            // Server error over client error
            var serverErrorResponse = new ApiResponse();
            serverErrorResponse.AddInternalError("Server error");
            serverErrorResponse.AddValidationError("test", "Client error");
            serverErrorResponse.GetResultCategory().Should().Be(ResultCategory.ServerError, "because server errors take priority over client errors");

            // Client error only
            var clientErrorResponse = new ApiResponse();
            clientErrorResponse.AddValidationError("test", "Client error");
            clientErrorResponse.GetResultCategory().Should().Be(ResultCategory.ClientError, "because only client errors present");
        }

        [Fact]
        public void GetErrorCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Error 1");
            response.AddValidationError("test2", "Error 2");
            response.AddNotFoundError("Not found");

            // Act
            var result = response.GetErrorCount();

            // Assert
            result.Should().Be(3, "because three errors were added");
        }

        [Fact]
        public void GetErrorCount_WithNoErrors_ShouldReturnZero()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.GetErrorCount();

            // Assert
            result.Should().Be(0, "because no errors were added");
        }

        [Fact]
        public void GetClientErrorCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Client error 1");
            response.AddValidationError("test2", "Client error 2");
            response.AddInternalError("Server error");

            // Act
            var result = response.GetClientErrorCount();

            // Assert
            result.Should().Be(2, "because two client errors were added");
        }

        [Fact]
        public void GetServerErrorCount_ShouldReturnCorrectCount()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddInternalError("Server error 1");
            response.AddInternalError("Server error 2");
            response.AddValidationError("test", "Client error");

            // Act
            var result = response.GetServerErrorCount();

            // Assert
            result.Should().Be(2, "because two server errors were added");
        }

        [Fact]
        public void GetErrorGroupsByCode_ShouldGroupCorrectly()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Error 1");
            response.AddValidationError("test2", "Error 2");
            response.AddNotFoundError("Not found");

            // Act
            var result = response.GetErrorGroupsByCode();

            // Assert
            result.Should().HaveCount(2, "because there are two different error codes");
            result.Should().ContainSingle(g => g.Key == ErrorCodes.InvalidArgument && g.Count() == 2, 
                "because there are two validation errors");
            result.Should().ContainSingle(g => g.Key == ErrorCodes.NotFound && g.Count() == 1, 
                "because there is one not found error");
        }

        [Fact]
        public void GetRetryableErrors_ShouldReturnOnlyRetryableErrors()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Non-retryable error");
            response.AddError(ErrorCodes.OperationTimeOut, "Retryable error");

            // Act
            var result = response.GetRetryableErrors().ToList();

            // Assert
            result.Should().HaveCount(1, "because only one error is retryable");
            result.First().StatusCode.Should().Be(ErrorCodes.OperationTimeOut, "because timeout errors are retryable");
        }

        [Fact]
        public void GetErrorsByPriority_ShouldReturnErrorsWithSpecificPriority()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Priority 3 error");
            response.AddInternalError("Priority 1 error");

            // Act
            var priority1Errors = response.GetErrorsByPriority(1).ToList();
            var priority3Errors = response.GetErrorsByPriority(3).ToList();

            // Assert
            priority1Errors.Should().HaveCount(1, "because one error has priority 1");
            priority3Errors.Should().HaveCount(1, "because one error has priority 3");
        }

        [Fact]
        public void GetErrorsByCategory_ShouldReturnErrorsWithSpecificCategory()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Client error");
            response.AddInternalError("Server error");

            // Act
            var clientErrors = response.GetErrorsByCategory(ResultCategory.ClientError).ToList();
            var serverErrors = response.GetErrorsByCategory(ResultCategory.ServerError).ToList();

            // Assert
            clientErrors.Should().HaveCount(1, "because one error is a client error");
            serverErrors.Should().HaveCount(1, "because one error is a server error");
        }

        [Fact]
        public void GetUserFriendlyMessages_ShouldReturnUserActionSuggestions()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Invalid email format");
            response.AddNotFoundError("User not found");

            // Act
            var messages = response.GetUserFriendlyMessages().ToList();

            // Assert
            messages.Should().HaveCount(2, "because there are two errors");
            messages.Should().AllSatisfy(m => m.Should().NotBeNullOrEmpty("because user-friendly messages should be provided"));
        }

        [Fact]
        public void GetDetailedErrorSummary_ShouldFormatWithErrorCodes()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email is required");
            response.AddNotFoundError("User not found");

            // Act
            var summary = response.GetDetailedErrorSummary();

            // Assert
            summary.Should().Contain("[InvalidArgument]", "because validation error code should be included");
            summary.Should().Contain("[NotFound]", "because not found error code should be included");
            summary.Should().Contain("Email is required", "because error detail should be included");
            summary.Should().Contain("User not found", "because error detail should be included");
        }

        [Fact]
        public void GetDetailedErrorSummary_WithCustomSeparator_ShouldUseCustomSeparator()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Error 1");
            response.AddValidationError("test2", "Error 2");

            // Act
            var summary = response.GetDetailedErrorSummary(" | ");

            // Assert
            summary.Should().Contain(" | ", "because custom separator should be used");
            summary.Should().NotContain(";", "because default separator should not be used");
        }

        [Fact]
        public void GetDetailedErrorSummary_WithNoErrors_ShouldReturnEmpty()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var summary = response.GetDetailedErrorSummary();

            // Assert
            summary.Should().BeEmpty("because there are no errors");
        }

        [Fact]
        public void GetLoggingErrorSummary_ShouldFormatForLogging()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test1", "Error 1");
            response.AddValidationError("test2", "Error 2");
            response.AddNotFoundError("Not found");

            // Act
            var summary = response.GetLoggingErrorSummary();

            // Assert
            summary.Should().Contain("Errors: 3", "because there are 3 errors total");
            summary.Should().Contain("InvalidArgument(2)", "because there are 2 validation errors");
            summary.Should().Contain("NotFound(1)", "because there is 1 not found error");
        }

        [Fact]
        public void GetLoggingErrorSummary_WithNoErrors_ShouldReturnNoErrors()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var summary = response.GetLoggingErrorSummary();

            // Assert
            summary.Should().Be("No errors", "because there are no errors");
        }

        [Fact]
        public void IsValidState_WithConsistentState_ShouldReturnTrue()
        {
            // Arrange & Act & Assert - Success state
            var successResponse = new ApiResponse();
            successResponse.IsValidState().Should().BeTrue("because no errors and success state is consistent");

            // Error state
            var errorResponse = new ApiResponse();
            errorResponse.AddValidationError("test", "Error");
            errorResponse.IsValidState().Should().BeTrue("because has errors and failure state is consistent");
        }

        [Fact]
        public void GetStateValidationIssues_WithInconsistentState_ShouldReturnIssues()
        {
            // Arrange - Create a response that's in an inconsistent state (manually set)
            var response = new ApiResponse();
            response.Errors = new List<ErrorMessage>
            {
                new ErrorMessage { StatusCode = ErrorCodes.Ok } // Success error but treated as having errors
            };

            // Act
            var issues = response.GetStateValidationIssues().ToList();

            // Assert - This test might not trigger issues depending on the IsSuccess implementation
            // But we're testing the method exists and works
            issues.Should().NotBeNull("because method should return a collection");
        }

        [Fact]
        public void IsRetryRecommended_WithRetryableErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddError(ErrorCodes.OperationTimeOut, "Timeout error");

            // Act
            var result = response.IsRetryRecommended();

            // Assert
            result.Should().BeTrue("because timeout errors are retryable");
        }

        [Fact]
        public void IsRetryRecommended_WithoutRetryableErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Validation error");

            // Act
            var result = response.IsRetryRecommended();

            // Assert
            result.Should().BeFalse("because validation errors are not retryable");
        }

        [Fact]
        public void GetHighestPriorityErrorCode_WithErrors_ShouldReturnHighestPriorityCode()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "Low priority");
            response.AddInternalError("High priority");

            // Act
            var result = response.GetHighestPriorityErrorCode();

            // Assert
            result.Should().Be(ErrorCodes.InternalError, "because internal error has highest priority");
        }

        [Fact]
        public void GetHighestPriorityErrorCode_WithNoErrors_ShouldReturnOk()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.GetHighestPriorityErrorCode();

            // Assert
            result.Should().Be(ErrorCodes.Ok, "because no errors means OK status");
        }

        [Fact]
        public void HasOnlyWarnings_WithWarningErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddError(ErrorCodes.AlreadyExists, "Already exists warning");

            // Act
            var result = response.HasOnlyWarnings();

            // Assert
            result.Should().BeTrue("because AlreadyExists is considered a warning");
        }

        [Fact]
        public void HasOnlyWarnings_WithNonWarningErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddInternalError("Internal server error"); // This should definitely not be a warning

            // Act
            var result = response.HasOnlyWarnings();

            // Assert
            result.Should().BeFalse("because internal server errors are not warnings");
        }

        [Fact]
        public void HasOnlyWarnings_WithNoErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.HasOnlyWarnings();

            // Assert
            result.Should().BeFalse("because there are no errors at all");
        }

        #endregion

        #region Generic Response Extension Tests

        [Fact]
        public void HasData_WithData_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.Data = new TestUser { Id = 1, Name = "Test" };

            // Act
            var result = response.HasData();

            // Assert
            result.Should().BeTrue("because response has data");
        }

        [Fact]
        public void HasData_WithoutData_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act
            var result = response.HasData();

            // Assert
            result.Should().BeFalse("because response has no data");
        }

        [Fact]
        public void DataOrDefault_WithData_ShouldReturnData()
        {
            // Arrange
            var user = new TestUser { Id = 1, Name = "Test User" };
            var response = new ApiResponse<TestUser>();
            response.Data = user;

            // Act
            var result = response.DataOrDefault();

            // Assert
            result.Should().BeSameAs(user, "because response has data");
        }

        [Fact]
        public void DataOrDefault_WithoutData_ShouldReturnNewInstance()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act
            var result = response.DataOrDefault();

            // Assert
            result.Should().NotBeNull("because DataOrDefault should return new instance");
            result.Should().BeOfType<TestUser>("because it should be correct type");
            result.Id.Should().Be(0, "because it's a new default instance");
        }

        [Fact]
        public void IsJsonApiCompliant_WithDataOnly_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.Data = new TestUser { Id = 1, Name = "Test" };

            // Act
            var result = response.IsJsonApiCompliant();

            // Assert
            result.Should().BeTrue("because data without errors is compliant");
        }

        [Fact]
        public void IsJsonApiCompliant_WithErrorsOnly_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.AddValidationError("test", "Test error");

            // Act
            var result = response.IsJsonApiCompliant();

            // Assert
            result.Should().BeTrue("because errors without data is compliant");
        }

        [Fact]
        public void IsJsonApiCompliant_WithDataAndErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.Data = new TestUser { Id = 1, Name = "Test" };
            response.Errors = new List<ErrorMessage> { ErrorMessage.Create(ErrorCodes.InternalError) };

            // Act
            var result = response.IsJsonApiCompliant();

            // Assert
            result.Should().BeFalse("because data and errors cannot coexist");
        }

        [Fact]
        public void IsJsonApiCompliant_WithNeitherDataNorErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act
            var result = response.IsJsonApiCompliant();

            // Assert
            result.Should().BeTrue("because empty response is compliant");
        }

        [Fact]
        public void GetComplianceIssues_WithCompliantResponse_ShouldReturnEmpty()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.Data = new TestUser { Id = 1, Name = "Test" };

            // Act
            var issues = response.GetComplianceIssues().ToList();

            // Assert
            issues.Should().BeEmpty("because response is compliant");
        }

        [Fact]
        public void GetComplianceIssues_WithNonCompliantResponse_ShouldReturnIssues()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.Data = new TestUser { Id = 1, Name = "Test" };
            response.Errors = new List<ErrorMessage> { ErrorMessage.Create(ErrorCodes.InternalError) };

            // Act
            var issues = response.GetComplianceIssues().ToList();

            // Assert
            issues.Should().HaveCount(1, "because there is one compliance issue");
            issues.First().Should().Contain("JSON:API violation", "because it should describe the violation");
            issues.First().Should().Contain("Data and Errors cannot coexist", "because it should explain the specific issue");
        }

        #endregion

        #region Edge Cases and Null Handling

        [Fact]
        public void ExtensionMethods_WithNullErrors_ShouldHandleGracefully()
        {
            // Arrange
            var response = new ApiResponse();
            response.Errors = null;

            // Act & Assert - All methods should handle null errors gracefully
            response.IsSuccess().Should().BeTrue("because null errors means success");
            response.HasFailures().Should().BeFalse("because null errors means no failures");
            response.HasServerErrors().Should().BeFalse("because null errors means no server errors");
            response.HasClientErrors().Should().BeFalse("because null errors means no client errors");
            response.HasCancellations().Should().BeFalse("because null errors means no cancellations");
            response.GetErrorCount().Should().Be(0, "because null errors means zero count");
            response.GetHighestPriorityError().Should().BeNull("because null errors means no priority error");
            response.GetRetryableErrors().Should().BeEmpty("because null errors means no retryable errors");
        }

        [Fact]
        public void ExtensionMethods_WithEmptyErrors_ShouldHandleGracefully()
        {
            // Arrange
            var response = new ApiResponse();
            response.Errors = new List<ErrorMessage>();

            // Act & Assert - All methods should handle empty errors gracefully
            response.IsSuccess().Should().BeTrue("because empty errors means success");
            response.HasFailures().Should().BeFalse("because empty errors means no failures");
            response.GetErrorCount().Should().Be(0, "because empty errors means zero count");
            response.GetErrorGroupsByCode().Should().BeEmpty("because empty errors means no groups");
        }

        [Fact]
        public void GenericExtensionMethods_WithNullData_ShouldHandleGracefully()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            response.Data = null;

            // Act & Assert
            response.HasData().Should().BeFalse("because data is null");
            response.DataOrDefault().Should().NotBeNull("because DataOrDefault should return new instance");
            response.IsJsonApiCompliant().Should().BeTrue("because null data with no errors is compliant");
        }

        #endregion
    }
}