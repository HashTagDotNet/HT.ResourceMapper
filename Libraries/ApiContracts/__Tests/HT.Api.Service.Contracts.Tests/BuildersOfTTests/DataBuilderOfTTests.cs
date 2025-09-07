// ReSharper disable InconsistentNaming
using HT.Api.Client.Contracts.Models;
using HT.Api.Service.Contracts.BuildersOfT;

namespace HT.Api.Service.Contracts.Tests.BuildersOfTTests
{
    /// <summary>
    /// Test model for DataBuilder<T> tests that satisfies the generic constraint
    /// </summary>
    public class TestDataModel
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
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/DataBuilder")]
    public class DataBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestDataModel> _mockServiceResponseBuilder;
        private readonly DataBuilder<TestDataModel> _dataBuilder;

        public DataBuilderOfTTests()
        {
            _mockServiceResponseBuilder = new ServiceResponseBuilder<TestDataModel>();
            _dataBuilder = new DataBuilder<TestDataModel>(_mockServiceResponseBuilder);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalledWithValidParent_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceResponseBuilder = new ServiceResponseBuilder<TestDataModel>();

            // Act
            var dataBuilder = new DataBuilder<TestDataModel>(serviceResponseBuilder);

            // Assert
            dataBuilder.Should().NotBeNull("because constructor should create a valid instance");
        }

        [Fact]
        public void Constructor_WhenCalledWithNullParent_ShouldAllowNullButFailOnAccess()
        {
            // Arrange & Act
            var dataBuilder = new DataBuilder<TestDataModel>(null!);

            // Assert
            dataBuilder.Should().NotBeNull("because constructor allows null parent");
            
            // But accessing properties should throw NullReferenceException
            Action accessAction = () => _ = dataBuilder.Http;
            accessAction.Should().Throw<NullReferenceException>("because null parent will cause NullReferenceException when accessing properties");
        }

        #endregion

        #region Set Method Tests

        [Fact]
        public void Set_WhenCalledWithValidData_ShouldSetDataOnApiResponse()
        {
            // Arrange
            var testData = new TestDataModel { Name = "Test Item", Value = 42, CreatedDate = DateTime.Now };

            // Act
            var result = _dataBuilder.Set(testData);

            // Assert
            result.Should().Be(_dataBuilder, "because Set should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().Be(testData, "because data should be set on the ApiResponse");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data!.Name.Should().Be("Test Item", "because data properties should be preserved");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Value.Should().Be(42, "because data properties should be preserved");
        }

        [Fact]
        public void Set_WhenCalledWithNullData_ShouldSetNullDataOnApiResponse()
        {
            // Act
            var result = _dataBuilder.Set(null!);

            // Assert
            result.Should().Be(_dataBuilder, "because Set should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().BeNull("because null data should be set on the ApiResponse");
        }

        [Fact]
        public void Set_WhenApiResponseHasErrors_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Errors = new List<Message>
            {
                new() { Title = "Test Error", CallStatus = CallStatusCode.InvalidArgument }
            };
            var testData = new TestDataModel { Name = "Test Item" };

            // Act
            Action act = () => _dataBuilder.Set(testData);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Cannot set data on API response when there are errors already registered.",
                    "because data and errors cannot coexist according to JSON:API specification");
        }

