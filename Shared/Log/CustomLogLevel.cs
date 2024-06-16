using System.Diagnostics.CodeAnalysis;
using Serilog.Core;
using Serilog.Events;

namespace CTO.Price.Shared.Log
{
    [ExcludeFromCodeCoverage]
    public class CustomLogLevel : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var customLogLevel = string.Empty;

            switch (logEvent.Level)
            {
                case LogEventLevel.Debug:
                    customLogLevel = "DEBUG";
                    break;

                case LogEventLevel.Error:
                    customLogLevel = "ERROR";
                    break;

                case LogEventLevel.Fatal:
                    customLogLevel = "FATAL";
                    break;

                case LogEventLevel.Information:
                    customLogLevel = "INFO";
                    break;

                case LogEventLevel.Verbose:
                    customLogLevel = "ALL";
                    break;

                case LogEventLevel.Warning:
                    customLogLevel = "WARNING";
                    break;
            }

            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CustomLogLevel", customLogLevel));
        }
    }
}