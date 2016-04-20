using System.Collections.Generic;
using Serilog;
using Serilog.Events;

namespace SerilogTimings.Tests.Support
{
    public class CollectingLogger
    {
        public ILogger Logger { get; }
        public List<LogEvent> Events { get; }
                 
        public CollectingLogger()
        {
            var sink = new CollectionSink();

            Logger = new LoggerConfiguration()
                .MinimumLevel.Is(LevelAlias.Minimum)
                .WriteTo.Sink(sink)
                .CreateLogger();

            Events = sink.Events;
        }
    }
}