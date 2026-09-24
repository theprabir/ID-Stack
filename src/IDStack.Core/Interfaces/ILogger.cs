using System;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Minimal logging abstraction for diagnostics.
    /// </summary>
    public interface ILogger
    {
        /// <summary>Logs an informational message.</summary>
        /// <param name="message">Message text.</param>
        void Info(string message);

        /// <summary>Logs a warning message.</summary>
        /// <param name="message">Message text.</param>
        void Warn(string message);

        /// <summary>Logs an exception with optional context.</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="context">Optional context describing where it happened.</param>
        void Error(Exception exception, string context = null);
    }
}
