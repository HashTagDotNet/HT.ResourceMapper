using FluentAssertions;
using HT.Api.Client.Contracts.Models;
// ReSharper disable InconsistentNaming

namespace HT.Api.Client.Contracts.Tests
{
    [Trait("Category", "Unit")]
    [Trait("Category", "HT")]
    [Trait("Category", "HT/Api")]
    [Trait("Category", "HT/Api/Client")]
    [Trait("Category", "HT/Api/Client/Contracts")]
    [Trait("Category", "HT/Api/Client/Contracts/Models")]
    [Trait("Category", "HT/Api/Client/Contracts/Models/ResultCategory")]
    public class ResultCategoryTests
    {
        #region Enum Value Tests

        [Fact]
        public void ResultCategory_EnumValues_ShouldHaveCorrectValues()
        {
            // Arrange & Act & Assert
            ((int)ResultCategory.Success).Should().Be(0, "because Success should be the default enum value");
            ((int)ResultCategory.Cancelled).Should().Be(1, "because Cancelled should have value 1");
            ((int)ResultCategory.ClientError).Should().Be(2, "because ClientError should have value 2");
            ((int)ResultCategory.ServerError).Should().Be(3, "because ServerError should have value 3");
            ((int)ResultCategory.Failure).Should().Be(4, "because Failure should have value 4");
        }

        [Fact]
        public void ResultCategory_AllValues_ShouldBeUnique()
        {
            // Arrange
            var allValues = Enum.GetValues<ResultCategory>().Select(e => (int)e).ToList();
            var uniqueValues = allValues.Distinct().ToList();

            // Act & Assert
            allValues.Should().HaveCount(uniqueValues.Count, "because all result category values should be unique");
        }

        [Fact]
        public void ResultCategory_ShouldHaveExpectedCount()
        {
            // Arrange
            var allValues = Enum.GetValues<ResultCategory>();

            // Act & Assert
            allValues.Should().HaveCount(5, "because there should be exactly 5 result categories defined");
        }

        #endregion

        #region Enum Name Tests

        [Theory]
        [InlineData(ResultCategory.Success, "Success")]
        [InlineData(ResultCategory.Cancelled, "Cancelled")]
        [InlineData(ResultCategory.ClientError, "ClientError")]
        [InlineData(ResultCategory.ServerError, "ServerError")]
        [InlineData(ResultCategory.Failure, "Failure")]
        public void ResultCategory_ToString_ShouldReturnCorrectName(ResultCategory category, string expectedName)
        {
            // Act
            var result = category.ToString();

            // Assert
            result.Should().Be(expectedName, $"because {category} should have the correct string representation");
        }

        #endregion

        #region Enum Parsing Tests

        [Theory]
        [InlineData("Success", ResultCategory.Success)]
        [InlineData("Cancelled", ResultCategory.Cancelled)]
        [InlineData("ClientError", ResultCategory.ClientError)]
        [InlineData("ServerError", ResultCategory.ServerError)]
        [InlineData("Failure", ResultCategory.Failure)]
        public void ResultCategory_Parse_ValidNames_ShouldReturnCorrectEnum(string name, ResultCategory expectedCategory)
        {
            // Act
            var result = Enum.Parse<ResultCategory>(name);

            // Assert
            result.Should().Be(expectedCategory, $"because parsing '{name}' should return {expectedCategory}");
        }

        [Theory]
        [InlineData("success")]
        [InlineData("SUCCESS")]
        [InlineData("cancelled")]
        [InlineData("CANCELLED")]
        public void ResultCategory_Parse_CaseInsensitive_ShouldWork(string name)
        {
            // Act
            var action = () => Enum.Parse<ResultCategory>(name, ignoreCase: true);

            // Assert
            action.Should().NotThrow("because enum parsing should be case insensitive when specified");
        }

        [Fact]
        public void ResultCategory_Parse_InvalidName_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidName = "InvalidCategory";

            // Act
            var action = () => Enum.Parse<ResultCategory>(invalidName);

            // Assert
            action.Should().Throw<ArgumentException>("because parsing an invalid category name should throw an exception");
        }

        #endregion

        #region Enum Validation Tests

        [Theory]
        [InlineData(ResultCategory.Success)]
        [InlineData(ResultCategory.Cancelled)]
        [InlineData(ResultCategory.ClientError)]
        [InlineData(ResultCategory.ServerError)]
        [InlineData(ResultCategory.Failure)]
        public void ResultCategory_IsDefined_ValidValues_ShouldReturnTrue(ResultCategory category)
        {
            // Act
            var result = Enum.IsDefined(category);

            // Assert
            result.Should().BeTrue($"because {category} should be a defined enum value");
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(99)]
        [InlineData(1000)]
        public void ResultCategory_IsDefined_InvalidValues_ShouldReturnFalse(int invalidValue)
        {
            // Arrange
            var invalidCategory = (ResultCategory)invalidValue;

            // Act
            var result = Enum.IsDefined(invalidCategory);

            // Assert
            result.Should().BeFalse($"because {invalidValue} should not be a defined enum value");
        }

        #endregion

        #region Helper Methods

        private static IEnumerable<ResultCategory> GetAllResultCategories()
        {
            return Enum.GetValues<ResultCategory>();
        }

        #endregion
    }
}