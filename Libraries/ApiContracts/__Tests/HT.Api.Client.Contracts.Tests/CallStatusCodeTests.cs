using HT.Api.Client.Contracts.Models;
using System.Net;
using FluentAssertions;
// ReSharper disable InconsistentNaming

namespace HT.Api.Client.Contracts.Tests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Client")]
    [Trait("Category", "HT/Api/Client/Contracts")]
    [Trait("Category", "HT/Api/Client/Contracts/Models")]
    [Trait("Category", "HT/Api/Client/Contracts/Models/CallStatusCode")]
    public class CallStatusCodeTests
    {
        #region Enum Value Tests

        [Fact]
        public void CallStatusCode_EnumValues_ShouldHaveCorrectValues()
        {
            // Arrange & Act & Assert
            ((int)CallStatusCode.Ok).Should().Be(0, "because Ok represents success state");
            ((int)CallStatusCode.Cancelled).Should().Be(1, "because Cancelled is a special non-error state");
            ((int)CallStatusCode.Error).Should().Be(8, "because Error is the base flag (2 << 2)");
            
            // Error codes with bit masks
            ((int)CallStatusCode.InvalidArgument).Should().Be(24, "because InvalidArgument = Error(8) | 2<<3(16)");
            ((int)CallStatusCode.OperationTimeOut).Should().Be(40, "because OperationTimeOut = Error(8) | 2<<4(32)");
            ((int)CallStatusCode.NotFound).Should().Be(72, "because NotFound = Error(8) | 2<<5(64)");
            ((int)CallStatusCode.AlreadyExists).Should().Be(136, "because AlreadyExists = Error(8) | 2<<6(128)");
            ((int)CallStatusCode.PermissionDenied).Should().Be(264, "because PermissionDenied = Error(8) | 2<<7(256)");
            ((int)CallStatusCode.ResourceExhausted).Should().Be(520, "because ResourceExhausted = Error(8) | 2<<8(512)");
            ((int)CallStatusCode.FailedPrecondition).Should().Be(1032, "because FailedPrecondition = Error(8) | 2<<9(1024)");
            ((int)CallStatusCode.Aborted).Should().Be(2056, "because Aborted = Error(8) | 2<<10(2048)");
            ((int)CallStatusCode.OutOfRange).Should().Be(4104, "because OutOfRange = Error(8) | 2<<11(4096)");
            ((int)CallStatusCode.NotImplemented).Should().Be(8200, "because NotImplemented = Error(8) | 2<<12(8192)");
            ((int)CallStatusCode.InternalError).Should().Be(16392, "because InternalError = Error(8) | 2<<13(16384)");
            ((int)CallStatusCode.Unavailable).Should().Be(32776, "because Unavailable = Error(8) | 2<<14(32768)");
            ((int)CallStatusCode.Unauthenticated).Should().Be(65544, "because Unauthenticated = Error(8) | 2<<15(65536)");
        }

        [Fact]
        public void CallStatusCode_AllValues_ShouldBeUnique()
        {
            // Arrange
            var allValues = Enum.GetValues<CallStatusCode>().Select(e => (int)e).ToList();
            var uniqueValues = allValues.Distinct().ToList();

            // Act & Assert
            allValues.Should().HaveCount(uniqueValues.Count, "because all call status codes should have unique values");
        }

        #endregion

        #region IsSuccess Tests

        [Fact]
        public void IsSuccess_Ok_ShouldReturnTrue()
        {
            // Arrange
            var statusCode = CallStatusCode.Ok;

            // Act
            var result = statusCode.IsSuccess();

            // Assert
            result.Should().BeTrue("because Ok indicates successful operation");
        }

        [Theory]
        [InlineData(CallStatusCode.Cancelled)]
        [InlineData(CallStatusCode.Error)]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.Unauthenticated)]
        public void IsSuccess_NonOkCallStatusCodes_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsSuccess();

            // Assert
            result.Should().BeFalse($"because {statusCode} does not indicate success");
        }

        #endregion

        #region IsFailure Tests

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.Cancelled)]
        [InlineData(CallStatusCode.Error)]
        public void IsFailure_NonFailureStates_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsFailure();

            // Assert
            result.Should().BeFalse($"because {statusCode} should not be considered a failure state");
        }

        [Theory]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.Unauthenticated)]
        [InlineData(CallStatusCode.PermissionDenied)]
        [InlineData(CallStatusCode.ResourceExhausted)]
        [InlineData(CallStatusCode.FailedPrecondition)]
        [InlineData(CallStatusCode.Aborted)]
        [InlineData(CallStatusCode.OutOfRange)]
        [InlineData(CallStatusCode.NotImplemented)]
        [InlineData(CallStatusCode.Unavailable)]
        [InlineData(CallStatusCode.OperationTimeOut)]
        public void IsFailure_CallStatusCodes_ShouldReturnTrue(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsFailure();

            // Assert
            result.Should().BeTrue($"because {statusCode} represents a failure state");
        }

        #endregion

        #region HasErrorFlag Tests

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.Cancelled)]
        public void HasErrorFlag_NonErrorStates_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.HasErrorFlag();

            // Assert
            result.Should().BeFalse($"because {statusCode} should not have the error flag set");
        }

        [Theory]
        [InlineData(CallStatusCode.Error)]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.Unauthenticated)]
        public void HasErrorFlag_ErrorStates_ShouldReturnTrue(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.HasErrorFlag();

            // Assert
            result.Should().BeTrue($"because {statusCode} should have the error flag set");
        }

        #endregion

        #region IsCancelled Tests

        [Fact]
        public void IsCancelled_Cancelled_ShouldReturnTrue()
        {
            // Arrange
            var statusCode = CallStatusCode.Cancelled;

            // Act
            var result = statusCode.IsCancelled();

            // Assert
            result.Should().BeTrue("because Cancelled state should be identified correctly");
        }

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.Error)]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        public void IsCancelled_NonCancelledStates_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsCancelled();

            // Assert
            result.Should().BeFalse($"because {statusCode} is not a cancelled state");
        }

        #endregion

        #region GetSpecificErrorBits Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, 0)]
        [InlineData(CallStatusCode.Cancelled, 0)]
        public void GetSpecificErrorBits_NonErrorStates_ShouldReturnZero(CallStatusCode statusCode, int expectedBits)
        {
            // Act
            var result = statusCode.GetSpecificErrorBits();

            // Assert
            result.Should().Be(expectedBits, $"because {statusCode} has no specific error bits");
        }

        [Theory]
        [InlineData(CallStatusCode.Error, 0)]
        [InlineData(CallStatusCode.InvalidArgument, 16)]
        [InlineData(CallStatusCode.OperationTimeOut, 32)]
        [InlineData(CallStatusCode.NotFound, 64)]
        [InlineData(CallStatusCode.AlreadyExists, 128)]
        public void GetSpecificErrorBits_ErrorStates_ShouldReturnCorrectBits(CallStatusCode statusCode, int expectedBits)
        {
            // Act
            var result = statusCode.GetSpecificErrorBits();

            // Assert
            result.Should().Be(expectedBits, $"because {statusCode} should have specific error bits {expectedBits}");
        }

        #endregion

        #region VerifyBitMask Tests

        [Fact]
        public void VerifyBitMask_AllCallStatusCodes_ShouldReturnTrue()
        {
            // Arrange
            var allStatusCodes = Enum.GetValues<CallStatusCode>();

            // Act & Assert
            foreach (var statusCode in allStatusCodes)
            {
                var result = statusCode.VerifyBitMask();
                result.Should().BeTrue($"because bit mask verification should pass for {statusCode}");
            }
        }

        #endregion

        #region GetBitMaskAnalysis Tests

        [Fact]
        public void GetBitMaskAnalysis_InvalidArgument_ShouldReturnCorrectAnalysis()
        {
            // Arrange
            var statusCode = CallStatusCode.InvalidArgument;

            // Act
            var result = statusCode.GetBitMaskAnalysis();

            // Assert
            result.Should().Contain("InvalidArgument", "because the analysis should include the status code name");
            result.Should().Contain("Value: 24", "because InvalidArgument has value 24");
            result.Should().Contain("HasErrorFlag: True", "because InvalidArgument has the error flag");
            result.Should().Contain("SpecificBits: 16", "because InvalidArgument has specific bits 16");
        }

        [Fact]
        public void GetBitMaskAnalysis_Ok_ShouldReturnCorrectAnalysis()
        {
            // Arrange
            var statusCode = CallStatusCode.Ok;

            // Act
            var result = statusCode.GetBitMaskAnalysis();

            // Assert
            result.Should().Contain("Ok", "because the analysis should include the status code name");
            result.Should().Contain("Value: 0", "because Ok has value 0");
            result.Should().Contain("HasErrorFlag: False", "because Ok does not have the error flag");
            result.Should().Contain("SpecificBits: 0", "because Ok has no specific bits");
        }

        #endregion

        #region IsClientError Tests

        [Theory]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.AlreadyExists)]
        [InlineData(CallStatusCode.PermissionDenied)]
        [InlineData(CallStatusCode.Unauthenticated)]
        [InlineData(CallStatusCode.FailedPrecondition)]
        [InlineData(CallStatusCode.OutOfRange)]
        public void IsClientError_ClientErrorCallStatusCodes_ShouldReturnTrue(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsClientError();

            // Assert
            result.Should().BeTrue($"because {statusCode} is a client error (4xx equivalent)");
        }

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.Cancelled)]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.Unavailable)]
        [InlineData(CallStatusCode.NotImplemented)]
        public void IsClientError_NonClientErrorCallStatusCodes_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsClientError();

            // Assert
            result.Should().BeFalse($"because {statusCode} is not a client error");
        }

        #endregion

        #region IsServerError Tests

        [Theory]
        [InlineData(CallStatusCode.InternalError)]
        [InlineData(CallStatusCode.NotImplemented)]
        [InlineData(CallStatusCode.Unavailable)]
        [InlineData(CallStatusCode.ResourceExhausted)]
        [InlineData(CallStatusCode.OperationTimeOut)]
        [InlineData(CallStatusCode.Aborted)]
        public void IsServerError_ServerErrorCallStatusCodes_ShouldReturnTrue(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsServerError();

            // Assert
            result.Should().BeTrue($"because {statusCode} is a server error (5xx equivalent)");
        }

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.Cancelled)]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.Unauthenticated)]
        public void IsServerError_NonServerErrorCallStatusCodes_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsServerError();

            // Assert
            result.Should().BeFalse($"because {statusCode} is not a server error");
        }

        #endregion

        #region IsRetryable Tests

        [Theory]
        [InlineData(CallStatusCode.Unavailable)]
        [InlineData(CallStatusCode.ResourceExhausted)]
        [InlineData(CallStatusCode.OperationTimeOut)]
        [InlineData(CallStatusCode.Aborted)]
        [InlineData(CallStatusCode.InternalError)]
        public void IsRetryable_RetryableCallStatusCodes_ShouldReturnTrue(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsRetryable();

            // Assert
            result.Should().BeTrue($"because {statusCode} represents a retryable error condition");
        }

        [Theory]
        [InlineData(CallStatusCode.Ok)]
        [InlineData(CallStatusCode.Cancelled)]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.Unauthenticated)]
        [InlineData(CallStatusCode.PermissionDenied)]
        public void IsRetryable_NonRetryableCallStatusCodes_ShouldReturnFalse(CallStatusCode statusCode)
        {
            // Act
            var result = statusCode.IsRetryable();

            // Assert
            result.Should().BeFalse($"because {statusCode} does not represent a retryable condition");
        }

        #endregion

        #region GetDescription Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "Operation completed successfully")]
        [InlineData(CallStatusCode.Cancelled, "Operation was cancelled")]
        [InlineData(CallStatusCode.InvalidArgument, "Invalid input provided")]
        [InlineData(CallStatusCode.NotFound, "Resource not found")]
        [InlineData(CallStatusCode.InternalError, "Internal server error")]
        [InlineData(CallStatusCode.Unauthenticated, "Authentication required")]
        public void GetDescription_ValidCallStatusCodes_ShouldReturnCorrectDescription(CallStatusCode statusCode, string expectedDescription)
        {
            // Act
            var result = statusCode.GetDescription();

            // Assert
            result.Should().Be(expectedDescription, $"because {statusCode} should have the correct description");
        }

        [Fact]
        public void GetDescription_UnknownCallStatusCode_ShouldReturnUnknownError()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetDescription();

            // Assert
            result.Should().Be("Unknown error", "because unknown call status codes should return default message");
        }

        #endregion

        #region GetSeverityClass Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "success")]
        [InlineData(CallStatusCode.Cancelled, "secondary")]
        [InlineData(CallStatusCode.InvalidArgument, "warning")]
        [InlineData(CallStatusCode.NotFound, "warning")]
        [InlineData(CallStatusCode.AlreadyExists, "info")]
        [InlineData(CallStatusCode.PermissionDenied, "danger")]
        [InlineData(CallStatusCode.Unauthenticated, "danger")]
        [InlineData(CallStatusCode.InternalError, "danger")]
        public void GetSeverityClass_ValidCallStatusCodes_ShouldReturnCorrectBootstrapClass(CallStatusCode statusCode, string expectedClass)
        {
            // Act
            var result = statusCode.GetSeverityClass();

            // Assert
            result.Should().Be(expectedClass, $"because {statusCode} should map to Bootstrap class {expectedClass}");
        }

        #endregion

        #region GetAlertClass Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "alert-success")]
        [InlineData(CallStatusCode.InvalidArgument, "alert-warning")]
        [InlineData(CallStatusCode.InternalError, "alert-danger")]
        [InlineData(CallStatusCode.Cancelled, "alert-secondary")]
        public void GetAlertClass_ValidCallStatusCodes_ShouldReturnCorrectAlertClass(CallStatusCode statusCode, string expectedClass)
        {
            // Act
            var result = statusCode.GetAlertClass();

            // Assert
            result.Should().Be(expectedClass, $"because {statusCode} should map to alert class {expectedClass}");
        }

        #endregion

        #region GetIconClass Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "fa-check-circle")]
        [InlineData(CallStatusCode.Cancelled, "fa-times-circle")]
        [InlineData(CallStatusCode.InvalidArgument, "fa-exclamation-triangle")]
        [InlineData(CallStatusCode.NotFound, "fa-search")]
        [InlineData(CallStatusCode.PermissionDenied, "fa-lock")]
        [InlineData(CallStatusCode.Unauthenticated, "fa-user-slash")]
        [InlineData(CallStatusCode.InternalError, "fa-bug")]
        public void GetIconClass_ValidCallStatusCodes_ShouldReturnCorrectFontAwesomeClass(CallStatusCode statusCode, string expectedClass)
        {
            // Act
            var result = statusCode.GetIconClass();

            // Assert
            result.Should().Be(expectedClass, $"because {statusCode} should map to Font Awesome class {expectedClass}");
        }

        [Fact]
        public void GetIconClass_UnknownCallStatusCode_ShouldReturnQuestionCircle()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetIconClass();

            // Assert
            result.Should().Be("fa-question-circle", "because unknown call status codes should return question mark icon");
        }

        #endregion

        #region GetPriority Tests

        [Theory]
        [InlineData(CallStatusCode.InternalError, 1)]
        [InlineData(CallStatusCode.Unavailable, 1)]
        [InlineData(CallStatusCode.Unauthenticated, 2)]
        [InlineData(CallStatusCode.PermissionDenied, 2)]
        [InlineData(CallStatusCode.InvalidArgument, 3)]
        [InlineData(CallStatusCode.NotFound, 3)]
        [InlineData(CallStatusCode.NotImplemented, 4)]
        [InlineData(CallStatusCode.Cancelled, 4)]
        [InlineData(CallStatusCode.Ok, 5)]
        public void GetPriority_ValidCallStatusCodes_ShouldReturnCorrectPriority(CallStatusCode statusCode, int expectedPriority)
        {
            // Act
            var result = statusCode.GetPriority();

            // Assert
            result.Should().Be(expectedPriority, $"because {statusCode} should have priority {expectedPriority} (1=highest, 5=lowest)");
        }

        [Fact]
        public void GetPriority_UnknownCallStatusCode_ShouldReturnDefaultPriority()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetPriority();

            // Assert
            result.Should().Be(3, "because unknown call status codes should return default priority 3");
        }

        #endregion

        #region ToHttpStatusCode Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, HttpStatusCode.OK)]
        [InlineData(CallStatusCode.Cancelled, HttpStatusCode.RequestTimeout)]
        [InlineData(CallStatusCode.InvalidArgument, HttpStatusCode.BadRequest)]
        [InlineData(CallStatusCode.NotFound, HttpStatusCode.NotFound)]
        [InlineData(CallStatusCode.AlreadyExists, HttpStatusCode.Conflict)]
        [InlineData(CallStatusCode.PermissionDenied, HttpStatusCode.Forbidden)]
        [InlineData(CallStatusCode.Unauthenticated, HttpStatusCode.Unauthorized)]
        [InlineData(CallStatusCode.InternalError, HttpStatusCode.InternalServerError)]
        [InlineData(CallStatusCode.Unavailable, HttpStatusCode.ServiceUnavailable)]
        [InlineData(CallStatusCode.NotImplemented, HttpStatusCode.NotImplemented)]
        public void ToHttpStatusCode_ValidCallStatusCodes_ShouldReturnCorrectHttpStatusCode(CallStatusCode statusCode, HttpStatusCode expectedStatusCode)
        {
            // Act
            var result = statusCode.ToHttpStatusCode();

            // Assert
            result.Should().Be(expectedStatusCode, $"because {statusCode} should map to HTTP status {expectedStatusCode}");
        }

        [Fact]
        public void ToHttpStatusCode_UnknownCallStatusCode_ShouldReturnInternalServerError()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.ToHttpStatusCode();

            // Assert
            result.Should().Be(HttpStatusCode.InternalServerError, "because unknown call status codes should default to InternalServerError");
        }

        #endregion

        #region GetResultCategory Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, ResultCategory.Success)]
        [InlineData(CallStatusCode.Cancelled, ResultCategory.Cancelled)]
        [InlineData(CallStatusCode.InvalidArgument, ResultCategory.ClientError)]
        [InlineData(CallStatusCode.NotFound, ResultCategory.ClientError)]
        [InlineData(CallStatusCode.Unauthenticated, ResultCategory.ClientError)]
        [InlineData(CallStatusCode.InternalError, ResultCategory.ServerError)]
        [InlineData(CallStatusCode.Unavailable, ResultCategory.ServerError)]
        [InlineData(CallStatusCode.NotImplemented, ResultCategory.ServerError)]
        public void GetResultCategory_ValidCallStatusCodes_ShouldReturnCorrectCategory(CallStatusCode statusCode, ResultCategory expectedCategory)
        {
            // Act
            var result = statusCode.GetResultCategory();

            // Assert
            result.Should().Be(expectedCategory, $"because {statusCode} should be categorized as {expectedCategory}");
        }

        #endregion

        #region GetUserActionSuggestion Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "Operation completed successfully.")]
        [InlineData(CallStatusCode.InvalidArgument, "Please check your input and try again.")]
        [InlineData(CallStatusCode.NotFound, "The requested resource could not be found. Please verify the information and try again.")]
        [InlineData(CallStatusCode.Unauthenticated, "Please log in to continue.")]
        [InlineData(CallStatusCode.PermissionDenied, "You don't have permission to perform this action. Please contact an administrator.")]
        public void GetUserActionSuggestion_ValidCallStatusCodes_ShouldReturnHelpfulSuggestion(CallStatusCode statusCode, string expectedSuggestion)
        {
            // Act
            var result = statusCode.GetUserActionSuggestion();

            // Assert
            result.Should().Be(expectedSuggestion, $"because {statusCode} should provide helpful user guidance");
        }

        [Fact]
        public void GetUserActionSuggestion_UnknownCallStatusCode_ShouldReturnGenericSuggestion()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetUserActionSuggestion();

            // Assert
            result.Should().Be("Please try again or contact support if the problem persists.", "because unknown call status codes should provide generic guidance");
        }

        #endregion

        #region GetDeveloperActionSuggestion Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "No action required.")]
        [InlineData(CallStatusCode.InvalidArgument, "Validate input parameters and request structure.")]
        [InlineData(CallStatusCode.NotFound, "Verify resource existence and access patterns.")]
        [InlineData(CallStatusCode.Unauthenticated, "Check authentication middleware and token validation.")]
        [InlineData(CallStatusCode.InternalError, "Check logs for exceptions and system state.")]
        public void GetDeveloperActionSuggestion_ValidCallStatusCodes_ShouldReturnTechnicalGuidance(CallStatusCode statusCode, string expectedSuggestion)
        {
            // Act
            var result = statusCode.GetDeveloperActionSuggestion();

            // Assert
            result.Should().Be(expectedSuggestion, $"because {statusCode} should provide technical guidance for developers");
        }

        [Fact]
        public void GetDeveloperActionSuggestion_UnknownCallStatusCode_ShouldReturnGenericGuidance()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetDeveloperActionSuggestion();

            // Assert
            result.Should().Be("Investigate the specific error context and implement appropriate handling.", "because unknown call status codes should provide generic developer guidance");
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<CallStatusCode> GetAllCallStatusCodes()
        {
            return Enum.GetValues<CallStatusCode>();
        }

        private static IEnumerable<CallStatusCode> GetClientErrorCallStatusCodes()
        {
            return
            [
                CallStatusCode.InvalidArgument,
                CallStatusCode.NotFound,
                CallStatusCode.AlreadyExists,
                CallStatusCode.PermissionDenied,
                CallStatusCode.Unauthenticated,
                CallStatusCode.FailedPrecondition,
                CallStatusCode.OutOfRange
            ];
        }

        private static IEnumerable<CallStatusCode> GetServerErrorCallStatusCodes()
        {
            return
            [
                CallStatusCode.InternalError,
                CallStatusCode.NotImplemented,
                CallStatusCode.Unavailable,
                CallStatusCode.ResourceExhausted,
                CallStatusCode.OperationTimeOut,
                CallStatusCode.Aborted
            ];
        }

        #endregion

        #region GetUserMessage Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, "Success")]
        [InlineData(CallStatusCode.Cancelled, "Operation was cancelled")]
        [InlineData(CallStatusCode.InvalidArgument, "Please check your input and try again")]
        [InlineData(CallStatusCode.NotFound, "The requested item was not found")]
        [InlineData(CallStatusCode.AlreadyExists, "This item already exists")]
        [InlineData(CallStatusCode.PermissionDenied, "You don't have permission to perform this action")]
        [InlineData(CallStatusCode.Unauthenticated, "Please log in to continue")]
        [InlineData(CallStatusCode.ResourceExhausted, "Resource limit exceeded. Please try again later")]
        [InlineData(CallStatusCode.FailedPrecondition, "Operation cannot be completed due to current conditions")]
        [InlineData(CallStatusCode.Aborted, "Operation was interrupted. Please try again")]
        [InlineData(CallStatusCode.OutOfRange, "The provided value is outside the acceptable range")]
        [InlineData(CallStatusCode.NotImplemented, "This feature is not yet available")]
        [InlineData(CallStatusCode.InternalError, "Something went wrong. Please try again later")]
        [InlineData(CallStatusCode.Unavailable, "Service is temporarily unavailable")]
        [InlineData(CallStatusCode.OperationTimeOut, "The operation took too long to complete")]
        [InlineData(CallStatusCode.Error, "An error occurred")]
        public void GetUserMessage_ValidCallStatusCodes_ShouldReturnCorrectUserFriendlyMessage(CallStatusCode statusCode, string expectedMessage)
        {
            // Act
            var result = statusCode.GetUserMessage();

            // Assert
            result.Should().Be(expectedMessage, $"because {statusCode} should have the correct user-friendly message");
            result.Should().NotBeNullOrEmpty("because all status codes should have meaningful user messages");
        }

        [Fact]
        public void GetUserMessage_UnknownCallStatusCode_ShouldReturnGenericMessage()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetUserMessage();

            // Assert
            result.Should().Be("Please try again or contact support if the problem persists", "because unknown status codes should return a generic helpful message");
        }

        [Fact]
        public void GetUserMessage_AllDefinedStatusCodes_ShouldHaveMessages()
        {
            // Arrange
            var allStatusCodes = Enum.GetValues<CallStatusCode>();

            // Act & Assert
            foreach (var statusCode in allStatusCodes)
            {
                var message = statusCode.GetUserMessage();
                message.Should().NotBeNullOrEmpty($"because {statusCode} should have a user-friendly message");
                message.Should().NotContain("Success!", "because 'Success!' should be changed to 'Success'");
            }
        }

        [Fact]
        public void GetUserMessage_SuccessStatusCode_ShouldNotContainExclamation()
        {
            // Act
            var result = CallStatusCode.Ok.GetUserMessage();

            // Assert
            result.Should().Be("Success", "because success message should be 'Success' without exclamation mark");
            result.Should().NotBe("Success!", "because exclamation mark should be removed");
        }

        #endregion

        #region GetHttpStatusCodeValue Tests

        [Theory]
        [InlineData(CallStatusCode.Ok, 200)]
        [InlineData(CallStatusCode.Cancelled, 408)]
        [InlineData(CallStatusCode.InvalidArgument, 400)]
        [InlineData(CallStatusCode.NotFound, 404)]
        [InlineData(CallStatusCode.AlreadyExists, 409)]
        [InlineData(CallStatusCode.PermissionDenied, 403)]
        [InlineData(CallStatusCode.Unauthenticated, 401)]
        [InlineData(CallStatusCode.InternalError, 500)]
        [InlineData(CallStatusCode.Unavailable, 503)]
        [InlineData(CallStatusCode.NotImplemented, 501)]
        [InlineData(CallStatusCode.ResourceExhausted, 429)]
        [InlineData(CallStatusCode.FailedPrecondition, 412)]
        [InlineData(CallStatusCode.Aborted, 409)]
        [InlineData(CallStatusCode.OutOfRange, 400)]
        [InlineData(CallStatusCode.OperationTimeOut, 408)]
        public void GetHttpStatusCodeValue_ValidCallStatusCodes_ShouldReturnCorrectHttpStatusCodeValue(CallStatusCode statusCode, int expectedHttpValue)
        {
            // Act
            var result = statusCode.GetHttpStatusCodeValue();

            // Assert
            result.Should().Be(expectedHttpValue, $"because {statusCode} should map to HTTP status {expectedHttpValue}");
            result.Should().BeGreaterOrEqualTo(200, "because HTTP status codes should be valid");
            result.Should().BeLessOrEqualTo(599, "because HTTP status codes should be in valid range");
        }

        [Fact]
        public void GetHttpStatusCodeValue_UnknownCallStatusCode_ShouldReturnInternalServerError()
        {
            // Arrange
            var unknownStatusCode = (CallStatusCode)999999;

            // Act
            var result = unknownStatusCode.GetHttpStatusCodeValue();

            // Assert
            result.Should().Be(500, "because unknown status codes should default to Internal Server Error (500)");
        }

        [Fact]
        public void GetHttpStatusCodeValue_AllDefinedStatusCodes_ShouldReturnValidHttpCodes()
        {
            // Arrange
            var allStatusCodes = Enum.GetValues<CallStatusCode>();

            // Act & Assert
            foreach (var statusCode in allStatusCodes)
            {
                var httpValue = statusCode.GetHttpStatusCodeValue();
                httpValue.Should().BeGreaterOrEqualTo(200, $"because {statusCode} should map to a valid HTTP status code");
                httpValue.Should().BeLessOrEqualTo(599, $"because {statusCode} should map to a valid HTTP status code");

                // Verify consistency with ToHttpStatusCode method
                var expectedValue = (int)statusCode.ToHttpStatusCode();
                httpValue.Should().Be(expectedValue, $"because GetHttpStatusCodeValue should be consistent with ToHttpStatusCode for {statusCode}");
            }
        }

        [Fact]
        public void GetHttpStatusCodeValue_ShouldBeConsistentWithToHttpStatusCode()
        {
            // Arrange
            var testStatusCodes = new[]
            {
                CallStatusCode.Ok,
                CallStatusCode.InvalidArgument,
                CallStatusCode.NotFound,
                CallStatusCode.InternalError,
                CallStatusCode.Unauthenticated,
                CallStatusCode.PermissionDenied
            };

            // Act & Assert
            foreach (var statusCode in testStatusCodes)
            {
                var valueFromNewMethod = statusCode.GetHttpStatusCodeValue();
                var valueFromExistingMethod = (int)statusCode.ToHttpStatusCode();

                valueFromNewMethod.Should().Be(valueFromExistingMethod, 
                    $"because GetHttpStatusCodeValue should return the same integer value as casting ToHttpStatusCode for {statusCode}");
            }
        }

        #endregion

        #region Integration Tests for New Methods

        [Fact]
        public void NewExtensionMethods_ShouldWorkInPracticalScenarios()
        {
            // Arrange
            var testScenarios = new[]
            {
                new { Status = CallStatusCode.Ok, ExpectedMessage = "Success", ExpectedHttpCode = 200 },
                new { Status = CallStatusCode.NotFound, ExpectedMessage = "The requested item was not found", ExpectedHttpCode = 404 },
                new { Status = CallStatusCode.InternalError, ExpectedMessage = "Something went wrong. Please try again later", ExpectedHttpCode = 500 },
                new { Status = CallStatusCode.Unauthenticated, ExpectedMessage = "Please log in to continue", ExpectedHttpCode = 401 }
            };

            // Act & Assert
            foreach (var scenario in testScenarios)
            {
                // Test user message
                var userMessage = scenario.Status.GetUserMessage();
                userMessage.Should().Be(scenario.ExpectedMessage, $"because {scenario.Status} should have correct user message");

                // Test HTTP status code value
                var httpCode = scenario.Status.GetHttpStatusCodeValue();
                httpCode.Should().Be(scenario.ExpectedHttpCode, $"because {scenario.Status} should have correct HTTP code");

                // Verify both methods work together
                userMessage.Should().NotBeNullOrEmpty("because user message should always be meaningful");
                httpCode.Should().BeGreaterThan(0, "because HTTP code should always be positive");
            }
        }

        [Fact]
        public void NewExtensionMethods_ShouldWorkWithErrorMessageClass()
        {
            // Arrange
            var errorMessage = new ErrorMessage { StatusCode = CallStatusCode.InvalidArgument };

            // Act
            var userMessage = errorMessage.StatusCode.GetUserMessage();
            var httpCodeValue = errorMessage.StatusCode.GetHttpStatusCodeValue();

            // Assert
            userMessage.Should().Be("Please check your input and try again", "because InvalidArgument should have helpful user message");
            httpCodeValue.Should().Be(400, "because InvalidArgument should map to HTTP 400");

            // Verify consistency with existing methods
            errorMessage.HttpStatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "because existing method should be consistent");
            httpCodeValue.Should().Be((int)errorMessage.HttpStatusCode, "because new method should match existing method");
        }

        #endregion
    }
}