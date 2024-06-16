using System.Collections.Generic;
using CTO.Price.Shared.Extensions;
using FluentAssertions;
using Xunit;

namespace SharedTests.Extensions
{
    public class PagingExtensionTest
    {
        [Fact]
        public void PagingExtension_GetPaginationSettingBySemiColon_ShouldBeSameSetting() {
            // Arrange
            var pageSize = "20;50;100";
            var settings = new List<string>{"20", "50", "100"};

            // Act
            var result = PagingExtension.GetPaginationSettingBySemiColon(pageSize);
            
            //Assert
            result.Should().BeEquivalentTo(settings);
        }
    }
}