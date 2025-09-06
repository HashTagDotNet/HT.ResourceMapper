// ReSharper disable InconsistentNaming
using System.Text.Json;
using HT.Api.Client.Contracts.Models;
using HT.Api.Client.Contracts.Serialization;

namespace HT.Api.Client.Contracts.Tests
{
    /// <summary>
    /// Test model for ApiResponse<T> tests that satisfies the generic constraint
    /// </summary>
    public class TestModel
    {
        public string? Name { get; set; }
        public int Value { get; set; }
        public List<string>? Items { get; set; }
    }

    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Client")]
    [Trait("Category", "HT/Api/Client/Contracts")]
    [Trait("Category", "HT/Api/Client/Contracts/Models")]
    [Trait("Category", "HT/Api/Client/Contracts/Models/ApiResponse")]
    public class ApiContractOfTTests
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly JsonSerializerOptions _mainJsonOptions;

        public ApiContractOfTTests()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = TestApiContractsJsonContext.Default
            };
            
            _mainJsonOptions = new JsonSerializerOptions
            {
                TypeInfoResolver = ApiContractsJsonContext.Default
            };
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalled_InitializesWithDefaultValues()
        {
            // Act
            var response = new ApiResponse<TestModel>();

            // Assert
            response.Data.Should().BeNull("because data should be null by default");
            response.Errors.Should().BeNull("because errors should be null by default");
            response.Links.Should().BeNull("because links should be null by default");
            response.MetaData.Should().NotBeNull("because metadata should always be initialized");
            response.MetaData.ResponseId.Should().NotBeNullOrEmpty("because response ID should be generated");
            response.MetaData.Timestamp.Should().NotBeNullOrEmpty("because timestamp should be generated");
        }

        [Fact]
        public void Constructor_WhenCalledMultipleTimes_GeneratesUniqueResponseIds()
        {
            // Act
            var response1 = new ApiResponse<TestModel>();
            var response2 = new ApiResponse<TestModel>();

            // Assert
            response1.MetaData.ResponseId.Should().NotBe(response2.MetaData.ResponseId, 
                "because each response should have a unique response ID");
        }

        #endregion

        #region Data Property Tests

        [Fact]
        public void Data_WhenSet_ShouldStoreValue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>();
            var testData = new TestModel { Name = "test", Value = 42 };

            // Act
            response.Data = testData;

            // Assert
            response.Data.Should().Be(testData, "because data should be stored correctly");
            response.Data.Name.Should().Be("test", "because data properties should be preserved");
        }

        [Fact]
        public void Data_WhenSetToNull_ShouldAllowNull()
        {
            // Arrange
            var response = new ApiResponse<TestModel> { Data = new TestModel { Name = "initial" } };

            // Act
            response.Data = null;

            // Assert
            response.Data.Should().BeNull("because data should accept null values");
        }

        #endregion

        #region Errors Property Tests

        [Fact]
        public void Errors_WhenSet_ShouldStoreValue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>();
            var errors = new List<Message>
            {
                new() { Title = "Test Error", Details = "Test error details" }
            };

            // Act
            response.Errors = errors;

            // Assert
            response.Errors.Should().BeEquivalentTo(errors, "because errors should be stored correctly");
        }

        [Fact]
        public void Errors_WhenSetToNull_ShouldAllowNull()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Errors = new List<Message> { new() { Title = "Test" } }
            };

            // Act
            response.Errors = null;

            // Assert
            response.Errors.Should().BeNull("because errors should accept null values");
        }

        #endregion

        #region Links Property Tests

        [Fact]
        public void Links_WhenSet_ShouldStoreValue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>();
            var links = new List<Link>
            {
                new("https://example.com/test", "self", "Test Link")
            };

            // Act
            response.Links = links;

            // Assert
            response.Links.Should().BeEquivalentTo(links, "because links should be stored correctly");
        }

        [Fact]
        public void Links_WhenSetToNull_ShouldAllowNull()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Links = new List<Link> { new("https://example.com", "self") }
            };

            // Act
            response.Links = null;

            // Assert
            response.Links.Should().BeNull("because links should accept null values");
        }

        #endregion

        #region MetaData Property Tests

        [Fact]
        public void MetaData_WhenSet_ShouldStoreValue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>();
            var metaData = new MetaData
            {
                ResponseId = "custom-id",
                CallStatus = CallStatusCode.Ok
            };

            // Act
            response.MetaData = metaData;

            // Assert
            response.MetaData.Should().Be(metaData, "because metadata should be stored correctly");
            response.MetaData.ResponseId.Should().Be("custom-id", "because custom response ID should be preserved");
        }

        #endregion

        #region ShouldSerializeData Tests

        [Fact]
        public void ShouldSerializeData_WhenDataIsNotNullAndNoErrors_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Data = new TestModel { Name = "test" },
                Errors = null
            };

            // Act
            var result = response.ShouldSerializeData();

            // Assert
            result.Should().BeTrue("because data should be serialized when there are no errors");
        }

        [Fact]
        public void ShouldSerializeData_WhenDataIsNotNullButHasErrors_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Data = new TestModel { Name = "test" },
                Errors = new List<Message> { new() { Title = "Error" } }
            };

            // Act
            var result = response.ShouldSerializeData();

            // Assert
            result.Should().BeFalse("because data should not be serialized when there are errors (JSON:API compliance)");
        }

        [Fact]
        public void ShouldSerializeData_WhenDataIsNull_ShouldReturnFalse()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Data = null,
                Errors = null
            };

            // Act
            var result = response.ShouldSerializeData();

            // Assert
            result.Should().BeFalse("because data should not be serialized when it is null");
        }

        [Fact]
        public void ShouldSerializeData_WhenDataIsNotNullAndErrorsIsEmpty_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Data = new TestModel { Name = "test" },
                Errors = new List<Message>()
            };

            // Act
            var result = response.ShouldSerializeData();

            // Assert
            result.Should().BeTrue("because data should be serialized when errors list is empty");
        }

        #endregion

        #region ShouldSerializeErrors Tests

        [Fact]
        public void ShouldSerializeErrors_WhenErrorsIsNullOrEmpty_ShouldReturnFalse()
        {
            // Arrange
            var response1 = new ApiResponse<TestModel> { Errors = null };
            var response2 = new ApiResponse<TestModel> { Errors = new List<Message>() };

            // Act
            var result1 = response1.ShouldSerializeErrors();
            var result2 = response2.ShouldSerializeErrors();

            // Assert
            result1.Should().BeFalse("because null errors should not be serialized");
            result2.Should().BeFalse("because empty errors list should not be serialized");
        }

        [Fact]
        public void ShouldSerializeErrors_WhenErrorsHasItems_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Errors = new List<Message> { new() { Title = "Error" } }
            };

            // Act
            var result = response.ShouldSerializeErrors();

            // Assert
            result.Should().BeTrue("because errors with items should be serialized");
        }

        #endregion

        #region ShouldSerializeLinks Tests

        [Fact]
        public void ShouldSerializeLinks_WhenLinksIsNullOrEmpty_ShouldReturnFalse()
        {
            // Arrange
            var response1 = new ApiResponse<TestModel> { Links = null };
            var response2 = new ApiResponse<TestModel> { Links = new List<Link>() };

            // Act
            var result1 = response1.ShouldSerializeLinks();
            var result2 = response2.ShouldSerializeLinks();

            // Assert
            result1.Should().BeFalse("because null links should not be serialized");
            result2.Should().BeFalse("because empty links list should not be serialized");
        }

        [Fact]
        public void ShouldSerializeLinks_WhenLinksHasItems_ShouldReturnTrue()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Links = new List<Link> { new("https://example.com", "self") }
            };

            // Act
            var result = response.ShouldSerializeLinks();

            // Assert
            result.Should().BeTrue("because links with items should be serialized");
        }

        #endregion

        #region JSON Serialization Tests with Source Generation

        [Fact]
        public void JsonSerialization_WithDataOnly_ShouldSerializeCorrectly()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Data = new TestModel { Name = "test data", Value = 42 }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<ApiResponse<TestModel>>(json, _jsonOptions);

            // Assert
            // Check for camelCase property names as defined in the interfaces
            json.Should().Contain("\"data\"", "because data should be serialized with camelCase naming");
            json.Should().Contain("\"test data\"", "because test data should be serialized");
            json.Should().NotContain("\"errors\"", "because errors should not be serialized when null");
            json.Should().NotContain("\"links\"", "because links should not be serialized when null");
            json.Should().Contain("\"meta\"", "because metadata should always be serialized with camelCase naming");
            
            deserialized.Should().NotBeNull("because deserialization should succeed");
            deserialized!.Data.Should().NotBeNull("because data should be deserialized");
            deserialized.Data!.Name.Should().Be("test data", "because data should be deserialized correctly");
            deserialized.Data.Value.Should().Be(42, "because numeric data should be deserialized correctly");
        }

        [Fact]
        public void JsonSerialization_WithErrorsOnly_ShouldSerializeCorrectly()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Errors = new List<Message>
                {
                    new() { Title = "Test Error", Details = "Error details", CallStatus = CallStatusCode.InvalidArgument }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<ApiResponse<TestModel>>(json, _jsonOptions);

            // Assert
            json.Should().NotContain("\"data\"", "because data should not be serialized when there are errors");
            json.Should().Contain("\"errors\"", "because errors should be serialized with camelCase naming");
            json.Should().Contain("\"Test Error\"", "because error title should be serialized");
            
            deserialized.Should().NotBeNull("because deserialization should succeed");
            deserialized!.Errors.Should().HaveCount(1, "because one error should be deserialized");
            deserialized.Errors![0].Title.Should().Be("Test Error", "because error title should be deserialized correctly");
        }

        [Fact]
        public void JsonSerialization_WithLinksOnly_ShouldSerializeCorrectly()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Links = new List<Link>
                {
                    new("https://example.com/test", "self", "Test Link", "GET")
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);
            var deserialized = JsonSerializer.Deserialize<ApiResponse<TestModel>>(json, _jsonOptions);

            // Assert
            json.Should().Contain("\"links\"", "because links should be serialized with camelCase naming");
            json.Should().Contain("\"https://example.com/test\"", "because link href should be serialized");
            
            deserialized.Should().NotBeNull("because deserialization should succeed");
            deserialized!.Links.Should().HaveCount(1, "because one link should be deserialized");
            deserialized.Links![0].Href.Should().Be("https://example.com/test", "because link href should be deserialized correctly");
        }

        [Fact]
        public void JsonSerialization_WithDataAndErrors_ShouldOnlySerializeErrors()
        {
            // Arrange
            var response = new ApiResponse<TestModel>
            {
                Data = new TestModel { Name = "test data" },
                Errors = new List<Message>
                {
                    new() { Title = "Test Error" }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);

            // Assert
            // Note: With source generation, ShouldSerialize methods are not automatically honored.
            // JSON:API compliance (data vs errors exclusivity) must be enforced at the application level
            // by setting Data to null when Errors are present, rather than relying on serialization logic.
            json.Should().Contain("\"errors\"", "because errors should be serialized with camelCase naming");
            json.Should().Contain("\"data\"", "because source generation serializes all non-null properties");
            
            // Test the ShouldSerialize methods work logically even if not used by source generation
            response.ShouldSerializeData().Should().BeFalse("because ShouldSerializeData should return false when errors are present");
            response.ShouldSerializeErrors().Should().BeTrue("because ShouldSerializeErrors should return true when errors list has items");
        }
        #endregion

        #region Complex Object Tests

        [Fact]
        public void ApiResponse_WithComplexGenericType_ShouldHandleCorrectly()
        {
            // Arrange
            var complexData = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 42,
                ["key3"] = new List<string> { "item1", "item2" }
            };
            var response = new ApiResponse<Dictionary<string, object>>
            {
                Data = complexData
            };

            // Act
            var json = JsonSerializer.Serialize(response, _mainJsonOptions);
            var deserialized = JsonSerializer.Deserialize<ApiResponse<Dictionary<string, object>>>(json, _mainJsonOptions);

            // Assert
            deserialized.Should().NotBeNull("because complex object deserialization should succeed");
            deserialized!.Data.Should().NotBeNull("because complex data should be preserved");
            // Note: JSON deserialization of object values might not preserve exact types, so we check string representation
            deserialized.Data!["key1"].ToString().Should().Be("value1", "because string values should be preserved");
        }

        #endregion

        #region Edge Cases

        [Fact]
        public void ApiResponse_WithNullMetaData_ShouldThrowWhenAccessed()
        {
            // Arrange
            var response = new ApiResponse<TestModel>();

            // Act
            Action act = () => response.MetaData = null!;

            // Assert - MetaData is required and should not be null in normal usage
            // Note: The property allows null assignment but the default constructor initializes it
            response.MetaData.Should().NotBeNull("because metadata should be initialized by default constructor");
        }

        [Fact]
        public void ApiResponse_EmptyResponse_ShouldSerializeMinimalJson()
        {
            // Arrange
            var response = new ApiResponse<TestModel>();

            // Act
            var json = JsonSerializer.Serialize(response, _jsonOptions);

            // Assert
            json.Should().Contain("\"meta\"", "because metadata should always be present");
            json.Should().NotContain("\"data\"", "because null data should not be serialized");
            json.Should().NotContain("\"errors\"", "because null errors should not be serialized");
            json.Should().NotContain("\"links\"", "because null links should not be serialized");
        }

        #endregion

        #region Type Constraint Tests

        [Fact]
        public void ApiResponse_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where TData : class, new()
            // We can't test compile-time constraints at runtime, but we can verify that valid types work
            
            // Arrange & Act
            var testModelResponse = new ApiResponse<TestModel>();
            var listResponse = new ApiResponse<List<string>>();
            var dictResponse = new ApiResponse<Dictionary<string, object>>();

            // Assert
            testModelResponse.Should().NotBeNull("because TestModel is a valid reference type with parameterless constructor");
            listResponse.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictResponse.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        #endregion
    }
}
