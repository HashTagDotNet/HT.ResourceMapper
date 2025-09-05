using HT.Api.Client.Contracts.Models;
using HT.Api.Client.Contracts.Extensions;
using FluentAssertions;

namespace HT.Api.Client.Contracts.Tests
{
    [Trait("Category", "Integration")]
    [Trait("Category", "FluentAPI")]
    public class FluentApiDemonstrationTests
    {
        public class UserDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }

        [Fact]
        public void FluentAPI_BaseApiResponse_ShouldWorkCorrectly()
        {
            // Demonstrate fluent API for base ApiResponse
            var response = new ApiResponse()
                .AddValidationError("email", "Email is required")
                .AddValidationError("name", "Name is required")
                .AddPermissionError("Access denied")
                .ClearErrors()  // Start fresh
                .AddNotFoundError("Resource not found")
                .WithError(CallStatusCode.InternalError, "Database error");

            // Verify the final state
            response.Errors.Should().HaveCount(2, "because two errors were added after clearing");
            response.HasErrorCode(CallStatusCode.NotFound).Should().BeTrue();
            response.HasErrorCode(CallStatusCode.InternalError).Should().BeTrue();
            response.IsSuccess().Should().BeFalse();
        }

        [Fact]
        public void FluentAPI_GenericApiResponse_ShouldWorkCorrectly()
        {
            // Demonstrate fluent API for generic ApiResponse<T>
            var user = new UserDto { Id = 1, Name = "John Doe", Email = "john@example.com" };
            
            var response = new ApiResponse<UserDto>()
                .SetData(user)
                .ClearData()    // Remove data
                .WithData(user) // Add it back
                .AddValidationError("email", "Email format invalid"); // This should clear data

            // Verify the final state
            response.Data.Should().BeNull("because adding error should clear data for JSON:API compliance");
            response.Errors.Should().HaveCount(1, "because one validation error was added");
            response.HasData().Should().BeFalse();
            response.IsSuccess().Should().BeFalse();
        }

        [Fact]
        public void FluentAPI_StaticFactoryMethods_ShouldWorkWithChaining()
        {
            // Demonstrate factory methods with fluent API
            var user = new UserDto { Id = 1, Name = "John Doe" };

            var successResponse = ApiResponse<UserDto>
                .Success(user)
                .WithData(new UserDto { Id = 2, Name = "Jane Doe" }); // Update data

            var errorResponse = ApiResponse<UserDto>
                .ValidationError("email", "Invalid email")
                .AddValidationError("name", "Name too short")
                .WithError(CallStatusCode.PermissionDenied, "Access denied");

            // Verify success response
            successResponse.HasData().Should().BeTrue();
            successResponse.Data!.Id.Should().Be(2, "because data was updated");
            successResponse.IsSuccess().Should().BeTrue();

            // Verify error response
            errorResponse.HasData().Should().BeFalse();
            errorResponse.Errors.Should().HaveCount(3, "because three errors were added");
            errorResponse.IsSuccess().Should().BeFalse();
        }

        [Fact]
        public void FluentAPI_JsonApiCompliance_ShouldBeEnforced()
        {
            // Demonstrate JSON:API compliance enforcement
            var user = new UserDto { Id = 1, Name = "Test User" };
            
            var response = new ApiResponse<UserDto>()
                .SetData(user);

            // Verify data is present
            response.HasData().Should().BeTrue();
            response.ShouldSerializeData().Should().BeTrue();

            // Add error - should automatically clear data
            response.AddValidationError("email", "Email required");

            // Verify JSON:API compliance is maintained
            response.HasData().Should().BeFalse("because data should be cleared when errors are added");
            response.ShouldSerializeData().Should().BeFalse("because data should not serialize with errors");
            response.IsJsonApiCompliant().Should().BeTrue("because data and errors don't coexist");

            // Set data again - should clear errors
            response.SetData(user);

            // Verify errors are cleared
            response.Errors.Should().BeNullOrEmpty("because errors should be cleared when data is set");
            response.HasData().Should().BeTrue("because data should be present");
            response.IsSuccess().Should().BeTrue("because no errors remain");
        }

