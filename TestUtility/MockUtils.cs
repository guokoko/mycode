using Microsoft.Extensions.Options;
using Moq;

namespace TestUtility
{
    public static class MockUtils
    {
        public static Mock<IOptionsMonitor<T>> MockOption<T>(T mockValue) {
            var monitor = new Mock<IOptionsMonitor<T>>();
            monitor.Setup(opt => opt.CurrentValue).Returns(mockValue);
            return monitor;
        }
    }
}