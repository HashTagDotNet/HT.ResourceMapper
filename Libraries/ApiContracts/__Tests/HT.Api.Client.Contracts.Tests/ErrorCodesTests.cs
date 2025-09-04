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
    [Trait("Category", "HT/Api/Client/Contracts/Models/ErrorCodes")]
    public class ErrorCodesTests
    {
        #region Enum Value Tests

        [Fact]
        public void ErrorCodes_EnumValues_ShouldHaveCorrectValues()
        {
            // Arrange & Act & Assert
            ((int)ErrorCodes.Ok).Should().Be(0, "because Ok represents success state");
            ((int)ErrorCodes.Cancelled).Should().Be(1, "because Cancelled is a special non-error state");
            ((int)ErrorCodes.Error).Should().Be(8, "because Error is the base flag (2 << 2)");
            
            // Error codes with bit masks
            ((int)ErrorCodes.InvalidArgument).Should().Be(24, "because InvalidArgument = Error(8) | 2<<3(16)");
            ((int)ErrorCodes.OperationTimeOut).Should().Be(40, "because OperationTimeOut = Error(8) | 2<<4(32)");
            ((int)ErrorCodes.NotFound).Should().Be(72, "because NotFound = Error(8) | 2<<5(64)");
            ((int)ErrorCodes.AlreadyExists).Should().Be(136, "because AlreadyExists = Error(8) | 2<<6(128)");
            ((int)ErrorCodes.PermissionDenied).Should().Be(264, "because PermissionDenied = Error(8) | 2<<7(256)");
            ((int)ErrorCodes.ResourceExhausted).Should().Be(520, "because ResourceExhausted = Error(8) | 2<<8(512)");
            ((int)ErrorCodes.FailedPrecondition).Should().Be(1032, "because FailedPrecondition = Error(8) | 2<<9(1024)");
            ((int)ErrorCodes.Aborted).Should().Be(2056, "because Aborted = Error(8) | 2<<10(2048)");
            ((int)ErrorCodes.OutOfRange).Should().Be(4104, "because OutOfRange = Error(8) | 2<<11(4096)");
            ((int)ErrorCodes.NotImplemented).Should().Be(8200, "because NotImplemented = Error(8) | 2<<12(8192)");
            ((int)ErrorCodes.InternalError).Should().Be(16392, "because InternalError = Error(8) | 2<<13(16384)");
            ((int)ErrorCodes.Unavailable).Should().Be(32776, "because Unavailable = Error(8) | 2<<14(32768)");
            ((int)ErrorCodes.Unauthenticated).Should().Be(65544, "because Unauthenticated = Error(8) | 2<<15(65536)");
        }

        [Fact]
        public void ErrorCodes_AllValues_ShouldBeUnique()
        {
            // Arrange
            var allValues = Enum.GetValues<ErrorCodes>().Select(e => (int)e).ToList();
            var uniqueValues = allValues.Distinct().ToList();

            // Act & Assert
            allValues.Should().HaveCount(uniqueValues.Count, "because all error codes should have unique values");
        }

        #endregion

        #region IsSuccess Tests

        [Fact]
        public void IsSuccess_Ok_ShouldReturnTrue()
        {
            // Arrange
            var errorCode = ErrorCodes.Ok;

            // Act
            var result = errorCode.IsSuccess();

            // Assert
            result.Should().BeTrue("because Ok indicates successful operation");
        }

        [Theory]
        [InlineData(ErrorCodes.Cancelled)]
        [InlineData(ErrorCodes.Error)]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        [InlineData(ErrorCodes.InternalError)]
        [InlineData(ErrorCodes.Unauthenticated)]
        public void IsSuccess_NonOkErrorCodes_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsSuccess();

            // Assert
            result.Should().BeFalse($"because {errorCode} does not indicate success");
        }

        #endregion

        #region IsFailure Tests

        [Theory]
        [InlineData(ErrorCodes.Ok)]
        [InlineData(ErrorCodes.Cancelled)]
        [InlineData(ErrorCodes.Error)]
        public void IsFailure_NonFailureStates_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsFailure();

            // Assert
            result.Should().BeFalse($"because {errorCode} should not be considered a failure state");
        }

        [Theory]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        [InlineData(ErrorCodes.InternalError)]
        [InlineData(ErrorCodes.Unauthenticated)]
        [InlineData(ErrorCodes.PermissionDenied)]
        [InlineData(ErrorCodes.ResourceExhausted)]
        [InlineData(ErrorCodes.FailedPrecondition)]
        [InlineData(ErrorCodes.Aborted)]
        [InlineData(ErrorCodes.OutOfRange)]
        [InlineData(ErrorCodes.NotImplemented)]
        [InlineData(ErrorCodes.Unavailable)]
        [InlineData(ErrorCodes.OperationTimeOut)]
        public void IsFailure_ErrorCodes_ShouldReturnTrue(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsFailure();

            // Assert
            result.Should().BeTrue($"because {errorCode} represents a failure state");
        }

        #endregion

        #region HasErrorFlag Tests

        [Theory]
        [InlineData(ErrorCodes.Ok)]
        [InlineData(ErrorCodes.Cancelled)]
        public void HasErrorFlag_NonErrorStates_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.HasErrorFlag();

            // Assert
            result.Should().BeFalse($"because {errorCode} should not have the error flag set");
        }

        [Theory]
        [InlineData(ErrorCodes.Error)]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        [InlineData(ErrorCodes.InternalError)]
        [InlineData(ErrorCodes.Unauthenticated)]
        public void HasErrorFlag_ErrorStates_ShouldReturnTrue(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.HasErrorFlag();

            // Assert
            result.Should().BeTrue($"because {errorCode} should have the error flag set");
        }

        #endregion

        #region IsCancelled Tests

        [Fact]
        public void IsCancelled_Cancelled_ShouldReturnTrue()
        {
            // Arrange
            var errorCode = ErrorCodes.Cancelled;

            // Act
            var result = errorCode.IsCancelled();

            // Assert
            result.Should().BeTrue("because Cancelled state should be identified correctly");
        }

        [Theory]
        [InlineData(ErrorCodes.Ok)]
        [InlineData(ErrorCodes.Error)]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        public void IsCancelled_NonCancelledStates_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsCancelled();

            // Assert
            result.Should().BeFalse($"because {errorCode} is not a cancelled state");
        }

        #endregion

        #region GetSpecificErrorBits Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, 0)]
        [InlineData(ErrorCodes.Cancelled, 0)]
        public void GetSpecificErrorBits_NonErrorStates_ShouldReturnZero(ErrorCodes errorCode, int expectedBits)
        {
            // Act
            var result = errorCode.GetSpecificErrorBits();

            // Assert
            result.Should().Be(expectedBits, $"because {errorCode} has no specific error bits");
        }

        [Theory]
        [InlineData(ErrorCodes.Error, 0)]
        [InlineData(ErrorCodes.InvalidArgument, 16)]
        [InlineData(ErrorCodes.OperationTimeOut, 32)]
        [InlineData(ErrorCodes.NotFound, 64)]
        [InlineData(ErrorCodes.AlreadyExists, 128)]
        public void GetSpecificErrorBits_ErrorStates_ShouldReturnCorrectBits(ErrorCodes errorCode, int expectedBits)
        {
            // Act
            var result = errorCode.GetSpecificErrorBits();

            // Assert
            result.Should().Be(expectedBits, $"because {errorCode} should have specific error bits {expectedBits}");
        }

        #endregion

        #region VerifyBitMask Tests

        [Fact]
        public void VerifyBitMask_AllErrorCodes_ShouldReturnTrue()
        {
            // Arrange
            var allErrorCodes = Enum.GetValues<ErrorCodes>();

            // Act & Assert
            foreach (var errorCode in allErrorCodes)
            {
                var result = errorCode.VerifyBitMask();
                result.Should().BeTrue($"because bit mask verification should pass for {errorCode}");
            }
        }

        #endregion

        #region GetBitMaskAnalysis Tests

        [Fact]
        public void GetBitMaskAnalysis_InvalidArgument_ShouldReturnCorrectAnalysis()
        {
            // Arrange
            var errorCode = ErrorCodes.InvalidArgument;

            // Act
            var result = errorCode.GetBitMaskAnalysis();

            // Assert
            result.Should().Contain("InvalidArgument", "because the analysis should include the error code name");
            result.Should().Contain("Value: 24", "because InvalidArgument has value 24");
            result.Should().Contain("HasErrorFlag: True", "because InvalidArgument has the error flag");
            result.Should().Contain("SpecificBits: 16", "because InvalidArgument has specific bits 16");
        }

        [Fact]
        public void GetBitMaskAnalysis_Ok_ShouldReturnCorrectAnalysis()
        {
            // Arrange
            var errorCode = ErrorCodes.Ok;

            // Act
            var result = errorCode.GetBitMaskAnalysis();

            // Assert
            result.Should().Contain("Ok", "because the analysis should include the error code name");
            result.Should().Contain("Value: 0", "because Ok has value 0");
            result.Should().Contain("HasErrorFlag: False", "because Ok does not have the error flag");
            result.Should().Contain("SpecificBits: 0", "because Ok has no specific bits");
        }

        #endregion

        #region IsClientError Tests

        [Theory]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        [InlineData(ErrorCodes.AlreadyExists)]
        [InlineData(ErrorCodes.PermissionDenied)]
        [InlineData(ErrorCodes.Unauthenticated)]
        [InlineData(ErrorCodes.FailedPrecondition)]
        [InlineData(ErrorCodes.OutOfRange)]
        public void IsClientError_ClientErrorCodes_ShouldReturnTrue(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsClientError();

            // Assert
            result.Should().BeTrue($"because {errorCode} is a client error (4xx equivalent)");
        }

        [Theory]
        [InlineData(ErrorCodes.Ok)]
        [InlineData(ErrorCodes.Cancelled)]
        [InlineData(ErrorCodes.InternalError)]
        [InlineData(ErrorCodes.Unavailable)]
        [InlineData(ErrorCodes.NotImplemented)]
        public void IsClientError_NonClientErrorCodes_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsClientError();

            // Assert
            result.Should().BeFalse($"because {errorCode} is not a client error");
        }

        #endregion

        #region IsServerError Tests

        [Theory]
        [InlineData(ErrorCodes.InternalError)]
        [InlineData(ErrorCodes.NotImplemented)]
        [InlineData(ErrorCodes.Unavailable)]
        [InlineData(ErrorCodes.ResourceExhausted)]
        [InlineData(ErrorCodes.OperationTimeOut)]
        [InlineData(ErrorCodes.Aborted)]
        public void IsServerError_ServerErrorCodes_ShouldReturnTrue(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsServerError();

            // Assert
            result.Should().BeTrue($"because {errorCode} is a server error (5xx equivalent)");
        }

        [Theory]
        [InlineData(ErrorCodes.Ok)]
        [InlineData(ErrorCodes.Cancelled)]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        [InlineData(ErrorCodes.Unauthenticated)]
        public void IsServerError_NonServerErrorCodes_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsServerError();

            // Assert
            result.Should().BeFalse($"because {errorCode} is not a server error");
        }

        #endregion

        #region IsRetryable Tests

        [Theory]
        [InlineData(ErrorCodes.Unavailable)]
        [InlineData(ErrorCodes.ResourceExhausted)]
        [InlineData(ErrorCodes.OperationTimeOut)]
        [InlineData(ErrorCodes.Aborted)]
        [InlineData(ErrorCodes.InternalError)]
        public void IsRetryable_RetryableErrorCodes_ShouldReturnTrue(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsRetryable();

            // Assert
            result.Should().BeTrue($"because {errorCode} represents a retryable error condition");
        }

        [Theory]
        [InlineData(ErrorCodes.Ok)]
        [InlineData(ErrorCodes.Cancelled)]
        [InlineData(ErrorCodes.InvalidArgument)]
        [InlineData(ErrorCodes.NotFound)]
        [InlineData(ErrorCodes.Unauthenticated)]
        [InlineData(ErrorCodes.PermissionDenied)]
        public void IsRetryable_NonRetryableErrorCodes_ShouldReturnFalse(ErrorCodes errorCode)
        {
            // Act
            var result = errorCode.IsRetryable();

            // Assert
            result.Should().BeFalse($"because {errorCode} does not represent a retryable condition");
        }

        #endregion

        #region GetDescription Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, "Operation completed successfully")]
        [InlineData(ErrorCodes.Cancelled, "Operation was cancelled")]
        [InlineData(ErrorCodes.InvalidArgument, "Invalid input provided")]
        [InlineData(ErrorCodes.NotFound, "Resource not found")]
        [InlineData(ErrorCodes.InternalError, "Internal server error")]
        [InlineData(ErrorCodes.Unauthenticated, "Authentication required")]
        public void GetDescription_ValidErrorCodes_ShouldReturnCorrectDescription(ErrorCodes errorCode, string expectedDescription)
        {
            // Act
            var result = errorCode.GetDescription();

            // Assert
            result.Should().Be(expectedDescription, $"because {errorCode} should have the correct description");
        }

        [Fact]
        public void GetDescription_UnknownErrorCode_ShouldReturnUnknownError()
        {
            // Arrange
            var unknownErrorCode = (ErrorCodes)999999;

            // Act
            var result = unknownErrorCode.GetDescription();

            // Assert
            result.Should().Be("Unknown error", "because unknown error codes should return default message");
        }

        #endregion

        #region GetSeverityClass Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, "success")]
        [InlineData(ErrorCodes.Cancelled, "secondary")]
        [InlineData(ErrorCodes.InvalidArgument, "warning")]
        [InlineData(ErrorCodes.NotFound, "warning")]
        [InlineData(ErrorCodes.AlreadyExists, "info")]
        [InlineData(ErrorCodes.PermissionDenied, "danger")]
        [InlineData(ErrorCodes.Unauthenticated, "danger")]
        [InlineData(ErrorCodes.InternalError, "danger")]
        public void GetSeverityClass_ValidErrorCodes_ShouldReturnCorrectBootstrapClass(ErrorCodes errorCode, string expectedClass)
        {
            // Act
            var result = errorCode.GetSeverityClass();

            // Assert
            result.Should().Be(expectedClass, $"because {errorCode} should map to Bootstrap class {expectedClass}");
        }

        #endregion

        #region GetAlertClass Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, "alert-success")]
        [InlineData(ErrorCodes.InvalidArgument, "alert-warning")]
        [InlineData(ErrorCodes.InternalError, "alert-danger")]
        [InlineData(ErrorCodes.Cancelled, "alert-secondary")]
        public void GetAlertClass_ValidErrorCodes_ShouldReturnCorrectAlertClass(ErrorCodes errorCode, string expectedClass)
        {
            // Act
            var result = errorCode.GetAlertClass();

            // Assert
            result.Should().Be(expectedClass, $"because {errorCode} should map to alert class {expectedClass}");
        }

        #endregion

        #region GetIconClass Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, "fa-check-circle")]
        [InlineData(ErrorCodes.Cancelled, "fa-times-circle")]
        [InlineData(ErrorCodes.InvalidArgument, "fa-exclamation-triangle")]
        [InlineData(ErrorCodes.NotFound, "fa-search")]
        [InlineData(ErrorCodes.PermissionDenied, "fa-lock")]
        [InlineData(ErrorCodes.Unauthenticated, "fa-user-slash")]
        [InlineData(ErrorCodes.InternalError, "fa-bug")]
        public void GetIconClass_ValidErrorCodes_ShouldReturnCorrectFontAwesomeClass(ErrorCodes errorCode, string expectedClass)
        {
            // Act
            var result = errorCode.GetIconClass();

            // Assert
            result.Should().Be(expectedClass, $"because {errorCode} should map to Font Awesome class {expectedClass}");
        }

        [Fact]
        public void GetIconClass_UnknownErrorCode_ShouldReturnQuestionCircle()
        {
            // Arrange
            var unknownErrorCode = (ErrorCodes)999999;

            // Act
            var result = unknownErrorCode.GetIconClass();

            // Assert
            result.Should().Be("fa-question-circle", "because unknown error codes should return question mark icon");
        }

        #endregion

        #region GetPriority Tests

        [Theory]
        [InlineData(ErrorCodes.InternalError, 1)]
        [InlineData(ErrorCodes.Unavailable, 1)]
        [InlineData(ErrorCodes.Unauthenticated, 2)]
        [InlineData(ErrorCodes.PermissionDenied, 2)]
        [InlineData(ErrorCodes.InvalidArgument, 3)]
        [InlineData(ErrorCodes.NotFound, 3)]
        [InlineData(ErrorCodes.NotImplemented, 4)]
        [InlineData(ErrorCodes.Cancelled, 4)]
        [InlineData(ErrorCodes.Ok, 5)]
        public void GetPriority_ValidErrorCodes_ShouldReturnCorrectPriority(ErrorCodes errorCode, int expectedPriority)
        {
            // Act
            var result = errorCode.GetPriority();

            // Assert
            result.Should().Be(expectedPriority, $"because {errorCode} should have priority {expectedPriority} (1=highest, 5=lowest)");
        }

        [Fact]
        public void GetPriority_UnknownErrorCode_ShouldReturnDefaultPriority()
        {
            // Arrange
            var unknownErrorCode = (ErrorCodes)999999;

            // Act
            var result = unknownErrorCode.GetPriority();

            // Assert
            result.Should().Be(3, "because unknown error codes should return default priority 3");
        }

        #endregion

        #region ToHttpStatusCode Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, HttpStatusCode.OK)]
        [InlineData(ErrorCodes.Cancelled, HttpStatusCode.RequestTimeout)]
        [InlineData(ErrorCodes.InvalidArgument, HttpStatusCode.BadRequest)]
        [InlineData(ErrorCodes.NotFound, HttpStatusCode.NotFound)]
        [InlineData(ErrorCodes.AlreadyExists, HttpStatusCode.Conflict)]
        [InlineData(ErrorCodes.PermissionDenied, HttpStatusCode.Forbidden)]
        [InlineData(ErrorCodes.Unauthenticated, HttpStatusCode.Unauthorized)]
        [InlineData(ErrorCodes.InternalError, HttpStatusCode.InternalServerError)]
        [InlineData(ErrorCodes.Unavailable, HttpStatusCode.ServiceUnavailable)]
        [InlineData(ErrorCodes.NotImplemented, HttpStatusCode.NotImplemented)]
        public void ToHttpStatusCode_ValidErrorCodes_ShouldReturnCorrectHttpStatusCode(ErrorCodes errorCode, HttpStatusCode expectedStatusCode)
        {
            // Act
            var result = errorCode.ToHttpStatusCode();

            // Assert
            result.Should().Be(expectedStatusCode, $"because {errorCode} should map to HTTP status {expectedStatusCode}");
        }

        [Fact]
        public void ToHttpStatusCode_UnknownErrorCode_ShouldReturnInternalServerError()
        {
            // Arrange
            var unknownErrorCode = (ErrorCodes)999999;

            // Act
            var result = unknownErrorCode.ToHttpStatusCode();

            // Assert
            result.Should().Be(HttpStatusCode.InternalServerError, "because unknown error codes should default to InternalServerError");
        }

        #endregion

        #region GetResultCategory Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, ResultCategory.Success)]
        [InlineData(ErrorCodes.Cancelled, ResultCategory.Cancelled)]
        [InlineData(ErrorCodes.InvalidArgument, ResultCategory.ClientError)]
        [InlineData(ErrorCodes.NotFound, ResultCategory.ClientError)]
        [InlineData(ErrorCodes.Unauthenticated, ResultCategory.ClientError)]
        [InlineData(ErrorCodes.InternalError, ResultCategory.ServerError)]
        [InlineData(ErrorCodes.Unavailable, ResultCategory.ServerError)]
        [InlineData(ErrorCodes.NotImplemented, ResultCategory.ServerError)]
        public void GetResultCategory_ValidErrorCodes_ShouldReturnCorrectCategory(ErrorCodes errorCode, ResultCategory expectedCategory)
        {
            // Act
            var result = errorCode.GetResultCategory();

            // Assert
            result.Should().Be(expectedCategory, $"because {errorCode} should be categorized as {expectedCategory}");
        }

        #endregion

        #region GetUserActionSuggestion Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, "Operation completed successfully.")]
        [InlineData(ErrorCodes.InvalidArgument, "Please check your input and try again.")]
        [InlineData(ErrorCodes.NotFound, "The requested resource could not be found. Please verify the information and try again.")]
        [InlineData(ErrorCodes.Unauthenticated, "Please log in to continue.")]
        [InlineData(ErrorCodes.PermissionDenied, "You don't have permission to perform this action. Please contact an administrator.")]
        public void GetUserActionSuggestion_ValidErrorCodes_ShouldReturnHelpfulSuggestion(ErrorCodes errorCode, string expectedSuggestion)
        {
            // Act
            var result = errorCode.GetUserActionSuggestion();

            // Assert
            result.Should().Be(expectedSuggestion, $"because {errorCode} should provide helpful user guidance");
        }

        [Fact]
        public void GetUserActionSuggestion_UnknownErrorCode_ShouldReturnGenericSuggestion()
        {
            // Arrange
            var unknownErrorCode = (ErrorCodes)999999;

            // Act
            var result = unknownErrorCode.GetUserActionSuggestion();

            // Assert
            result.Should().Be("Please try again or contact support if the problem persists.", "because unknown error codes should provide generic guidance");
        }

        #endregion

        #region GetDeveloperActionSuggestion Tests

        [Theory]
        [InlineData(ErrorCodes.Ok, "No action required.")]
        [InlineData(ErrorCodes.InvalidArgument, "Validate input parameters and request structure.")]
        [InlineData(ErrorCodes.NotFound, "Verify resource existence and access patterns.")]
        [InlineData(ErrorCodes.Unauthenticated, "Check authentication middleware and token validation.")]
        [InlineData(ErrorCodes.InternalError, "Check logs for exceptions and system state.")]
        public void GetDeveloperActionSuggestion_ValidErrorCodes_ShouldReturnTechnicalGuidance(ErrorCodes errorCode, string expectedSuggestion)
        {
            // Act
            var result = errorCode.GetDeveloperActionSuggestion();

            // Assert
            result.Should().Be(expectedSuggestion, $"because {errorCode} should provide technical guidance for developers");
        }

        [Fact]
        public void GetDeveloperActionSuggestion_UnknownErrorCode_ShouldReturnGenericGuidance()
        {
            // Arrange
            var unknownErrorCode = (ErrorCodes)999999;

            // Act
            var result = unknownErrorCode.GetDeveloperActionSuggestion();

            // Assert
            result.Should().Be("Investigate the specific error context and implement appropriate handling.", "because unknown error codes should provide generic developer guidance");
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<ErrorCodes> GetAllErrorCodes()
        {
            return Enum.GetValues<ErrorCodes>();
        }

        private static IEnumerable<ErrorCodes> GetClientErrorCodes()
        {
            return
            [
                ErrorCodes.InvalidArgument,
                ErrorCodes.NotFound,
                ErrorCodes.AlreadyExists,
                ErrorCodes.PermissionDenied,
                ErrorCodes.Unauthenticated,
                ErrorCodes.FailedPrecondition,
                ErrorCodes.OutOfRange
            ];
        }

        private static IEnumerable<ErrorCodes> GetServerErrorCodes()
        {
            return
            [
                ErrorCodes.InternalError,
                ErrorCodes.NotImplemented,
                ErrorCodes.Unavailable,
                ErrorCodes.ResourceExhausted,
                ErrorCodes.OperationTimeOut,
                ErrorCodes.Aborted
            ];
        }

        #endregion
    }
}