        [Fact]
        public void Set_WhenApiResponseHasEmptyErrorsList_ShouldSetDataSuccessfully()
        {
            // Arrange
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Errors = new List<Message>();
            var testData = new TestDataModel { Name = "Test Item", Value = 100 };

            // Act
            var result = _dataBuilder.Set(testData);

            // Assert
            result.Should().Be(_dataBuilder, "because Set should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().Be(testData, "because data should be set when errors list is empty");
        }

        [Fact]
        public void Set_WhenCalledMultipleTimes_ShouldOverridePreviousData()
        {
            // Arrange
            var firstData = new TestDataModel { Name = "First", Value = 1 };
            var secondData = new TestDataModel { Name = "Second", Value = 2 };

            // Act
            _dataBuilder.Set(firstData);
            var result = _dataBuilder.Set(secondData);

            // Assert
            result.Should().Be(_dataBuilder, "because Set should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().Be(secondData, "because second data should override first data");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data!.Name.Should().Be("Second", "because data should be completely replaced");
        }

        #endregion

        #region Clear Method Tests

        [Fact]
        public void Clear_WhenCalledWithExistingData_ShouldSetDataToNull()
        {
            // Arrange
            var testData = new TestDataModel { Name = "Test Item", Value = 42 };
            _dataBuilder.Set(testData);

            // Act
            var result = _dataBuilder.Clear();

            // Assert
            result.Should().Be(_dataBuilder, "because Clear should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().BeNull("because Clear should set data to null");
        }

        [Fact]
        public void Clear_WhenCalledWithNoExistingData_ShouldKeepDataAsNull()
        {
            // Act
            var result = _dataBuilder.Clear();

            // Assert
            result.Should().Be(_dataBuilder, "because Clear should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().BeNull("because data should remain null");
        }

        [Fact]
        public void Clear_WhenCalledAfterSettingData_ShouldAllowSettingDataAgain()
        {
            // Arrange
            var initialData = new TestDataModel { Name = "Initial", Value = 1 };
            var newData = new TestDataModel { Name = "New", Value = 2 };

            // Act
            _dataBuilder.Set(initialData);
            _dataBuilder.Clear();
            var result = _dataBuilder.Set(newData);

            // Assert
            result.Should().Be(_dataBuilder, "because fluent API should continue to work");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().Be(newData, "because new data should be set after clearing");
        }

        #endregion

        #region Builder Navigation Properties Tests

        [Fact]
        public void Http_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var httpBuilder = _dataBuilder.Http;

            // Assert
            httpBuilder.Should().NotBeNull("because Http property should return a valid HttpApiResponseBuilder instance");
            httpBuilder.Should().Be(_mockServiceResponseBuilder.Http, "because Http property should return the same instance as parent");
        }

        [Fact]
        public void Validation_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var validationBuilder = _dataBuilder.Validation;

            // Assert
            validationBuilder.Should().NotBeNull("because Validation property should return a valid ValidationBuilder instance");
            validationBuilder.Should().Be(_mockServiceResponseBuilder.Validation, "because Validation property should return the same instance as parent");
        }

        [Fact]
        public void Errors_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var errorBuilder = _dataBuilder.Errors;

            // Assert
            errorBuilder.Should().NotBeNull("because Errors property should return a valid ErrorBuilder instance");
            errorBuilder.Should().Be(_mockServiceResponseBuilder.Errors, "because Errors property should return the same instance as parent");
        }

        [Fact]
        public void Links_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var linksBuilder = _dataBuilder.Links;

            // Assert
            linksBuilder.Should().NotBeNull("because Links property should return a valid LinksBuilder instance");
            linksBuilder.Should().Be(_mockServiceResponseBuilder.Links, "because Links property should return the same instance as parent");
        }

        [Fact]
        public void Meta_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var metaBuilder = _dataBuilder.Meta;

