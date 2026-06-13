---
description: This prompt creates unit tests for a given class or method.
applyTo: **/*test*/**
mode: agent
---

# Step-By-Test Process

1. ALWAYS verify MOQ version is 4.18.4 or earlier
1. ALWAYS verify FluentAssertions is using 7.1.0 or earlier
2. Analyze the provided code to identify key functionalities and edge cases.
1. Generate unit tests that cover the identified functionalities and edge cases.
1. Run only the tests in the target test class.
1. AVOID running all tests in the solution.
1. Ensure the tests are written in a clear and maintainable manner.
1. ALWAYS report the total time this prompt took to run.

# Test Libraries
- Use xUnit for writing the tests.
- Use only one mocking framework in a single class.
- Use MOQ for mocking dependencies.
- Use ONLY MOQ version 4.18.4 or earlier. NEVER use any version later than 4.18.4.
- Use FluentAssertions for assertions with clear and descriptive messages.
- REQUIRE concrete implementations for dependencies that are not Interfaces.

# Test Scope
- Focus on unit tests only; do not create integration or end-to-end tests.
- Cover both positive and negative scenarios.
- Include edge cases and boundary conditions.
- Test null argument scenarios where applicable.
- Test cancellation token handling for async methods.
- Aim for high code coverage, but prioritize meaningful tests over quantity.
- Cover as many branches and paths as possible.
- Test parameterized scenarios using [Theory] and [InlineData] where appropriate.
- IGNORE trivial use cases.
- IGNORE auto-generated code.
- IGNORE exception handling unless it is a core part of the method's functionality.
- IGNORE logging unless it is a core part of the method's functionality.

# Test Structure
- Make sure test class has `// ReSharper disable InconsistentNaming` between top of file and start of the namespace
- Each test method should follow the naming convention: MethodName_StateUnderTest_ExpectedBehavior.
- Use the Arrange-Act-Assert pattern consistently.
- Include setup and teardown methods if necessary to prepare the test environment.
- Ensure that each test is independent and can be run in isolation.
- Use [InlineData] for parameterized tests where applicable.
- Group related tests using #region comments for better organization.
- Create helper methods for commonly used test setup scenarios.
- Use descriptive variable names that clearly indicate the test scenario.

# Using [Trait] Attributes
- Use the [Trait] attribute to categorize tests, e.g., [Trait("Category", "Unit")].
- MUST place [Trait] attribute ONLY on the class level.
- Always include [Trait("Category", "Unit")] for unit tests.
- Identify the class containing the method under test.
- Create a trait for each segment of the class's namespace. For example:

# Test Method Names
- Each test method should follow the naming convention: MethodName_StateUnderTest_ExpectedBehavior.
- Use clear and descriptive names that indicate the purpose of the test.
- Avoid overly long names; keep them concise yet informative.
- Use PascalCase for method names.
- In 'StateUnderTest', do not include 'Should' as first word, the '_' delmiter is sufficient. (do: MyMethod_NullInput_ThrowsException, don't: MyMethod_ShouldThrowExceptionOnNullInput)

# Test Organization
- Create a separate test class for each class being tested.
- Test folders should mirror the structure of the source code folders.
-

```csharp
namespace MyCompanyOrModule.SubSystem.Services.UserService;
public class MyClassWithMethodBeingTested
{
    public MethodUnderTest()
    {
    }
}

[Trait("Category", "Unit")]
[Trait("Category", "MyCompany")]
[Trait("Category", "MyCompany/SubSystem")]
[Trait("Category", "MyCompany/SubSystem/Services")]
[Trait("Category", "MyCompany/SubSystem/Services/UserService")]
[Trait("Category", "MyCompany/SubSystem/Services/UserService/MyClassWithMethodBeingTested")]
public class MyTestClass
{
    public void MyTestMethod()
    {
        // Test implementation
    }
}
```

# Test Class Organization
- Initialize mocked dependencies in the constructor Moq equivalents.
- Create fake objects as needed if there are no interfaces to mock.
- Store frequently used mocks as private readonly fields.
- Create helper methods for common test data setup (e.g., CreateValidProcessorContext(), CreateValidMonitor()).
- Use descriptive names for helper methods that indicate the scenario they create.
- Group test methods by the method being tested using #region comments.
- Place helper methods in a separate #region at the bottom of the class.

# Assertion Guidelines
- Use FluentAssertions for more readable assertions with descriptive failure messages.
- Always include a "because" clause in assertions to explain why the assertion should pass.
- Verify mock interactions using Received() methods to ensure dependencies are called correctly.
- Test both the return values and side effects of methods.
- For async methods, use async/await patterns consistently.

# Common Test Patterns
- **Cancellation Tests**: For async methods, test that cancellation tokens are properly handled.
- **State Verification**: Verify that objects are in the expected state after method execution.
- **Mock Verification**: Ensure mocked dependencies are called with correct parameters.

# Example Test Structure
```csharp
[Fact]
public async Task MethodName_StateUnderTest_ExpectedBehavior()
{
    // Arrange
    var context = CreateValidProcessorContext();
    var expectedResult = CreateExpectedResult();
    
    _mockService.Setup(x => x.MethodCall(Arg.Any<Type>()))
               .Returns(expectedResult);

    // Act
    var result = await _systemUnderTest.MethodName(context);

    // Assert
    result.Should().NotBeNull("because method should always return a result");
    result.Property.Should().Be(expectedValue, "because the property should match expected value");
    
    _mockService.Received(1).MethodCall(Arg.Is<Type>(x => x.Property == expectedValue));
}


