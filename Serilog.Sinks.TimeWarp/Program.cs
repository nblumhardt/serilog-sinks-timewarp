using System;
using System.Linq;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.TimeWarp
{
    class TimeWarpSink : IDisposable, ILogEventSink
    {
        readonly ILogEventSink _target;
        readonly Func<LogEvent, DateTimeOffset> _getTimestamp;

        public TimeWarpSink(ILogEventSink target, Func<LogEvent, DateTimeOffset> getTimestamp)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _getTimestamp = getTimestamp ?? throw new ArgumentNullException(nameof(getTimestamp));
        }

        public void Dispose()
        {
            (_target as IDisposable)?.Dispose();
        }

        public void Emit(LogEvent logEvent)
        {
            var timestamp = _getTimestamp(logEvent);
            var surrogate = new LogEvent(timestamp, logEvent.Level, logEvent.Exception, logEvent.MessageTemplate,
                                         logEvent.Properties.Select(kv => new LogEventProperty(kv.Key, kv.Value)));
            _target.Emit(surrogate);
        }
    }

    public static class LoggerSinkConfigurationTimeWarpExtensions
    {
        public static LoggerConfiguration TimeWarp(this LoggerSinkConfiguration loggerSinkConfiguration, Func<LogEvent, DateTimeOffset> getTimestamp, Action<LoggerSinkConfiguration> configure)
        {
            return LoggerSinkConfiguration.Wrap(loggerSinkConfiguration, sink => new TimeWarpSink(sink, getTimestamp), configure);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            DateTimeOffset EveryDayIsLikeSunday(LogEvent le) => new DateTimeOffset(
                2017, 11, 26,
                le.Timestamp.Hour,
                le.Timestamp.Minute, 
                le.Timestamp.Second,
                le.Timestamp.Millisecond,
                le.Timestamp.Offset);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.TimeWarp(EveryDayIsLikeSunday, writeTo =>
                    writeTo.Console(outputTemplate: "[{Timestamp:o} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
                .CreateLogger();

            Log.Information("Hello, world!");

            Log.CloseAndFlush();
        }
    }
}
