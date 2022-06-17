// Copyright 2019 SerilogTimings Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Serilog.Core;

namespace SerilogTimings
{
    /// <summary>
    /// Exception-handling related helpers for <see cref="Operation"/>.
    /// </summary>
    public static class OperationExtensions
    {
        /// <summary>
        /// Enriches resulting log event with the given exception and skips exception-handling block.
        /// </summary>
        /// <param name="operation">Operation to enrich with exception.</param>
        /// <param name="exception">Exception related to the event.</param>
        /// <returns><c>false</c></returns>
        /// <seealso cref="Operation.SetException"/>
        /// <example>
        /// <code>
        /// using (var op = Operation.Begin(...)
        /// {
        ///     try
        ///     {
        ///         //Do something
        ///         op.Complete();
        ///     }
        ///     catch (Exception e) when (op.SetExceptionAndRethrow(e))
        ///     {
        ///         //this will never be called
        ///     }
        /// }
        /// </code>
        /// </example>
        public static bool SetExceptionAndRethrow(this Operation operation, Exception exception)
        {
            operation.SetException(exception);
            return false;
        }

        /// <summary>
        /// Complete the timed operation enriching it with provided enricher.
        /// </summary>
        /// <param name="operation">Operation to enrich and complete.</param>
        /// <param name="enricher">Enricher that applies in the context.</param>
        /// <seealso cref="Operation.Complete()"/>
        /// <seealso cref="Operation.EnrichWith(ILogEventEnricher)"/>
        public static void Complete(this Operation operation, ILogEventEnricher enricher)
            => operation.EnrichWith(enricher).Complete();

        /// <summary>
        /// Complete the timed operation enriching it with provided enrichers.
        /// </summary>
        /// <param name="operation">Operation to enrich and complete.</param>
        /// <param name="enrichers">Enrichers that apply in the context.</param>
        /// <seealso cref="Operation.Complete()"/>
        /// <seealso cref="Operation.EnrichWith(IEnumerable{ILogEventEnricher})"/>
        public static void Complete(this Operation operation, IEnumerable<ILogEventEnricher> enrichers)
            => operation.EnrichWith(enrichers).Complete();

        /// <summary>
        /// Abandon the timed operation with an included result value.
        /// </summary>
        /// <param name="operation">Operation to enrich and abandon.</param>
        /// <param name="resultPropertyName">The name for the property to attach to the event.</param>
        /// <param name="result">The result value.</param>
        /// <param name="destructureObjects">If true, the property value will be destructured (serialized).</param>
        /// <seealso cref="Operation.Abandon()"/>
        /// <seealso cref="Operation.EnrichWith(string,object,bool)"/>
        public static void Abandon(this Operation operation, string resultPropertyName, object result, bool destructureObjects = false)
            => operation.EnrichWith(resultPropertyName, result, destructureObjects).Abandon();

        /// <summary>
        /// Abandon the timed operation enriching it with provided enricher.
        /// </summary>
        /// <param name="operation">Operation to enrich and abandon.</param>
        /// <param name="enricher">Enricher that applies in the context.</param>
        /// <seealso cref="Operation.Abandon()"/>
        /// <seealso cref="Operation.EnrichWith(ILogEventEnricher)"/>
        public static void Abandon(this Operation operation, ILogEventEnricher enricher)
            => operation.EnrichWith(enricher).Abandon();

        /// <summary>
        /// Abandon the timed operation enriching it with provided enrichers.
        /// </summary>
        /// <param name="operation">Operation to enrich and abandon.</param>
        /// <param name="enrichers">Enrichers that apply in the context.</param>
        /// <seealso cref="Operation.Abandon()"/>
        /// <seealso cref="Operation.EnrichWith(IEnumerable{ILogEventEnricher})"/>
        public static void Abandon(this Operation operation, IEnumerable<ILogEventEnricher> enrichers)
            => operation.EnrichWith(enrichers).Abandon();

        /// <summary>
        /// Abandon the timed operation with an included exception.
        /// </summary>
        /// <param name="operation">Operation to enrich and abandon.</param>
        /// <param name="exception">Enricher related to the event.</param>
        /// <seealso cref="Operation.Abandon()"/>
        /// <seealso cref="Operation.SetException(Exception)"/>
        public static void Abandon(this Operation operation, Exception exception)
            => operation.SetException(exception).Abandon();
    }
}
