using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using SerilogTimings.Extensions;
using SerilogTimings.Tests.Support;
using Xunit;

namespace SerilogTimings.Tests
{
    public class OperationTests
    {
        const string OutcomeCompleted = "completed";
        const string OutcomeAbandoned = "abandoned";

        // ReSharper disable once UnusedMethodReturnValue.Local
        static LogEvent AssertSingleCompletionEvent(CollectingLogger logger, LogEventLevel expectedLevel,
            string expectedOutcome)
        {
            T GetScalarPropertyValue<T>(LogEvent e, string key)
            {
                Assert.True(e.Properties.TryGetValue(key, out var value));
                return Assert.IsType<T>(Assert.IsType<ScalarValue>(value).Value);
            }

            var ev = Assert.Single(logger.Events);
            Assert.NotNull(ev);
            Assert.Equal(expectedLevel, ev.Level);

            Assert.Equal(expectedOutcome, GetScalarPropertyValue<string>(ev, nameof(Operation.Properties.Outcome)));
            GetScalarPropertyValue<double>(ev, nameof(Operation.Properties.Elapsed));
            return ev;
        }

        static double GetElapsedMilliseconds(CollectingLogger logger)
        {
            var elapsed = (double)((ScalarValue)logger.Events.Single().Properties[nameof(Operation.Properties.Elapsed)]).Value;
            return elapsed;
        }

        [Fact]
        public void DisposeRecordsCompletionOfTimings()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.TimeOperation("Test");
            op.Dispose();
            AssertSingleCompletionEvent(logger, LogEventLevel.Information, OutcomeCompleted);
        }

        [Fact]
        public void CompleteRecordsCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Complete();
            AssertSingleCompletionEvent(logger, LogEventLevel.Information, OutcomeCompleted);

            op.Dispose();
            Assert.Single(logger.Events);
        }

        [Fact]
        public void DisposeRecordsAbandonmentOfIncompleteOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Dispose();
            AssertSingleCompletionEvent(logger, LogEventLevel.Warning, OutcomeAbandoned);

            op.Dispose();
            Assert.Single(logger.Events);
        }

        [Fact]
        public void AbandonRecordsAbandonmentOfBegunOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Abandon();
            AssertSingleCompletionEvent(logger, LogEventLevel.Warning, OutcomeAbandoned);

            op.Dispose();
            Assert.Single(logger.Events);
        }

        [Fact]
        public void CompleteRecordsResultsOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Complete("Value", 42);
            Assert.Single(logger.Events);
            Assert.True(logger.Events.Single().Properties.ContainsKey("Value"));
        }

        [Fact]
        public void OnceCanceledDisposeDoesNotRecordCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Cancel();
            op.Dispose();
            Assert.Empty(logger.Events);
        }

        [Fact]
        public void OnceCanceledCompleteDoesNotRecordCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Cancel();
            op.Complete();
            op.Dispose();
            Assert.Empty(logger.Events);
        }

        [Fact]
        public void OnceCanceledAbandonDoesNotRecordCompletionOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Cancel();
            op.Abandon();
            op.Dispose();
            Assert.Empty(logger.Events);
        }

        [Fact]
        public void OnceCompletedAbandonDoesNotRecordAbandonmentOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Complete();
            AssertSingleCompletionEvent(logger, LogEventLevel.Information, OutcomeCompleted);

            op.Abandon();
            op.Dispose();
            Assert.Single(logger.Events);
        }

        [Fact]
        public void OnceAbandonedCompleteDoesNotRecordAbandonmentOfOperations()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.Abandon();
            AssertSingleCompletionEvent(logger, LogEventLevel.Warning, OutcomeAbandoned);

            op.Complete();
            op.Dispose();
            Assert.Single(logger.Events);
        }

        [Fact]
        public void CustomCompletionLevelsAreApplied()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.OperationAt(LogEventLevel.Error).Time("Test");
            op.Dispose();
            AssertSingleCompletionEvent(logger, LogEventLevel.Error, OutcomeCompleted);
        }

        [Fact]
        public void AbandonmentLevelsDefaultToCustomCompletionLevelIfApplied()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.OperationAt(LogEventLevel.Error).Begin("Test");
            op.Dispose();
            AssertSingleCompletionEvent(logger, LogEventLevel.Error, OutcomeAbandoned);
        }

        [Fact]
        public void CustomAbandonmentLevelsAreApplied()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.OperationAt(LogEventLevel.Error, LogEventLevel.Fatal).Begin("Test");
            op.Dispose();
            AssertSingleCompletionEvent(logger, LogEventLevel.Fatal, OutcomeAbandoned);
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

            var sourceContext = ((ScalarValue)logger.Events.Single().Properties["SourceContext"]).Value;
            Assert.Equal(sourceContext, typeof(OperationTests).FullName);
        }

        [Fact]
        public void CompleteRecordsOperationId()
        {
            var innerLogger = new CollectingLogger();
            var logger = new LoggerConfiguration()
                .WriteTo.Logger(innerLogger.Logger)
                .Enrich.FromLogContext()
                .CreateLogger();

            var op = logger.BeginOperation("Test");
            op.Complete();
            Assert.True(
                Assert.Single(innerLogger.Events)
                    .Properties.ContainsKey(nameof(Operation.Properties.OperationId))
            );
        }

        [Fact]
        public void AbandonRecordsOperationId()
        {
            var innerLogger = new CollectingLogger();
            var logger = new LoggerConfiguration()
                .WriteTo.Logger(innerLogger.Logger)
                .Enrich.FromLogContext()
                .CreateLogger();

            var op = logger.BeginOperation("Test");
            op.Dispose();
            Assert.True(
                Assert.Single(innerLogger.Events)
                    .Properties.ContainsKey(nameof(Operation.Properties.OperationId))
            );
        }

        [Theory]
        [InlineData(10, 20)]
        [InlineData(100, 20)]
        [InlineData(1000, 50)]
        //hard timing numbers smell but should catch any order of magnitude errors in frequency calculations at least.
        public async Task TimingTolerance(int delay, int operationTolerance)
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.TimeOperation("Test");
            await Task.Delay(delay);
            op.Dispose();

            var elapsed = GetElapsedMilliseconds(logger);
            Assert.InRange(elapsed, delay, delay + operationTolerance);
        }

        [Fact]
        public async Task ElapsedUpdatesDuringOperation()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            var first = op.Elapsed;
            await Task.Delay(10);
            var second = op.Elapsed;
            await Task.Delay(10);
            op.Complete();
            var third = op.Elapsed;
            await Task.Delay(10);
            var fourth = op.Elapsed;
            await Task.Delay(10);
            op.Complete();
            var fifth = op.Elapsed;

            Assert.NotEqual(first, second);
            Assert.NotEqual(second, third);
            Assert.Equal(third, fourth);
            Assert.Equal(fourth, fifth);
        }
    }
}
