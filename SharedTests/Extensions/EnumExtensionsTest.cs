using CTO.Price.Shared.Enums;
using CTO.Price.Shared.Extensions;
using FluentAssertions;
using Xunit;

namespace SharedTests.Extensions
{
    public class EnumExtensionsTest
    {
        [Fact]
        public void EnumExtensions_GetDisplayName_ShouldBeSameDisplayName() {
            // Arrange
            var enumName = EmployeeType.CG_Employee;
            var name = "CG Employee";

            // Act
            var result = EnumExtensions.GetDisplayName(enumName);
            
            //Assert
            result.Should().BeEquivalentTo(name);
        }
    }
}