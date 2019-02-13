using System;
using Serilog.Core;
using Serilog.Events;
using SerilogTimings.Extensions;
using SerilogTimings.Tests.Support;
using Xunit;

namespace SerilogTimings.Tests
{
    public class OperationEnrichmentTests
    {
        private static void AssertScalarPropertyOfSingleEvent<T>(CollectingLogger logger, string propertyName, T expected)
        {
            var ev = Assert.Single(logger.Events);
            Assert.True(ev.Properties.TryGetValue(propertyName, out var value));
            Assert.Equal(
                expected,
                Assert.IsType<T>(Assert.IsType<ScalarValue>(value).Value)
                );
        }

        [Fact]
        public void AbandonRecordsAddedProperty()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith("Value", 42);
            op.Dispose();
            AssertScalarPropertyOfSingleEvent(logger, "Value", 42);
        }

        [Fact]
        public void CompleteRecordsAddedProperty()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith("Value", 42);
            op.Complete();
            AssertScalarPropertyOfSingleEvent(logger, "Value", 42);
        }

        [Fact]
        public void CompleteOverridesAddedProperty()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith("Value", 42);
            op.Complete("Value", 43);
            AssertScalarPropertyOfSingleEvent(logger, "Value", 43);
        }

        [Fact]
        public void AbandonRecordsPropertyAddedViaEnricher()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith(new Enricher("Value", 42));
            op.Dispose();
            AssertScalarPropertyOfSingleEvent(logger, "Value", 42);
        }

        [Fact]
        public void CompleteRecordsPropertyAddedViaEnricher()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith(new Enricher("Value", 42));
            op.Complete();
            AssertScalarPropertyOfSingleEvent(logger, "Value", 42);
        }

        [Fact]
        public void OnceCanceledDisposeDoesNotInvokeEnricher()
        {
            var enricher = new Enricher(null, null);
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith(enricher);
            op.Cancel();
            op.Dispose();
            Assert.False(enricher.WasCalled);
        }

        [Fact]
        public void OnceCanceledCompleteDoesNotInvokeEnricher()
        {
            var enricher = new Enricher(null, null);
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith(enricher);
            op.Cancel();
            op.Complete();
            op.Dispose();
            Assert.False(enricher.WasCalled);
        }

        [Fact]
        public void AbandonRecordsPropertiesAddedViaEnrichers()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith(new ILogEventEnricher[] {
                new Enricher("Question", "unknown"),
                new Enricher("Answer", 42)
            });
            op.Dispose();
            AssertScalarPropertyOfSingleEvent(logger, "Question", "unknown");
            AssertScalarPropertyOfSingleEvent(logger, "Answer", 42);
        }

        [Fact]
        public void CompleteRecordsPropertiesAddedViaEnrichers()
        {
            var logger = new CollectingLogger();
            var op = logger.Logger.BeginOperation("Test");
            op.EnrichWith(new ILogEventEnricher[] {
                new Enricher("Question", "unknown"),
                new Enricher("Answer", 42)
            });
            op.Complete();
            AssertScalarPropertyOfSingleEvent(logger, "Question", "unknown");
            AssertScalarPropertyOfSingleEvent(logger, "Answer", 42);
        }

        [Fact]
        public void PropertyBonanza()
        {
            var logger = new CollectingLogger();
            logger.Logger
                .BeginOperation("Test")
                .EnrichWith(new ILogEventEnricher[] {
                    new Enricher("Question", "unknown"),
                    new Enricher("Answer", 42)
                })
                .EnrichWith(new Enricher("Don't", "panic"))
                .EnrichWith("And bring", "towel")
                .Complete();
            AssertScalarPropertyOfSingleEvent(logger, "Question", "unknown");
            AssertScalarPropertyOfSingleEvent(logger, "Answer", 42);
            AssertScalarPropertyOfSingleEvent(logger, "Don't", "panic");
            AssertScalarPropertyOfSingleEvent(logger, "And bring", "towel");
        }

        [Fact]
        public void DisposedOperationDoesNotCaptureException()
        {
            var logger = new CollectingLogger();
            Assert.Throws<InvalidOperationException>((Action) (() =>
                {
                    using (logger.Logger.BeginOperation("Test"))
                    {
                        throw new InvalidOperationException();
                    }
                })
            );
            Assert.Null(Assert.Single(logger.Events).Exception);
        }

        [Fact]
        public void DisposeRecordsSetException()
        {
            var exception = new InvalidOperationException();
            var logger = new CollectingLogger();
            Assert.Throws<InvalidOperationException>((Action) (() =>
                {
                    using (var op = logger.Logger.BeginOperation("Test"))
                    {
                        try
                        {
                            throw exception;
                        }
                        catch (Exception e)
                        {
                            op.SetException(e);
                            throw;
                        }
                    }
                })
            );
            Assert.Same(exception, Assert.Single(logger.Events).Exception);
        }

        [Fact]
        public void CompleteRecordsSetException()
        {
            var exception = new InvalidOperationException();
            var logger = new CollectingLogger();
            using (var op = logger.Logger.BeginOperation("Test"))
            {
                try
                {
                    throw exception;
                }
                catch (Exception e)
                {
                    op.SetException(e);
                    op.Complete();
                }
            }
            Assert.Same(exception, Assert.Single(logger.Events).Exception);
        }

        [Fact]
        public void SetExceptionAndRethrowSkipsCatchBlock()
        {
            var exception = new InvalidOperationException();
            var logger = new CollectingLogger();
            Assert.Throws<InvalidOperationException>((Action) (() =>
                {
                    using (var op = logger.Logger.BeginOperation("Test"))
                    {
                        try
                        {
                            throw exception;
                        }
                        catch (Exception e) when (op.SetExceptionAndRethrow(e))
                        {
                            throw new Exception("This catch block should be skipped");
                        }
                    }
                })
            );
            Assert.Same(exception, Assert.Single(logger.Events).Exception);
        }

        private class Enricher : ILogEventEnricher
        {
            private string _propertyName;
            private object _value;

            public Enricher(string propertyName, object value)
            {
                _propertyName = propertyName;
                _value = value;
            }

            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                WasCalled = true;
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(_propertyName, _value));
            }

            public bool WasCalled { get; private set; }
        }
    }
}
