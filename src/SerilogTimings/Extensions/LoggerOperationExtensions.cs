// Copyright 2016 SerilogTimings Contributors
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

using System;
using Serilog;

namespace SerilogTimings.Extensions
{
    /// <summary>
    /// Extends <see cref="ILogger"/> with timed operations.
    /// </summary>
    public static class LoggerOperationExtensions
    {
        public static IDisposable TimeOperation(this ILogger logger, string messageTemplate, params object[] args)
        {
            return new Operation(logger, messageTemplate, args, CompletionBehaviour.Complete);
        }

        public static Operation BeginOperation(this ILogger logger, string messageTemplate, params object[] args)
        {
            return new Operation(logger, messageTemplate, args, CompletionBehaviour.Abandon);
        }
    }
}
