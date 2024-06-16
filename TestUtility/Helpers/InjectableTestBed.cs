using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Moq;
using RZ.Foundation.Extensions;
using RZ.Foundation.Types;
using Xunit.Abstractions;
using static RZ.Foundation.Prelude;

namespace TestUtility.Helpers
{
    public class TestLogger<T> : ILogger<T>{
        readonly ITestOutputHelper output;
        public TestLogger(ITestOutputHelper output) {
            this.output = output;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) {
            output.WriteLine($"[{logLevel} {eventId.ToString()}] {formatter(state, exception)}"); 
        }

        public bool IsEnabled(LogLevel logLevel) {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state) {
            throw new NotImplementedException();
        }
    }

    class TestLocalizer<T> : IStringLocalizer<T>
    {
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Enumerable.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(CultureInfo culture) => this;

        public LocalizedString this[string name] => new LocalizedString(name, name, resourceNotFound: true);

        public LocalizedString this[string name, params object[] arguments] => this[name];
    }

    public abstract class InjectableTestBed<T> where T : class
    {
        protected readonly ServiceCollection Builder = new ServiceCollection();
        protected readonly IDictionary<Type,object> precreated;

        protected InjectableTestBed(ITestOutputHelper output) : this(output, Iter(Enumerable.Empty<(Type,object)>())) { }

        protected InjectableTestBed(ITestOutputHelper output, Iter<(Type AsType, object Instance)> preRegistration) {
            var ctorParams = typeof(T).GetConstructors(BindingFlags.Public|BindingFlags.Instance).Single().GetParameters();

            var preCreatedDict = preRegistration.ToDictionary(key => key.AsType, val => val.Instance);

            Builder.AddSingleton(output);
            Builder.AddSingleton(typeof(ILogger<T>), typeof(TestLogger<T>));
            Builder.AddSingleton(typeof(IStringLocalizer<>), typeof(TestLocalizer<>));
            precreated = preRegistration.ToDictionary(k => k.AsType, v => v.Instance);

            foreach (var p in ctorParams.Where(cp => !typeof(ILogger).IsAssignableFrom(cp.ParameterType) && !typeof(IStringLocalizer).IsAssignableFrom(cp.ParameterType)))
            {
                var obj = preCreatedDict.Get(p.ParameterType).GetOrElse(() => CreateMockByType(p.ParameterType));
                var mock = obj is Mock m? m.Object : obj;
                Builder.AddSingleton(p.ParameterType, mock);
                precreated[p.ParameterType] = obj;
            }

            Builder.AddTransient<T>();
        }

        public Mock<TService> Fake<TService>() where TService: class {
            var key = typeof(TService).GetTypeInfo();
            return precreated.Get(key).Get(x => (Mock<TService>)x, () => {
                var mock = new Mock<TService>();
                Builder.AddSingleton(mock.Object);
                precreated[key] = mock;
                return mock;
            });
        }

        public void RegisterSingleton<TInterface>(TInterface obj) where TInterface : class {
            Builder.AddSingleton(obj);
        }
        public void RegisterTransientType<TInterface, TConcrete>() where TConcrete: class, TInterface
                                                          where TInterface: class
        {
            Builder.AddTransient<TInterface, TConcrete>();
        }

        static Mock CreateMockByType(Type type) {
            var ctor = typeof(Mock<>).MakeGenericType(type).GetConstructor(new Type[0])!;
            return (Mock) ctor.Invoke(null);
        }
    }
}