        [Fact]
        public void FluentAPI_ComplexScenario_ShouldWork()
        {
            // Demonstrate a complex real-world scenario
            var originalUser = new UserDto { Id = 1, Name = "John", Email = "john@old.com" };
            var updatedUser = new UserDto { Id = 1, Name = "John Doe", Email = "john@new.com" };

            // Create the response step by step to maintain type safety
            var response = ApiResponse<UserDto>
                .Success(originalUser)
                .SetData(updatedUser)  // Update with new data
                .ClearData()           // Simulate clearing for some business logic
                .AddValidationError("email", "Email domain not allowed")
                .AddValidationError("name", "Name too long");

            // Clear errors and continue with generic type preserved
            response.ClearErrors();

            // Set final data and add final error
            response.SetData(updatedUser)
                   .WithError(CallStatusCode.Cancelled, "Operation was cancelled by user");

            // Verify final state represents cancellation with no data
            response.HasData().Should().BeFalse("because final operation added an error");
            response.Errors.Should().HaveCount(1, "because only the final error remains");
            response.HasErrorCode(CallStatusCode.Cancelled).Should().BeTrue();
            response.GetResultCategory().Should().Be(ResultCategory.Cancelled);
            response.IsRetryRecommended().Should().BeFalse("because cancellations typically aren't retryable");
        }

        [Fact]
        public void FluentAPI_TypeSafety_ShouldBePreserved()
        {
            // Demonstrate that fluent API preserves type safety
            ApiResponse<UserDto> typedResponse = new ApiResponse<UserDto>()
                .AddValidationError("test", "test")
                .SetData(new UserDto { Id = 1, Name = "Test" })
                .ClearData()
                .WithData(new UserDto { Id = 2, Name = "Test2" });

            ApiResponse baseResponse = new ApiResponse()
                .AddValidationError("test", "test")
                .AddNotFoundError("not found")
                .ClearErrors()
                .WithError(CallStatusCode.InternalError, "error");

            // Verify types are preserved
            typedResponse.Should().BeOfType<ApiResponse<UserDto>>();
            baseResponse.Should().BeOfType<ApiResponse>();

            // Verify functionality works correctly
            typedResponse.HasData().Should().BeTrue();
            typedResponse.Data!.Id.Should().Be(2);
            
            baseResponse.Errors.Should().HaveCount(1);
            baseResponse.HasErrorCode(CallStatusCode.InternalError).Should().BeTrue();
        }

        [Fact]
        public void CallStatus_AuthoritativeField_ShouldWorkCorrectly()
        {
            // Demonstrate CallStatus as the authoritative field of record
            var response = new ApiResponse()
                .AddValidationError("email", "Email is required")
                .AddInternalError("Database error");

            // Verify CallStatus reflects highest priority error
            response.CurrentStatus().Should().Be(CallStatusCode.InternalError, 
                "because CallStatus should reflect highest priority error");
            response.MetaData.CallStatus.Should().Be(CallStatusCode.InternalError, 
                "because CallStatus field is the authoritative source");

            // Demonstrate explicit override
            response.SetCallStatus(CallStatusCode.Cancelled);
            response.CurrentStatus().Should().Be(CallStatusCode.Cancelled, 
                "because CallStatus can be explicitly overridden");

            // Demonstrate refresh from errors
            response.RefreshCallStatus();
            response.CurrentStatus().Should().Be(CallStatusCode.InternalError, 
                "because RefreshCallStatus recalculates from errors");

            // Demonstrate consistency checking
            response.SetCallStatus(CallStatusCode.Ok);
            response.IsCallStatusConsistent().Should().BeFalse(
                "because CallStatus doesn't match the errors");
        }

        [Fact]
        public void CallStatus_GenericResponse_ShouldMaintainJsonApiCompliance()
        {
            // Demonstrate CallStatus with generic response and JSON:API compliance
            var user = new UserDto { Id = 1, Name = "John Doe", Email = "john@example.com" };
            
            var response = new ApiResponse<UserDto>()
                .SetData(user);

            // Verify success state
            response.CurrentStatus().Should().Be(CallStatusCode.Ok, 
                "because setting data should result in Ok status");
            response.MetaData.CallStatus.Should().Be(CallStatusCode.Ok, 
                "because CallStatus should be set to Ok");

            // Add error - should clear data and update CallStatus
            response.AddValidationError("email", "Invalid format");

            response.CurrentStatus().Should().Be(CallStatusCode.InvalidArgument, 
                "because adding error should update CallStatus");
            response.HasData().Should().BeFalse(
                "because adding error clears data for JSON:API compliance");
            response.IsJsonApiCompliant().Should().BeTrue(
                "because data and errors don't coexist");

            // Clear errors - should reset to Ok
            response.ClearErrors();
            response.CurrentStatus().Should().Be(CallStatusCode.Ok, 
                "because clearing errors should reset CallStatus to Ok");
        }
    }
}