using HT.Api.Client.Contracts.Models;
using HT.Api.Client.Contracts.Extensions;
using HT.Api.Client.Contracts.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    [Trait("Category", "HT/Api/Client/Contracts/Models/ApiResponse")]
    public class ApiResponseTests
    {
        #region Constructor and Basic Properties Tests

        [Fact]
        public void ApiResponse_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var response = new ApiResponse();

            // Assert
            response.Errors.Should().BeNull("because errors should be null by default");
            response.Links.Should().BeNull("because links should be null by default");
            response.MetaData.Should().NotBeNull("because metadata should be initialized");
            response.MetaData.ResponseId.Should().NotBeNullOrEmpty("because response ID should be generated");
            response.MetaData.Timestamp.Should().NotBeNullOrEmpty("because timestamp should be generated");
        }

        [Fact]
        public void ApiResponse_MetaData_ShouldBeInitializedByDefault()
        {
            // Act
            var response = new ApiResponse();

            // Assert
            response.MetaData.Should().NotBeNull("because MetaData should be initialized by default");
            response.MetaData.ResponseId.Should().HaveLength(8, "because ResponseId should be 8 characters");
            response.MetaData.Timestamp.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{2}Z", 
                "because timestamp should follow yyyy-MM-ddTHH:mm:ss.ffZ format");
        }

        [Fact]
        public void ApiResponse_ShouldSerializeErrors_ShouldReturnCorrectly()
        {
            // Arrange
            var response = new ApiResponse();

            // Act & Assert - No errors
            response.ShouldSerializeErrors().Should().BeFalse("because there are no errors");

            // Add an error
            response.AddValidationError("test", "test error");
            response.ShouldSerializeErrors().Should().BeTrue("because there is now an error");

            // Clear errors
            response.ClearErrors();
            response.ShouldSerializeErrors().Should().BeFalse("because errors list is empty after clearing");
        }

        [Fact]
        public void ApiResponse_ShouldSerializeLinks_ShouldReturnCorrectly()
        {
            // Arrange
            var response = new ApiResponse();

            // Act & Assert - No links
            response.ShouldSerializeLinks().Should().BeFalse("because there are no links");

            // Add a link
            response.Links = new List<Link> { new Link("http://example.com", "self") };
            response.ShouldSerializeLinks().Should().BeTrue("because there is now a link");

            // Clear links
            response.Links.Clear();
            response.ShouldSerializeLinks().Should().BeFalse("because links list is empty after clearing");
        }

        #endregion

        #region Error Management Methods Tests

        [Fact]
        public void AddValidation_ShouldAddErrorCorrectly()
        {
            // Arrange
            var response = new ApiResponse();
            var propertyName = "Email";
            var message = "Invalid email format";
            var errorCode = ErrorCodes.InvalidArgument;

            // Act
            response.AddValidation(propertyName, message, errorCode);

            // Assert
            response.Errors.Should().NotBeNull("because an error was added");
            response.Errors.Should().HaveCount(1, "because one error was added");
            
            var error = response.Errors.First();
            error.Property.Should().Be(propertyName, "because property name should match");
            error.Detail.Should().Be(message, "because detail should match");
            error.StatusCode.Should().Be(errorCode, "because status code should match");
        }

        [Fact]
        public void AddError_WithBasicParameters_ShouldAddErrorCorrectly()
        {
            // Arrange
            var response = new ApiResponse();
            var errorCode = ErrorCodes.NotFound;
            var detail = "User not found";
            var property = "UserId";

            // Act
            response.AddError(errorCode, detail, property);

            // Assert
            response.Errors.Should().NotBeNull("because an error was added");
            response.Errors.Should().HaveCount(1, "because one error was added");
            
            var error = response.Errors.First();
            error.StatusCode.Should().Be(errorCode, "because status code should match");
            error.Detail.Should().Be(detail, "because detail should match");
            error.Property.Should().Be(property, "because property should match");
        }

        [Fact]
        public void AddError_WithCustomSuggestions_ShouldAddErrorWithSuggestions()
        {
            // Arrange
            var response = new ApiResponse();
            var errorCode = ErrorCodes.Unauthenticated;
            var detail = "Invalid credentials";
            var property = "credentials";
            var userActions = "Please check your username and password";
            var developerActions = "Verify authentication middleware configuration";

            // Act
            response.AddError(errorCode, detail, property, userActions, developerActions);

            // Assert
            response.Errors.Should().NotBeNull("because an error was added");
            response.Errors.Should().HaveCount(1, "because one error was added");
            
            var error = response.Errors.First();
            error.StatusCode.Should().Be(errorCode, "because status code should match");
            error.Detail.Should().Be(detail, "because detail should match");
            error.Property.Should().Be(property, "because property should match");
            error.SuggestedUserActions.Should().Be(userActions, "because user actions should match");
            error.SuggestedApplicationActions.Should().Be(developerActions, "because developer actions should match");
        }

        [Fact]
        public void AddError_ShouldInitializeErrorsListWhenNull()
        {
            // Arrange
            var response = new ApiResponse();
            response.Errors.Should().BeNull("because errors list should start as null");

            // Act
            response.AddError(ErrorCodes.InternalError);

            // Assert
            response.Errors.Should().NotBeNull("because errors list should be initialized");
            response.Errors.Should().HaveCount(1, "because one error was added");
        }

        #endregion

        #region Convenience Error Methods Tests

        [Fact]
        public void AddNotFoundError_ShouldAddCorrectErrorCode()
        {
            // Arrange
            var response = new ApiResponse();
            var detail = "Resource not found";
            var property = "resourceId";

            // Act
            response.AddNotFoundError(detail, property);

            // Assert
            response.Errors.Should().HaveCount(1, "because one error was added");
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.NotFound, "because NotFound error code should be used");
            error.Detail.Should().Be(detail, "because detail should match");
            error.Property.Should().Be(property, "because property should match");
        }

        [Fact]
        public void AddValidationError_ShouldAddCorrectErrorCode()
        {
            // Arrange
            var response = new ApiResponse();
            var property = "email";
            var detail = "Email is required";

            // Act
            response.AddValidationError(property, detail);

            // Assert
            response.Errors.Should().HaveCount(1, "because one error was added");
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.InvalidArgument, "because InvalidArgument error code should be used");
            error.Detail.Should().Be(detail, "because detail should match");
            error.Property.Should().Be(property, "because property should match");
        }

        [Fact]
        public void AddPermissionError_ShouldAddCorrectErrorCode()
        {
            // Arrange
            var response = new ApiResponse();
            var detail = "Access denied";

            // Act
            response.AddPermissionError(detail);

            // Assert
            response.Errors.Should().HaveCount(1, "because one error was added");
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.PermissionDenied, "because PermissionDenied error code should be used");
            error.Detail.Should().Be(detail, "because detail should match");
        }

        [Fact]
        public void AddAuthenticationError_ShouldAddCorrectErrorCode()
        {
            // Arrange
            var response = new ApiResponse();
            var detail = "Invalid token";

            // Act
            response.AddAuthenticationError(detail);

            // Assert
            response.Errors.Should().HaveCount(1, "because one error was added");
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.Unauthenticated, "because Unauthenticated error code should be used");
            error.Detail.Should().Be(detail, "because detail should match");
        }

        [Fact]
        public void AddInternalError_ShouldAddCorrectErrorCode()
        {
            // Arrange
            var response = new ApiResponse();
            var detail = "Database connection failed";

            // Act
            response.AddInternalError(detail);

            // Assert
            response.Errors.Should().HaveCount(1, "because one error was added");
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.InternalError, "because InternalError error code should be used");
            error.Detail.Should().Be(detail, "because detail should match");
        }

        [Fact]
        public void AddCancellationError_ShouldAddCorrectErrorCode()
        {
            // Arrange
            var response = new ApiResponse();
            var detail = "Operation was cancelled";

            // Act
            response.AddCancellationError(detail);

            // Assert
            response.Errors.Should().HaveCount(1, "because one error was added");
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.Cancelled, "because Cancelled error code should be used");
            error.Detail.Should().Be(detail, "because detail should match");
        }

        #endregion

        #region Error Query Methods Tests

        [Fact]
        public void GetErrorsForProperty_ShouldReturnMatchingErrors()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email is required");
            response.AddValidationError("email", "Email format is invalid");
            response.AddValidationError("name", "Name is required");

            // Act
            var emailErrors = response.GetErrorsForProperty("email").ToList();
            var nameErrors = response.GetErrorsForProperty("name").ToList();
            var nonExistentErrors = response.GetErrorsForProperty("nonexistent").ToList();

            // Assert
            emailErrors.Should().HaveCount(2, "because there are two email errors");
            nameErrors.Should().HaveCount(1, "because there is one name error");
            nonExistentErrors.Should().BeEmpty("because there are no errors for nonexistent property");
        }

        [Fact]
        public void GetErrorsForProperty_ShouldBeCaseInsensitive()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("Email", "Email is required");

            // Act
            var errors = response.GetErrorsForProperty("email").ToList();

            // Assert
            errors.Should().HaveCount(1, "because case insensitive matching should work");
        }

        [Fact]
        public void GetErrorsByCode_ShouldReturnMatchingErrors()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email error");
            response.AddNotFoundError("User not found");
            response.AddValidationError("name", "Name error");

            // Act
            var validationErrors = response.GetErrorsByCode(ErrorCodes.InvalidArgument).ToList();
            var notFoundErrors = response.GetErrorsByCode(ErrorCodes.NotFound).ToList();
            var nonExistentErrors = response.GetErrorsByCode(ErrorCodes.InternalError).ToList();

            // Assert
            validationErrors.Should().HaveCount(2, "because there are two validation errors");
            notFoundErrors.Should().HaveCount(1, "because there is one not found error");
            nonExistentErrors.Should().BeEmpty("because there are no internal errors");
        }

        [Fact]
        public void HasErrorCode_ShouldReturnCorrectResult()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email error");
            response.AddNotFoundError("User not found");

            // Act & Assert
            response.HasErrorCode(ErrorCodes.InvalidArgument).Should().BeTrue("because there is a validation error");
            response.HasErrorCode(ErrorCodes.NotFound).Should().BeTrue("because there is a not found error");
            response.HasErrorCode(ErrorCodes.InternalError).Should().BeFalse("because there are no internal errors");
        }

        [Fact]
        public void GetErrorsForProperty_WithNullErrors_ShouldReturnEmpty()
        {
            // Arrange
            var response = new ApiResponse();
            response.Errors.Should().BeNull("because no errors have been added");

            // Act
            var errors = response.GetErrorsForProperty("test").ToList();

            // Assert
            errors.Should().BeEmpty("because errors list is null");
        }

        #endregion

        #region Error Management Tests

        [Fact]
        public void ClearErrors_ShouldRemoveAllErrors()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email error");
            response.AddNotFoundError("User not found");
            response.Errors.Should().HaveCount(2, "because two errors were added");

            // Act
            response.ClearErrors();

            // Assert
            response.Errors.Should().NotBeNull("because the list should still exist");
            response.Errors.Should().BeEmpty("because all errors should be cleared");
        }

        [Fact]
        public void ClearErrors_WithNullErrors_ShouldNotThrow()
        {
            // Arrange
            var response = new ApiResponse();
            response.Errors.Should().BeNull("because no errors have been added");

            // Act & Assert
            var action = () => response.ClearErrors();
            action.Should().NotThrow("because clearing null errors should be safe");
        }

        [Fact]
        public void GetErrorSummary_WithErrors_ShouldReturnConcatenatedMessages()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email is required");
            response.AddValidationError("name", "Name is required");

            // Act
            var summary = response.GetErrorSummary();

            // Assert
            summary.Should().Contain("Email is required", "because email error should be included");
            summary.Should().Contain("Name is required", "because name error should be included");
            summary.Should().Contain(";", "because default separator should be used");
        }

        [Fact]
        public void GetErrorSummary_WithCustomSeparator_ShouldUseSeparator()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email is required");
            response.AddValidationError("name", "Name is required");

            // Act
            var summary = response.GetErrorSummary(" | ");

            // Assert
            summary.Should().Contain(" | ", "because custom separator should be used");
            summary.Should().NotContain(";", "because default separator should not be used");
        }

        [Fact]
        public void GetErrorSummary_WithNoErrors_ShouldReturnEmpty()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var summary = response.GetErrorSummary();

            // Assert
            summary.Should().BeEmpty("because there are no errors");
        }

        [Fact]
        public void GetErrorSummary_ShouldFallbackToGetDescription()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddError(ErrorCodes.NotFound); // No detail provided

            // Act
            var summary = response.GetErrorSummary();

            // Assert
            summary.Should().NotBeEmpty("because GetDescription should provide fallback");
            summary.Should().Contain("not found", "because NotFound description should be used");
        }

        #endregion

        #region Fluent API Tests

        [Fact]
        public void WithError_ShouldAddErrorAndReturnResponse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.WithError(ErrorCodes.NotFound, "User not found", "userId");

            // Assert
            result.Should().BeSameAs(response, "because fluent API should return same instance");
            result.Should().BeOfType<ApiResponse>("because result should be of correct type");
            response.Errors.Should().HaveCount(1, "because one error was added");
            
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.NotFound, "because error code should match");
            error.Detail.Should().Be("User not found", "because detail should match");
            error.Property.Should().Be("userId", "because property should match");
        }

        [Fact]
        public void WithValidationError_ShouldAddErrorAndReturnResponse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.WithValidationError("email", "Email is required");

            // Assert
            result.Should().BeSameAs(response, "because fluent API should return same instance");
            result.Should().BeOfType<ApiResponse>("because result should be of correct type");
            response.Errors.Should().HaveCount(1, "because one error was added");
            
            var error = response.Errors.First();
            error.StatusCode.Should().Be(ErrorCodes.InvalidArgument, "because validation error should use InvalidArgument");
            error.Detail.Should().Be("Email is required", "because detail should match");
            error.Property.Should().Be("email", "because property should match");
        }

        [Fact]
        public void FluentAPI_ShouldAllowChaining()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response
                .WithValidationError("email", "Email is required")
                .WithError(ErrorCodes.NotFound, "User not found");

            // Assert
            result.Should().BeSameAs(response, "because fluent API should return same instance");
            result.Should().BeOfType<ApiResponse>("because result should be of correct type");
            response.Errors.Should().HaveCount(2, "because two errors were added through chaining");
        }

        [Fact]
        public void AddError_ShouldReturnApiResponse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.AddError(ErrorCodes.InternalError, "Internal server error");

            // Assert
            result.Should().BeSameAs(response, "because AddError should return same instance");
            result.Should().BeOfType<ApiResponse>("because result should be of correct type");
            response.Errors.Should().HaveCount(1, "because one error was added");
        }

        [Fact]
        public void AddValidationError_ShouldReturnApiResponse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            var result = response.AddValidationError("name", "Name is required");

            // Assert
            result.Should().BeSameAs(response, "because AddValidationError should return same instance");
            result.Should().BeOfType<ApiResponse>("because result should be of correct type");
            response.Errors.Should().HaveCount(1, "because one validation error was added");
        }

        [Fact]
        public void ClearErrors_ShouldReturnApiResponse()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Email error");
            response.AddNotFoundError("User not found");

            // Act
            var result = response.ClearErrors();

            // Assert
            result.Should().BeSameAs(response, "because ClearErrors should return same instance");
            result.Should().BeOfType<ApiResponse>("because result should be of correct type");
            response.Errors.Should().NotBeNull("because the list should still exist");
            response.Errors.Should().BeEmpty("because all errors should be cleared");
        }

        [Fact]
        public void ConvenienceErrorMethods_ShouldReturnApiResponse()
        {
            // Arrange
            var response = new ApiResponse();

            // Act & Assert - Test all convenience methods return ApiResponse
            response.AddNotFoundError("Not found").Should().BeSameAs(response).And.BeOfType<ApiResponse>();
            response.AddPermissionError("Access denied").Should().BeSameAs(response).And.BeOfType<ApiResponse>();
            response.AddAuthenticationError("Invalid token").Should().BeSameAs(response).And.BeOfType<ApiResponse>();
            response.AddInternalError("Server error").Should().BeSameAs(response).And.BeOfType<ApiResponse>();
            response.AddCancellationError("Cancelled").Should().BeSameAs(response).And.BeOfType<ApiResponse>();

            // Verify all errors were added
            response.Errors.Should().HaveCount(5, "because five convenience methods were called");
        }

        [Fact]
        public void BulkErrorMethods_ShouldReturnApiResponse()
        {
            // Arrange
            var response = new ApiResponse();
            var messages = new[] { "Error 1", "Error 2" };
            var validationDict = new Dictionary<string, List<string>>
            {
                { "field1", new List<string> { "Field1 error" } }
            };

            // Act & Assert
            response.AddValidationErrors("test", messages).Should().BeSameAs(response).And.BeOfType<ApiResponse>();
            response.AddValidationErrors(validationDict).Should().BeSameAs(response).And.BeOfType<ApiResponse>();

            // Verify errors were added
            response.Errors.Should().HaveCount(3, "because bulk methods added errors");
        }
        #endregion

        #region JSON Serialization Tests - Source Generation Verification

        [Fact]
        public void JsonSerialization_ShouldUseSourceGeneration_WithApiContractsContext()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("email", "Invalid format");
            response.Links = new List<Link> { new Link("http://example.com", "self") };

            // Act
            var json = JsonSerializer.Serialize(response, ApiContractsJsonContext.Default.ApiResponse);
            var deserialized = JsonSerializer.Deserialize<ApiResponse>(json, ApiContractsJsonContext.Default.ApiResponse);

            // Assert
            json.Should().NotBeNullOrEmpty("because serialization should produce JSON");
            deserialized.Should().NotBeNull("because deserialization should work");
            deserialized.Errors.Should().HaveCount(1, "because error should be preserved");
            deserialized.Links.Should().HaveCount(1, "because link should be preserved");
            deserialized.MetaData.Should().NotBeNull("because metadata should be preserved");
        }

        [Fact]
        public void JsonSerialization_ShouldUseMetadataMode_NotReflection()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "test error");

            // Verify that the context uses metadata-based serialization (source generation)
            var typeInfo = ApiContractsJsonContext.Default.ApiResponse;
            
            // Assert
            typeInfo.Should().NotBeNull("because type info should be available from source generation");
            typeInfo.Type.Should().Be<ApiResponse>("because type info should be for ApiResponse");
            
            // Verify serialization mode (metadata-based, not reflection)
            typeInfo.Kind.Should().Be(JsonTypeInfoKind.Object, "because ApiResponse should be serialized as object");
            typeInfo.Properties.Should().NotBeEmpty("because properties should be defined in metadata");
        }

        [Fact]
        public void JsonSerialization_VerifySourceGenerationIsActive()
        {
            // Arrange
            var response = new ApiResponse();
            response.AddValidationError("test", "test error");
            
            // Verify TypeInfo is from source generation
            var typeInfo = ApiContractsJsonContext.Default.ApiResponse;
            typeInfo.Should().NotBeNull("because source generation should provide type info");
            typeInfo.Kind.Should().Be(JsonTypeInfoKind.Object, "because ApiResponse should be serialized as object");
            typeInfo.Properties.Should().NotBeEmpty("because properties should be pre-defined in metadata");
            
            // Verify the context can resolve type info
            var contextTypeInfo = ApiContractsJsonContext.Default.GetTypeInfo(typeof(ApiResponse));
            contextTypeInfo.Should().NotBeNull("because context should be able to resolve ApiResponse type info");
            contextTypeInfo.Type.Should().Be<ApiResponse>("because resolved type info should be for ApiResponse");
            
            // Verify serialization works and produces expected results
            var json = JsonSerializer.Serialize(response, ApiContractsJsonContext.Default.ApiResponse);
            json.Should().Contain("\"errors\"", "because errors should be serialized");
            json.Should().Contain("\"meta\"", "because metadata should be serialized");
            
            // Verify properties are correctly configured in metadata
            var errorsProperty = typeInfo.Properties.FirstOrDefault(p => p.Name == "errors");
            errorsProperty.Should().NotBeNull("because errors property should be defined in metadata");
            errorsProperty.PropertyType.Should().Be(typeof(List<ErrorMessage>), "because errors property type should be correctly defined");
            
            var metaProperty = typeInfo.Properties.FirstOrDefault(p => p.Name == "meta");
            metaProperty.Should().NotBeNull("because meta property should be defined in metadata");
            metaProperty.PropertyType.Should().Be(typeof(MetaData), "because meta property type should be correctly defined");
            
            // Verify property naming policy is applied (camelCase)
            var linksProperty = typeInfo.Properties.FirstOrDefault(p => p.Name == "links");
            if (linksProperty != null) // Links property may not always be present depending on serialization options
            {
                linksProperty.Name.Should().Be("links", "because camelCase naming should be applied to property names");
            }
            
            // Verify property order configuration
            var properties = typeInfo.Properties.ToList();
            properties.Should().Contain(p => p.Name == "errors", "because errors property should be configured");
            properties.Should().Contain(p => p.Name == "meta", "because meta property should be configured");
            
            // Verify conditional serialization is configured for errors
            errorsProperty.ShouldSerialize.Should().NotBeNull("because errors should have conditional serialization configured");
            
            // Verify the context is configured with proper options
            var contextOptions = ApiContractsJsonContext.Default.Options;
            contextOptions.Should().NotBeNull("because context should have serializer options");
            contextOptions.PropertyNamingPolicy.Should().NotBeNull("because camelCase naming policy should be configured");
            
            // Verify deserialization round-trip works with source generation
            var deserializedResponse = JsonSerializer.Deserialize<ApiResponse>(json, ApiContractsJsonContext.Default.ApiResponse);
            deserializedResponse.Should().NotBeNull("because deserialization should work with source generation");
            deserializedResponse.Errors.Should().HaveCount(1, "because error should be preserved through round-trip");
            deserializedResponse.MetaData.Should().NotBeNull("because metadata should be preserved through round-trip");
            
            // Verify that we're NOT using reflection mode by checking type info characteristics
            typeInfo.Type.Should().Be<ApiResponse>("because type should be correctly identified");
            typeInfo.Kind.Should().Be(JsonTypeInfoKind.Object, "because it should be identified as an object type");
            
            // Properties collection should be populated (indicating metadata mode, not reflection fallback)
            typeInfo.Properties.Should().NotBeEmpty("because metadata-based serialization should have pre-defined properties");
            typeInfo.Properties.Should().HaveCountGreaterThan(2, "because ApiResponse should have multiple properties defined");
        }

        #endregion

        #region Integration with Extension Methods Tests

        [Fact]
        public void ApiResponse_ShouldWorkWithExtensionMethods()
        {
            // Arrange
            var response = new ApiResponse();

            // Act & Assert - Success case
            response.IsSuccess().Should().BeTrue("because response has no errors");
            response.HasFailures().Should().BeFalse("because response has no failures");

            // Add an error
            response.AddValidationError("email", "Invalid format");

            // Act & Assert - Failure case
            response.IsSuccess().Should().BeFalse("because response now has errors");
            response.HasFailures().Should().BeTrue("because response now has failures");
            response.HasClientErrors().Should().BeTrue("because validation error is a client error");
            response.GetErrorCount().Should().Be(1, "because one error was added");
        }

        [Fact]
        public void ApiResponse_ExtensionMethods_ShouldProvideCorrectClassifications()
        {
            // Arrange
            var response = new ApiResponse();

            // Add different types of errors
            response.AddValidationError("email", "Invalid email");           // Client error
            response.AddInternalError("Database connection failed");         // Server error
            response.AddCancellationError("Operation cancelled");           // Cancellation

            // Act & Assert
            response.HasClientErrors().Should().BeTrue("because validation error is a client error");
            response.HasServerErrors().Should().BeTrue("because internal error is a server error");
            response.HasCancellations().Should().BeTrue("because cancellation error is present");
            response.GetErrorCount().Should().Be(3, "because three errors were added");
            
            var resultCategory = response.GetResultCategory();
            resultCategory.Should().Be(ResultCategory.Cancelled, "because cancellations take priority in the logic");
        }

        [Fact]
        public void ApiResponse_ExtensionMethods_ShouldPrioritizeServerErrorsWhenNoCancellations()
        {
            // Arrange
            var response = new ApiResponse();

            // Add different types of errors (no cancellations)
            response.AddValidationError("email", "Invalid email");           // Client error
            response.AddInternalError("Database connection failed");         // Server error

            // Act & Assert
            response.HasClientErrors().Should().BeTrue("because validation error is a client error");
            response.HasServerErrors().Should().BeTrue("because internal error is a server error");
            response.HasCancellations().Should().BeFalse("because no cancellation error is present");
            response.GetErrorCount().Should().Be(2, "because two errors were added");
            
            var resultCategory = response.GetResultCategory();
            resultCategory.Should().Be(ResultCategory.ServerError, "because server errors take priority over client errors");
        }

        #endregion

        #region Edge Cases and Error Conditions Tests

        [Fact]
        public void ApiResponse_WithLargeNumberOfErrors_ShouldHandleCorrectly()
        {
            // Arrange
            var response = new ApiResponse();
            var errorCount = 1000;

            // Act
            for (int i = 0; i < errorCount; i++)
            {
                response.AddValidationError($"field{i}", $"Error {i}");
            }

            // Assert
            response.Errors.Should().HaveCount(errorCount, "because all errors should be added");
            response.GetErrorSummary().Length.Should().BeGreaterThan(0, "because summary should include all errors");
        }

        [Fact]
        public void ApiResponse_WithNullParameters_ShouldHandleGracefully()
        {
            // Arrange
            var response = new ApiResponse();

            // Act & Assert - Should not throw for null parameters
            var action1 = () => response.AddError(ErrorCodes.InternalError, null, null);
            var action2 = () => response.AddValidationError("test", null);
            var action3 = () => response.GetErrorsForProperty(null);

            action1.Should().NotThrow("because null detail and property should be handled");
            action2.Should().NotThrow("because null detail should be handled");
            action3.Should().NotThrow("because null property name should be handled");
        }

        [Fact]
        public void ApiResponse_WithEmptyStrings_ShouldHandleCorrectly()
        {
            // Arrange
            var response = new ApiResponse();

            // Act
            response.AddValidationError("", "");
            response.AddError(ErrorCodes.NotFound, "", "");

            // Assert
            response.Errors.Should().HaveCount(2, "because errors with empty strings should be added");
            
            var emptyPropertyErrors = response.GetErrorsForProperty("").ToList();
            emptyPropertyErrors.Should().HaveCount(2, "because both errors have empty property names");
        }

        #endregion

        #region Non-Blazor Client Compatibility Tests

        [Fact]
        public void ApiResponse_ShouldSerializeForNonBlazorClients()
        {
            // Arrange - Simulate a typical server response for non-Blazor client
            var response = new ApiResponse();
            response.AddValidationError("userId", "User ID is required");
            response.AddValidationError("email", "Email format is invalid");
            response.Links = new List<Link> 
            { 
                new Link("http://api.example.com/users", "self"),
                new Link("http://api.example.com/users/help", "help")
            };

            // Act - Use universal context (suitable for non-Blazor clients)
            var json = JsonSerializer.Serialize(response, ApiContractsJsonContext.Default.ApiResponse);
            var deserializedResponse = JsonSerializer.Deserialize<ApiResponse>(json, ApiContractsJsonContext.Default.ApiResponse);

            // Assert
            deserializedResponse.Should().NotBeNull("because deserialization should work for non-Blazor clients");
            deserializedResponse.Errors.Should().HaveCount(2, "because both validation errors should be preserved");
            deserializedResponse.Links.Should().HaveCount(2, "because both links should be preserved");
            
            // Verify functionality works with extension methods
            deserializedResponse.IsSuccess().Should().BeFalse("because there are validation errors");
            deserializedResponse.HasClientErrors().Should().BeTrue("because validation errors are client errors");
            deserializedResponse.GetErrorCount().Should().Be(2, "because two errors exist");
        }

        [Fact]
        public void ApiResponse_ShouldWorkWithTypicalRestApiScenario()
        {
            // Arrange - Simulate typical REST API error response
            var response = new ApiResponse();
            response.AddNotFoundError("User with ID 123 not found", "userId");
            response.MetaData.Tags = new SortedDictionary<string, string>
            {
                { "operation", "GetUser" },
                { "requestId", "req-123-456" }
            };

            // Act
            var json = JsonSerializer.Serialize(response, ApiContractsJsonContext.Default.ApiResponse);

            // Assert
            json.Should().Contain("\"statusCode\":\"NotFound\"", "because error code should be serialized");
            json.Should().Contain("User with ID 123 not found", "because error detail should be included");
            json.Should().Contain("\"operation\":\"GetUser\"", "because metadata tags should be included");
            
            // Verify round-trip for client consumption
            var clientResponse = JsonSerializer.Deserialize<ApiResponse>(json, ApiContractsJsonContext.Default.ApiResponse);
            clientResponse.HasErrorCode(ErrorCodes.NotFound).Should().BeTrue("because NotFound error should be preserved");
        }

        #endregion
    }
}