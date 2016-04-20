using System.Collections.Generic;
using Serilog.Core;
using Serilog.Events;

namespace SerilogTimings.Tests.Support
{
    public class CollectionSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = new List<LogEvent>();

        public void Emit(LogEvent le)
        {
            Events.Add(le);
        }
    }
}
