using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Serilog;
using Serilog.Events;

using SerilogTimings;
using SerilogTimings.Extensions;
using SerilogTimings.Tests;
using SerilogTimings.Tests.Support;
using Xunit;

namespace SerilogTimings.Tests
{
    public class OperationTests
    {
        [Fact]
        public void DisposeRecordsCompletionOfTimings()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.TimeOperation("Test");
            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
            Assert.Equal(LogEventLevel.Information, logger.Events.Single().Level);
        }

        [Fact]
        public void CompleteRecordsCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Complete();
            Assert.Equal(1, logger.Events.Count);
            Assert.Equal(LogEventLevel.Information, logger.Events.Single().Level);

            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
        }

        [Fact]
        public void DisposeRecordsAbandonmentOfIncompleteOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
            Assert.Equal(LogEventLevel.Warning, logger.Events.Single().Level);

            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
        }

        [Fact]
        public void CompleteRecordsResultsOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Complete("Value", 42);
            Assert.Equal(1, logger.Events.Count);
            Assert.True(logger.Events.Single().Properties.ContainsKey("Value"));
        }

        [Fact]
        public void OnceCanceledDisposeDoesNotRecordCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Cancel();
            op.Dispose();
            Assert.Equal(0, logger.Events.Count);
        }

        [Fact]
        public void OnceCanceledCompleteDoesNotRecordCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Cancel();
            op.Complete();
            op.Dispose();
            Assert.Equal(0, logger.Events.Count);
        }

        [Fact]
        public void CustomCompletionLevelsAreApplied()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.OperationAt(LogEventLevel.Error).Time("Test");
            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
            Assert.Equal(LogEventLevel.Error, logger.Events.Single().Level);
        }

        [Fact]
        public void AbandonmentLevelsDefaultToCustomCompletionLevelIfApplied()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.OperationAt(LogEventLevel.Error).Begin("Test");
            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
            Assert.Equal(LogEventLevel.Error, logger.Events.Single().Level);
        }

        [Fact]
        public void CustomAbandonmentLevelsAreApplied()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.OperationAt(LogEventLevel.Error, LogEventLevel.Fatal).Begin("Test");
            op.Dispose();
            Assert.Equal(1, logger.Events.Count);
            Assert.Equal(LogEventLevel.Fatal, logger.Events.Single().Level);
        }

        [Fact]
        public void IfNeitherLevelIsEnabledACachedResultIsReturned()
        {
            var logger = new LoggerConfiguration().CreateLogger(); // Information
            var op = logger.OperationAt(LogEventLevel.Verbose).Time("Test");
            var op2 = logger.OperationAt(LogEventLevel.Verbose).Time("Test");
            Assert.Same(op, op2);
        }

        [Fact]
        public void LoggerContextIsPreserved()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger
                .ForContext<OperationTests>().BeginOperation("Test");
            op.Complete();

            var sourceContext = (logger.Events.Single().Properties["SourceContext"] as ScalarValue).Value;
            Assert.Equal(sourceContext, typeof(OperationTests).FullName);
        }

        [Theory]
        [InlineData(100, 250, LogEventLevel.Information)]
        [InlineData(500, 250, LogEventLevel.Warning)]
        public async Task WarnIfThresholdExceeded(int operationDurationInMs, int thresholdInMs, LogEventLevel thresholdLevel)
        {
            var operationDuration = TimeSpan.FromMilliseconds(operationDurationInMs);
            var threshold = TimeSpan.FromMilliseconds(thresholdInMs);
            var logger = new CollectingLogger();

            var operation = logger.Logger.BeginOperation(
                o => o.Elapsed > threshold ? thresholdLevel : LogEventLevel.Information,
                o => LogEventLevel.Fatal,
                "Test");

            await Task.Delay(operationDuration);

            operation.Complete();

            Assert.Equal(thresholdLevel, logger.Events.Single().Level);
        }
    }
}