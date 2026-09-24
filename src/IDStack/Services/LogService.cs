using System;
using System.IO;
using IDStack.Core.Constants;
using IDStack.Core.Interfaces;

namespace IDStack.Services
{
    /// <summary>
    /// File-based logger writing to %APPDATA%\IDStack\logs\yyyy-MM-dd.log.
    /// Never throws: logging failures are silently ignored by design.
    /// </summary>
    public class LogService : ILogger
    {
        private readonly string _logDirectory;
        private readonly object _lock = new object();

        /// <summary>
        /// Creates the logger, writing into the given directory (defaults to %APPDATA%\IDStack\logs).
        /// </summary>
        /// <param name="logDirectory">Target directory.</param>
        public LogService(string logDirectory = null)
        {
            _logDirectory = logDirectory
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppConstants.AppDataFolderName,
                    "logs");
        }

        /// <inheritdoc />
        public void Info(string message)
        {
            Write("INFO", message, null);
        }

        /// <inheritdoc />
        public void Warn(string message)
        {
            Write("WARN", message, null);
        }

        /// <inheritdoc />
        public void Error(Exception exception, string context = null)
        {
            Write("ERROR", context, exception);
        }

        private void Write(string level, string message, Exception exception)
        {
            try
            {
                lock (_lock)
                {
                    Directory.CreateDirectory(_logDirectory);
                    var path = Path.Combine(_logDirectory, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                    var line = string.Format(
                        "{0:HH:mm:ss.fff} [{1}] {2}",
                        DateTime.Now,
                        level,
                        message ?? string.Empty);
                    if (exception != null)
                    {
                        line += Environment.NewLine + exception;
                    }

                    File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging must never break the app.
            }
        }
    }
}
