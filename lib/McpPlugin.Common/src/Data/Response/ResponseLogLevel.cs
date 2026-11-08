// RedPand MCP. Licensed under the Apache License, Version 2.0.

using System;
using Microsoft.Extensions.Logging;

namespace com.RedPand.MCP.Plugin.Common.Model
{
    /// <summary>
    /// How loudly a failed answer is logged. Only a fault of the plugin or the server (<see cref="ResponseErrorKind.Internal"/>)
    /// is an error, logged with its exception. Every other failure was answered normally: the request was wrong, the tool
    /// was not found, the editor was busy or reloading scripts, the call ran out of time. Those are one-line warnings, so a
    /// red line in the console always means something is broken.
    /// </summary>
    public static class ResponseLogLevel
    {
        public static LogLevel Of(ResponseErrorKind kind)
            => kind == ResponseErrorKind.Internal ? LogLevel.Error : LogLevel.Warning;

        /// <summary>Logs a failed answer at <see cref="Of"/>; the exception (and its stack) is kept only for an error.</summary>
        public static void LogFailure(ILogger logger, ResponseErrorKind kind, Exception? ex, string message)
        {
            var level = Of(kind);
            logger.Log(level, level == LogLevel.Error ? ex : null, "{Message}", message);
        }
    }
}
