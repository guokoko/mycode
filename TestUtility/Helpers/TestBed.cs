using System;
using System.Collections.Generic;
using CTO.Price.Shared.Domain;
using Microsoft.Extensions.Options;
using RZ.Foundation.Types;
using Xunit.Abstractions;
using static RZ.Foundation.Prelude;

namespace TestUtility.Helpers
{
    public class TestBed<T> : DITestBed<T> where T : class
    {
        public new static TestBed<T> New(ITestOutputHelper output) => new TestBed<T>(output);

        public new static TestBed<T> New(ITestOutputHelper output,
            Iter<(Type AsType, object Instance)> preRegistration) =>
            new TestBed<T>(output, preRegistration);

        public TestBed(ITestOutputHelper output) : this(output, Iter(PreconfigWithMocks())) {

        }

        public TestBed(ITestOutputHelper output, Iter<(Type AsType, object Instance)> preRegistration)
            : base(output, preRegistration) {
        }

        static IEnumerable<(Type, object)> PreconfigWithMocks() {
            return new List<(Type, object)>()
            {
                (typeof(IOptions<PublishConfiguration>), Options.Create(new PublishConfiguration()
                {
                    StoreChannelMap = new Dictionary<string, string>()
                    {
                        {"10138", "CDS-Website"},
                        {"20174", "RBS-Website"}
                    }
                })) 
            };
        }
    }
}