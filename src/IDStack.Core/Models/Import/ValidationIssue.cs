namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// A data-quality problem found during validation.
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>Severity of an issue.</summary>
        public enum SeverityLevel
        {
            /// <summary>Informational only; generation can proceed.</summary>
            Info,

            /// <summary>Potential problem; generation can proceed with care.</summary>
            Warning,

            /// <summary>Blocking problem; the row should be skipped or fixed.</summary>
            Error
        }

        /// <summary>Creates an issue.</summary>
        /// <param name="severity">Severity.</param>
        /// <param name="rowNumber">Row number, or 0 for file-level issues.</param>
        /// <param name="message">Human-readable description.</param>
        public ValidationIssue(SeverityLevel severity, int rowNumber, string message)
        {
            Severity = severity;
            RowNumber = rowNumber;
            Message = message;
        }

        /// <summary>How severe the issue is.</summary>
        public SeverityLevel Severity { get; }

        /// <summary>Affected row number (1-based), or 0 for file-level issues.</summary>
        public int RowNumber { get; }

        /// <summary>Human-readable description.</summary>
        public string Message { get; }

        /// <summary>Returns the formatted issue text.</summary>
        /// <returns>Row-prefixed message.</returns>
        public override string ToString()
        {
            return RowNumber > 0
                ? "Row " + RowNumber + ": " + Message
                : Message;
        }
    }
}
