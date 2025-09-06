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
    [Trait("Category", "HT/Api/Service/Contracts/BuildersOfT/ValidationBuilder")]
    public class ValidationBuilderOfTTests
    {
        private readonly ServiceResponseBuilder<TestBuilderModel> _serviceResponseBuilder;
        private readonly ValidationBuilder<TestBuilderModel> _validationBuilder;

        public ValidationBuilderOfTTests()
        {
            _serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();
            _validationBuilder = new ValidationBuilder<TestBuilderModel>(_serviceResponseBuilder);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_WhenCalledWithValidParent_ShouldInitializeCorrectly()
        {
            // Arrange
            var serviceResponseBuilder = new ServiceResponseBuilder<TestBuilderModel>();

            // Act
            var validationBuilder = new ValidationBuilder<TestBuilderModel>(serviceResponseBuilder);

            // Assert
            validationBuilder.Should().NotBeNull("because constructor should create a valid instance");
        }

        [Fact]
        public void Constructor_WhenCalledWithNullParent_ShouldAllowNullButFailOnAccess()
        {
            // Arrange & Act
            var validationBuilder = new ValidationBuilder<TestBuilderModel>(null!);

            // Assert
            validationBuilder.Should().NotBeNull("because constructor allows null parent");
            
            // But accessing properties should throw NullReferenceException
            Action accessAction = () => _ = validationBuilder.Http;
            accessAction.Should().Throw<NullReferenceException>("because null parent will cause NullReferenceException when accessing properties");
        }

        #endregion

        #region AddValidation Method Tests (Property Location Overload)

        [Fact]
        public void AddValidation_WithPropertyLocationAndName_ShouldAddValidationErrorToApiResponse()
        {
            // Arrange
            const PropertyLocation propertyLocation = PropertyLocation.Body;
            const string propertyName = "EmailAddress";
            const string title = "Invalid Email";
            const string detail = "Email address format is invalid";
            const string code = "VAL001";

            // Act
            var result = _validationBuilder.AddValidation(propertyLocation, propertyName, title, detail, code);

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().NotBeNull("because errors collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(1, "because one validation error should be added");
            
            var addedError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0];
            addedError.PropertyLocation.Should().Be(propertyLocation, "because property location should be set correctly");
            addedError.PropertyName.Should().Be(propertyName, "because property name should be set correctly");
            addedError.Title.Should().Be(title, "because title should be set correctly");
            addedError.Details.Should().Be(detail, "because details should be set correctly");
            addedError.MessageCode.Should().Be(code, "because message code should be set correctly");
            addedError.CallStatus.Should().Be(CallStatusCode.InvalidArgument, "because validation errors should have InvalidArgument status");
        }

        [Fact]
        public void AddValidation_WithNullOptionalParameters_ShouldAddValidationErrorWithNullValues()
        {
            // Arrange
            const PropertyLocation propertyLocation = PropertyLocation.Query;
            const string propertyName = "UserId";

            // Act
            var result = _validationBuilder.AddValidation(propertyLocation, propertyName, null, null, null);

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(1, "because one validation error should be added");
            
            var addedError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0];
            addedError.PropertyLocation.Should().Be(propertyLocation, "because property location should be set correctly");
            addedError.PropertyName.Should().Be(propertyName, "because property name should be set correctly");
            addedError.Title.Should().BeNull("because title was passed as null");
            addedError.Details.Should().BeNull("because details was passed as null");
            addedError.MessageCode.Should().BeNull("because message code was passed as null");
            addedError.CallStatus.Should().Be(CallStatusCode.InvalidArgument, "because validation errors should have InvalidArgument status");
        }

        [Theory]
        [InlineData(PropertyLocation.Body)]
        [InlineData(PropertyLocation.Header)]
        [InlineData(PropertyLocation.Path)]
        [InlineData(PropertyLocation.Query)]
        public void AddValidation_WithDifferentPropertyLocations_ShouldSetCorrectly(PropertyLocation propertyLocation)
        {
            // Act
            var result = _validationBuilder.AddValidation(propertyLocation, "TestProperty", "Test Error");

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].PropertyLocation.Should().Be(propertyLocation, "because property location should be set correctly");
        }

        #endregion

        #region AddValidation Method Tests (CallStatusCode Overload)

        [Fact]
        public void AddValidation_WithCallStatusCodeAndPropertyLocation_ShouldAddValidationErrorToApiResponse()
        {
            // Arrange
            const CallStatusCode callStatus = CallStatusCode.OutOfRange;
            const PropertyLocation propertyLocation = PropertyLocation.Path;
            const string propertyName = "PageNumber";
            const string title = "Invalid Page Number";
            const string detail = "Page number must be between 1 and 100";
            const string code = "VAL002";

            // Act
            var result = _validationBuilder.AddValidation(callStatus, propertyLocation, propertyName, title, detail, code);

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(1, "because one validation error should be added");
            
            var addedError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0];
            addedError.CallStatus.Should().Be(CallStatusCode.InvalidArgument, "because validation builder always sets InvalidArgument regardless of input CallStatusCode");
            addedError.PropertyLocation.Should().Be(propertyLocation, "because property location should be set correctly");
            addedError.PropertyName.Should().Be(propertyName, "because property name should be set correctly");
            addedError.Title.Should().Be(title, "because title should be set correctly");
            addedError.Details.Should().Be(detail, "because details should be set correctly");
            addedError.MessageCode.Should().Be(code, "because message code should be set correctly");
        }

        [Fact]
        public void AddValidation_WithNullPropertyLocation_ShouldAddValidationErrorWithNullPropertyLocation()
        {
            // Arrange
            const CallStatusCode callStatus = CallStatusCode.NotFound;
            const string propertyName = "ResourceId";

            // Act
            var result = _validationBuilder.AddValidation(callStatus, null, propertyName);

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].PropertyLocation.Should().BeNull("because property location was passed as null");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].PropertyName.Should().Be(propertyName, "because property name should be set correctly");
        }

        [Theory]
        [InlineData(CallStatusCode.InvalidArgument)]
        [InlineData(CallStatusCode.OutOfRange)]
        [InlineData(CallStatusCode.NotFound)]
        [InlineData(CallStatusCode.FailedPrecondition)]
        public void AddValidation_WithDifferentCallStatusCodes_ShouldAlwaysSetInvalidArgument(CallStatusCode inputCallStatus)
        {
            // Act
            var result = _validationBuilder.AddValidation(inputCallStatus, PropertyLocation.Body, "TestProperty");

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].CallStatus.Should().Be(CallStatusCode.InvalidArgument, "because validation builder always sets InvalidArgument regardless of input");
        }

        #endregion

        #region Multiple Validations Tests

        [Fact]
        public void AddValidation_CalledMultipleTimes_ShouldAddMultipleValidationErrors()
        {
            // Act
            _validationBuilder
                .AddValidation(PropertyLocation.Body, "Name", "Name Required", "Name field is required")
                .AddValidation(PropertyLocation.Body, "Email", "Invalid Email", "Email format is invalid")
                .AddValidation(PropertyLocation.Query, "PageSize", "Invalid Page Size", "Page size must be positive");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(3, "because three validation errors should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].PropertyName.Should().Be("Name", "because first validation should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![1].PropertyName.Should().Be("Email", "because second validation should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![2].PropertyName.Should().Be("PageSize", "because third validation should be preserved");
        }

        [Fact]
        public void AddValidation_MixingBothOverloads_ShouldAddAllValidationErrors()
        {
            // Act
            _validationBuilder
                .AddValidation(PropertyLocation.Body, "Name", "Name Required")
                .AddValidation(CallStatusCode.OutOfRange, PropertyLocation.Query, "PageNumber", "Invalid Range");

            // Assert
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(2, "because two validation errors should be added");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![0].PropertyName.Should().Be("Name", "because first validation should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors![1].PropertyName.Should().Be("PageNumber", "because second validation should be preserved");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.All(e => e.CallStatus == CallStatusCode.InvalidArgument).Should().BeTrue("because all validation errors should have InvalidArgument status");
        }

        #endregion

        #region Builder Navigation Properties Tests

        [Fact]
        public void Http_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var httpBuilder = _validationBuilder.Http;

            // Assert
            httpBuilder.Should().NotBeNull("because Http property should return a valid HttpApiResponseBuilder instance");
            httpBuilder.Should().Be(_serviceResponseBuilder.Http, "because Http property should return the same instance as parent");
        }

        [Fact]
        public void Validation_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var validationBuilder = _validationBuilder.Validation;

            // Assert
            validationBuilder.Should().NotBeNull("because Validation property should return a valid ValidationBuilder instance");
            ReferenceEquals(validationBuilder, _serviceResponseBuilder.Validation).Should().BeTrue("because Validation property should return the parent's Validation builder instance");
        }

        [Fact]
        public void Errors_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var errorBuilder = _validationBuilder.Errors;

            // Assert
            errorBuilder.Should().NotBeNull("because Errors property should return a valid ErrorBuilder instance");
            errorBuilder.Should().Be(_serviceResponseBuilder.Errors, "because Errors property should return the same instance as parent");
        }

        [Fact]
        public void Links_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var linksBuilder = _validationBuilder.Links;

            // Assert
            linksBuilder.Should().NotBeNull("because Links property should return a valid LinksBuilder instance");
            linksBuilder.Should().Be(_serviceResponseBuilder.Links, "because Links property should return the same instance as parent");
        }

        [Fact]
        public void Data_ShouldReturnCorrectBuilderInstance()
        {
            // Act
            var dataBuilder = _validationBuilder.Data;

            // Assert
            dataBuilder.Should().NotBeNull("because Data property should return a valid DataBuilder instance");
            dataBuilder.Should().Be(_serviceResponseBuilder.Data, "because Data property should return the same instance as parent");
        }

        #endregion

        #region BuildResponse Method Tests

        [Fact]
        public void BuildResponse_WhenCalledWithAction_ShouldExecuteActionOnResponse()
        {
            // Arrange
            _validationBuilder.AddValidation(PropertyLocation.Body, "TestField", "Validation Error");
            var actionCalled = false;
            ApiServiceResponse<TestBuilderModel>? capturedResponse = null;

            // Act
            var response = _validationBuilder.BuildResponse(r =>
            {
                actionCalled = true;
                capturedResponse = r;
            });

            // Assert
            actionCalled.Should().BeTrue("because the action should be executed");
            capturedResponse.Should().Be(response, "because the action should receive the same response instance");
            response.ApiResponse.Errors.Should().HaveCount(1, "because validation error should be preserved in the final response");
            response.ApiResponse.MetaData.CallStatus.Should().Be(CallStatusCode.InvalidArgument, "because call status should be set based on validation errors");
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Fact]
        public void AddValidation_WhenErrorsCollectionIsNull_ShouldInitializeCollection()
        {
            // Arrange
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors = null;

            // Act
            var result = _validationBuilder.AddValidation(PropertyLocation.Body, "TestField", "Test Error");

            // Assert
            result.Should().Be(_validationBuilder, "because AddValidation should return the same instance for fluent API");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.Should().NotBeNull("because errors collection should be initialized");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(1, "because one validation error should be added");
        }

        [Fact]
        public void FluentAPI_ComplexValidationScenario_ShouldWorkCorrectly()
        {
            // Act
            var result = _validationBuilder
                .AddValidation(PropertyLocation.Body, "FirstName", "Required Field", "First name is required", "FNAME_REQ")
                .AddValidation(PropertyLocation.Body, "LastName", "Required Field", "Last name is required", "LNAME_REQ")
                .AddValidation(PropertyLocation.Body, "Email", "Invalid Format", "Email format is invalid", "EMAIL_FMT")
                .AddValidation(CallStatusCode.OutOfRange, PropertyLocation.Query, "Age", "Age Range", "Age must be between 18 and 120", "AGE_RANGE");

            // Assert
            result.Should().Be(_validationBuilder, "because fluent API should return the same instance");
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors!.Should().HaveCount(4, "because four validation errors should be added");
            
            // Verify all errors have InvalidArgument status
            _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.All(e => e.CallStatus == CallStatusCode.InvalidArgument).Should().BeTrue("because all validation errors should have InvalidArgument status");
            
            // Verify specific properties
            var firstNameError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.First(e => e.PropertyName == "FirstName");
            firstNameError.MessageCode.Should().Be("FNAME_REQ", "because message code should be preserved");
            firstNameError.PropertyLocation.Should().Be(PropertyLocation.Body, "because property location should be preserved");
            
            var ageError = _serviceResponseBuilder.BackingServiceResponse.ApiResponse.Errors.First(e => e.PropertyName == "Age");
            ageError.PropertyLocation.Should().Be(PropertyLocation.Query, "because query parameter validation should be preserved");
            ageError.Details.Should().Be("Age must be between 18 and 120", "because validation details should be preserved");
        }

        [Fact]
        public void ValidationBuilder_GenericConstraint_ShouldRequireReferenceTypeWithParameterlessConstructor()
        {
            // This test verifies the generic constraint: where T : class, new()
            
            // Arrange & Act
            var testModelBuilder = new ValidationBuilder<TestBuilderModel>(_serviceResponseBuilder);
            var listBuilder = new ValidationBuilder<List<string>>(new ServiceResponseBuilder<List<string>>());
            var dictBuilder = new ValidationBuilder<Dictionary<string, object>>(new ServiceResponseBuilder<Dictionary<string, object>>());

            // Assert
            testModelBuilder.Should().NotBeNull("because TestBuilderModel is a valid reference type with parameterless constructor");
            listBuilder.Should().NotBeNull("because List<T> is a valid reference type with parameterless constructor");
            dictBuilder.Should().NotBeNull("because Dictionary<T,U> is a valid reference type with parameterless constructor");
        }

        #endregion
    }
}