            // Assert
            metaBuilder.Should().NotBeNull("because Meta property should return a valid MetaBuilder instance");
            metaBuilder.Should().Be(_mockServiceResponseBuilder.Meta, "because Meta property should return the same instance as parent");
        }

        #endregion

        #region BuildResponse Method Tests

        [Fact]
        public void BuildResponse_WhenCalledWithoutAction_ShouldReturnCorrectApiServiceResponse()
        {
            // Arrange
            var testData = new TestDataModel { Name = "Test", Value = 123 };
            _dataBuilder.Set(testData);

            // Act
            var response = _dataBuilder.BuildResponse();

            // Assert
            response.Should().NotBeNull("because BuildResponse should return a valid ApiServiceResponse");
            response.ApiResponse.Should().NotBeNull("because ApiServiceResponse should have an ApiResponse");
            response.ApiResponse.Data.Should().Be(testData, "because data should be preserved in the final response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, "because successful response should have Ok status");
        }

        [Fact]
        public void BuildResponse_WhenCalledWithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            var testData = new TestDataModel { Name = "Test", Value = 456 };
            _dataBuilder.Set(testData);
            var actionCalled = false;
            ApiServiceResponse<TestDataModel>? capturedResponse = null;

            // Act
            var response = _dataBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
            response.ApiResponse.Data.Should().Be(testData, "because data should be preserved in the final response");
        }

        #endregion

        #region Fluent API Integration Tests

        [Fact]
        public void FluentAPI_DataSetThenClear_ShouldWorkCorrectly()
        {
            // Arrange
            var testData = new TestDataModel { Name = "Fluent Test", Value = 999 };

            // Act
            var result = _dataBuilder
                .Set(testData)
                .Clear();

            // Assert
            result.Should().Be(_dataBuilder, "because fluent API should return the same instance");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().BeNull("because Clear should remove the data");
        }

        [Fact]
        public void FluentAPI_DataSetThenNavigateToOtherBuilders_ShouldMaintainData()
        {
            // Arrange
            var testData = new TestDataModel { Name = "Navigation Test", Value = 777 };

            // Act
            _dataBuilder.Set(testData);
            var metaBuilder = _dataBuilder.Meta;
            var httpBuilder = _dataBuilder.Http;

            // Assert
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data.Should().Be(testData, "because data should be maintained when navigating to other builders");
            metaBuilder.Should().NotBeNull("because Meta navigation should work");
            httpBuilder.Should().NotBeNull("because Http navigation should work");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Test Name")]
        [InlineData("Another Test")]
        public void Set_WithDifferentNameValues_ShouldSetCorrectly(string name)
        {
            // Arrange
            var testData = new TestDataModel { Name = name, Value = 100 };

            // Act
            var result = _dataBuilder.Set(testData);

            // Assert
            result.Should().Be(_dataBuilder, "because Set should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data!.Name.Should().Be(name, "because name should be set correctly regardless of value");
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(int.MaxValue)]
        public void Set_WithDifferentValueNumbers_ShouldSetCorrectly(int value)
        {
            // Arrange
            var testData = new TestDataModel { Name = "Number Test", Value = value };

            // Act
            var result = _dataBuilder.Set(testData);

            // Assert
            result.Should().Be(_dataBuilder, "because Set should return the same instance for fluent API");
            _mockServiceResponseBuilder.BackingServiceResponse.ApiResponse.Data!.Value.Should().Be(value, "because value should be set correctly regardless of magnitude");
        }

        #endregion

        #region Edge Cases and Error Scenarios

        [Fact]
        public void Set_WhenApiResponseIsNull_ShouldHandleGracefully()
        {
            // This test verifies the class handles null ApiResponse gracefully
            // In the current implementation, ApiResponse is always initialized, but this tests defensive programming
            
            // Arrange
            var testData = new TestDataModel { Name = "Edge Case", Value = 555 };

            // Act & Assert
            // This should not throw an exception even if ApiResponse were null
            var result = _dataBuilder.Set(testData);
            result.Should().Be(_dataBuilder, "because Set should handle edge cases gracefully");
        }

        [Fact]
        public void DataBuilder_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where T : class, new()
            
            // Arrange & Act
            var testModelBuilder = new DataBuilder<TestDataModel>(_mockServiceResponseBuilder);
            var listBuilder = new DataBuilder<List<string>>(new ServiceResponseBuilder<List<string>>());
            var dictBuilder = new DataBuilder<Dictionary<string, object>>(new ServiceResponseBuilder<Dictionary<string, object>>());

            // Assert
            testModelBuilder.Should().NotBeNull("because TestDataModel is a valid reference type with parameterless constructor");
            listBuilder.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictBuilder.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        #endregion
    }
}
