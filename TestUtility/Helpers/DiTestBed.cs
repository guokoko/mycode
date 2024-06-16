using System;
using Microsoft.Extensions.DependencyInjection;
using RZ.Foundation.Types;
using Xunit.Abstractions;

namespace TestUtility.Helpers
{
    public class DITestBed<T> : InjectableTestBed<T> where T : class
    {
        public static DITestBed<T> New(ITestOutputHelper output) => new DITestBed<T>(output);
        public static DITestBed<T> New(ITestOutputHelper output, Iter<(Type AsType, object Instance)> preRegistration) =>
            new DITestBed<T>(output, preRegistration);

        public DITestBed(ITestOutputHelper output) : base(output) {
            Output = output;
        }

        public DITestBed(ITestOutputHelper output, Iter<(Type AsType, object Instance)> preRegistration) : base(output, preRegistration) {
            Output = output;
        }

        public ITestOutputHelper Output { get; }

        public T CreateSubject() {
            var resolver = Builder.BuildServiceProvider();

            return resolver.GetService<T>();
        }
    }
}