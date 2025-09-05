using HT.Api.Client.Contracts.Extensions;
using HT.Api.Client.Contracts.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
// ReSharper disable InconsistentNaming

namespace HT.Api.Client.Contracts.Tests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Client")]
    [Trait("Category", "HT/Api/Client/Contracts")]
    [Trait("Category", "HT/Api/Client/Contracts/Models")]
    [Trait("Category", "HT/Api/Client/Contracts/Models/ApiResponseOfT")]
    public class ApiResponseOfTTests
    {
        #region Test Data Classes

        public class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        public class TestProduct
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }

        #endregion

        #region Constructor and Basic Properties Tests

        [Fact]
        public void ApiResponseOfT_DefaultConstructor_ShouldInitializeCorrectly()
        {
            // Act
            var response = new ApiResponse<TestUser>();

            // Assert
            response.Data.Should().BeNull("because data should be null by default");
            response.Errors.Should().BeNull("because errors should be null by default");
            response.Links.Should().BeNull("because links should be null by default");
            response.MetaData.Should().NotBeNull("because metadata should be initialized");
        }

        [Fact]
        public void ApiResponseOfT_ShouldSerializeData_ShouldWorkCorrectly()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe", Email = "john@example.com" };

            // Act & Assert - No data initially
            response.ShouldSerializeData().Should().BeFalse("because data is null and there are no errors");

            // Set data
            response.Data = user;
            response.ShouldSerializeData().Should().BeTrue("because data is present and there are no errors");

            // Add error - should not serialize data
            response.AddValidationError("email", "Invalid email");
            response.ShouldSerializeData().Should().BeFalse("because data should not serialize when errors are present");
        }

        #endregion

        #region Data Management Tests

        [Fact]
        public void SetData_ShouldSetDataAndClearErrors()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Add some errors first
            response.AddValidationError("name", "Name is required");
            response.Errors.Should().NotBeEmpty("because error was added");

            // Act
            response.SetData(user);

            // Assert
            response.Data.Should().BeSameAs(user, "because data should be set");
            response.Errors.Should().BeNullOrEmpty("because errors should be cleared when data is set");
        }

        [Fact]
        public void SetData_WithNullData_ShouldThrowArgumentNullException()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act & Assert
            var action = () => response.SetData(null!);
            action.Should().Throw<ArgumentNullException>("because null data should not be allowed");
        }

        [Fact]
        public void ClearData_ShouldSetDataToNull()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;

            // Act
            response.ClearData();

            // Assert
            response.Data.Should().BeNull("because data should be cleared");
        }

        #endregion

        #region Error Management with JSON:API Compliance Tests

        [Fact]
        public void AddError_ShouldClearDataForJsonApiCompliance()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;

            // Act
            response.AddError(ErrorCodes.NotFound, "User not found");

            // Assert
            response.Data.Should().BeNull("because data should be cleared when errors are added");
            response.Errors.Should().HaveCount(1, "because one error was added");
        }

        [Fact]
        public void AddValidationError_ShouldClearDataForJsonApiCompliance()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;

            // Act
            response.AddValidationError("email", "Email is required");

            // Assert
            response.Data.Should().BeNull("because data should be cleared when validation errors are added");
            response.Errors.Should().HaveCount(1, "because one validation error was added");
        }

        #endregion

        #region Fluent API Tests

        [Fact]
        public void WithError_ShouldReturnCorrectGenericType()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act
            var result = response.WithError(ErrorCodes.NotFound, "User not found");

            // Assert
            result.Should().BeSameAs(response, "because fluent API should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Errors.Should().HaveCount(1, "because one error was added");
        }

        [Fact]
        public void WithValidationError_ShouldReturnCorrectGenericType()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act
            var result = response.WithValidationError("email", "Email is required");

            // Assert
            result.Should().BeSameAs(response, "because fluent API should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Errors.Should().HaveCount(1, "because one validation error was added");
        }

        [Fact]
        public void WithData_ShouldSetDataAndReturnCorrectType()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Act
            var result = response.WithData(user);

            // Assert
            result.Should().BeSameAs(response, "because fluent API should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Data.Should().BeSameAs(user, "because data should be set");
        }

        [Fact]
        public void FluentAPI_ShouldAllowChaining()
        {
            // Arrange
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Act
            var response = new ApiResponse<TestUser>()
                .WithData(user)
                .WithError(ErrorCodes.InternalError, "Something went wrong"); // This should clear data

            // Assert
            response.Data.Should().BeNull("because adding error should clear data");
            response.Errors.Should().HaveCount(1, "because one error was added");
        }

        [Fact]
        public void SetData_ShouldReturnCorrectType()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Act
            var result = response.SetData(user);

            // Assert
            result.Should().BeSameAs(response, "because SetData should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Data.Should().BeSameAs(user, "because data should be set");
        }

        [Fact]
        public void ClearData_ShouldReturnCorrectType()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;

            // Act
            var result = response.ClearData();

            // Assert
            result.Should().BeSameAs(response, "because ClearData should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Data.Should().BeNull("because data should be cleared");
        }

        [Fact]
        public void AddError_ShouldReturnCorrectGenericTypeAndClearData()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;

            // Act
            var result = response.AddError(ErrorCodes.NotFound, "User not found");

            // Assert
            result.Should().BeSameAs(response, "because AddError should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Data.Should().BeNull("because data should be cleared when errors are added");
            response.Errors.Should().HaveCount(1, "because one error was added");
        }

        [Fact]
        public void AddValidationError_ShouldReturnCorrectGenericTypeAndClearData()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;

            // Act
            var result = response.AddValidationError("email", "Email is required");

            // Assert
            result.Should().BeSameAs(response, "because AddValidationError should return same instance");
            result.Should().BeOfType<ApiResponse<TestUser>>("because result should be of correct generic type");
            response.Data.Should().BeNull("because data should be cleared when validation errors are added");
            response.Errors.Should().HaveCount(1, "because one validation error was added");
        }

        #endregion

        #region Static Factory Methods Tests

        [Fact]
        public void Success_ShouldCreateSuccessResponseWithData()
        {
            // Arrange
            var user = new TestUser { Id = 1, Name = "John Doe", Email = "john@example.com" };

            // Act
            var response = ApiResponse<TestUser>.Success(user);

            // Assert
            response.Data.Should().BeSameAs(user, "because data should be set");
            response.Errors.Should().BeNullOrEmpty("because success response should have no errors");
            response.IsSuccess().Should().BeTrue("because it should be a success response");
        }

        [Fact]
        public void Error_ShouldCreateErrorResponseWithNoData()
        {
            // Act
            var response = ApiResponse<TestUser>.Error(ErrorCodes.NotFound, "User not found");

            // Assert
            response.Data.Should().BeNull("because error response should have no data");
            response.Errors.Should().HaveCount(1, "because one error should be added");
            response.IsSuccess().Should().BeFalse("because it should be an error response");
        }

        [Fact]
        public void ValidationError_ShouldCreateValidationErrorResponse()
        {
            // Act
            var response = ApiResponse<TestUser>.ValidationError("email", "Email is required");

            // Assert
            response.Data.Should().BeNull("because validation error response should have no data");
            response.Errors.Should().HaveCount(1, "because one validation error should be added");
            response.Errors.First().StatusCode.Should().Be(ErrorCodes.InvalidArgument, "because validation errors use InvalidArgument code");
        }

        [Fact]
        public void NotFound_ShouldCreateNotFoundErrorResponse()
        {
            // Act
            var response = ApiResponse<TestUser>.NotFound("User not found", "userId");

            // Assert
            response.Data.Should().BeNull("because not found response should have no data");
            response.Errors.Should().HaveCount(1, "because one error should be added");
            response.Errors.First().StatusCode.Should().Be(ErrorCodes.NotFound, "because it should be a NotFound error");
        }

        [Fact]
        public void InternalError_ShouldCreateInternalErrorResponse()
        {
            // Act
            var response = ApiResponse<TestUser>.InternalError("Database connection failed");

            // Assert
            response.Data.Should().BeNull("because internal error response should have no data");
            response.Errors.Should().HaveCount(1, "because one error should be added");
            response.Errors.First().StatusCode.Should().Be(ErrorCodes.InternalError, "because it should be an InternalError");
        }

        #endregion

        #region Extension Methods Integration Tests

        [Fact]
        public void HasData_ExtensionMethod_ShouldWorkCorrectly()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Act & Assert - No data
            response.HasData().Should().BeFalse("because no data is set");

            // Set data
            response.Data = user;
            response.HasData().Should().BeTrue("because data is now set");

            // Clear data
            response.ClearData();
            response.HasData().Should().BeFalse("because data was cleared");
        }

        [Fact]
        public void DataOrDefault_ExtensionMethod_ShouldReturnDataOrDefault()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();

            // Act & Assert - No data set
            var defaultData = response.DataOrDefault();
            defaultData.Should().NotBeNull("because DataOrDefault should return new instance");
            defaultData.Should().BeOfType<TestUser>("because it should be correct type");

            // Set actual data
            var user = new TestUser { Id = 1, Name = "John Doe" };
            response.Data = user;
            response.DataOrDefault().Should().BeSameAs(user, "because actual data should be returned");
        }

        [Fact]
        public void IsJsonApiCompliant_ExtensionMethod_ShouldValidateCompliance()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Act & Assert - Initially compliant (no data, no errors)
            response.IsJsonApiCompliant().Should().BeTrue("because empty response is compliant");

            // Data only - compliant
            response.SetData(user);
            response.IsJsonApiCompliant().Should().BeTrue("because data without errors is compliant");

            // Add error manually (bypassing the automatic clearing)
            response.Data = user;
            response.Errors = new List<ErrorMessage> { ErrorMessage.Create(ErrorCodes.InternalError) };
            response.IsJsonApiCompliant().Should().BeFalse("because data and errors cannot coexist");
        }

        [Fact]
        public void GetComplianceIssues_ExtensionMethod_ShouldReturnIssues()
        {
            // Arrange
            var response = new ApiResponse<TestUser>();
            var user = new TestUser { Id = 1, Name = "John Doe" };

            // Set up non-compliant state
            response.Data = user;
            response.Errors = new List<ErrorMessage> { ErrorMessage.Create(ErrorCodes.InternalError) };

            // Act
            var issues = response.GetComplianceIssues().ToList();

            // Assert
            issues.Should().HaveCount(1, "because there should be one compliance issue");
            issues.First().Should().Contain("JSON:API violation", "because it should describe the violation");
        }

        #endregion

        #region JSON Serialization Tests

        [Fact]
        public void JsonSerialization_ShouldSerializeGenericResponseCorrectly()
        {
            // Arrange
            var user = new TestUser { Id = 1, Name = "John Doe", Email = "john@example.com" };
            var response = ApiResponse<TestUser>.Success(user);

            // Act - Use the base JsonSerializer options since our test types aren't in the context
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            // Assert
            json.Should().NotBeNullOrEmpty("because serialization should produce JSON");
            json.Should().Contain("\"data\"", "because data should be serialized");
            json.Should().Contain("John Doe", "because user data should be included");
            json.Should().NotContain("\"errors\"", "because errors should not be serialized when null");
        }

        [Fact]
        public void JsonDeserialization_ShouldDeserializeGenericResponseCorrectly()
        {
            // Arrange
            var json = """
                {
                    "data": {
                        "id": 1,
                        "name": "John Doe",
                        "email": "john@example.com"
                    },
                    "meta": {
                        "responseId": "test123",
                        "timestamp": "2024-01-01T12:00:00.00Z"
                    }
                }
                """;

            // Act - Use the base JsonSerializer options since our test types aren't in the context
            var response = JsonSerializer.Deserialize<ApiResponse<TestUser>>(json, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            // Assert
            response.Should().NotBeNull("because deserialization should succeed");
            response!.Data.Should().NotBeNull("because data should be deserialized");
            response.Data!.Id.Should().Be(1, "because ID should be preserved");
            response.Data.Name.Should().Be("John Doe", "because name should be preserved");
            response.Data.Email.Should().Be("john@example.com", "because email should be preserved");
            response.HasData().Should().BeTrue("because response should have data");
        }

        [Fact]
        public void JsonSerialization_ShouldRespectJsonApiCompliance()
        {
            // Arrange - Create response with error
            var response = ApiResponse<TestUser>.ValidationError("email", "Email is required");

            // Act - Use base JsonSerializer options
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            // Assert
            json.Should().Contain("\"errors\"", "because errors should be serialized");
            json.Should().NotContain("\"data\"", "because data should not be serialized when errors are present");
            json.Should().Contain("Email is required", "because error detail should be included");
        }

        #endregion

        #region Cross-Type Compatibility Tests

        [Fact]
        public void ApiResponseOfT_ShouldWorkWithDifferentDataTypes()
        {
            // Arrange
            var productResponse = new ApiResponse<TestProduct>();
            var userResponse = new ApiResponse<TestUser>();

            var product = new TestProduct { ProductId = 1, ProductName = "Test Product", Price = 99.99m };
            var user = new TestUser { Id = 1, Name = "Test User", Email = "test@example.com" };

            // Act
            productResponse.SetData(product);
            userResponse.SetData(user);

            // Assert
            productResponse.HasData().Should().BeTrue("because product data was set");
            userResponse.HasData().Should().BeTrue("because user data was set");

            productResponse.Data.Should().BeOfType<TestProduct>("because product response should have product data");
            userResponse.Data.Should().BeOfType<TestUser>("because user response should have user data");
        }

        [Fact]
        public void StaticFactoryMethods_ShouldWorkWithDifferentTypes()
        {
            // Arrange
            var product = new TestProduct { ProductId = 1, ProductName = "Test Product", Price = 99.99m };

            // Act
            var successResponse = ApiResponse<TestProduct>.Success(product);
            var errorResponse = ApiResponse<TestProduct>.NotFound("Product not found");

            // Assert
            successResponse.Data.Should().BeSameAs(product, "because success response should have the product");
            errorResponse.Data.Should().BeNull("because error response should have no data");

            successResponse.Should().BeOfType<ApiResponse<TestProduct>>("because success response should be correct type");
            errorResponse.Should().BeOfType<ApiResponse<TestProduct>>("because error response should be correct type");
        }

        #endregion
    }
}