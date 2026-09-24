namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// Binding between a template placeholder and an Excel column.
    /// </summary>
    public class ColumnMapping
    {
        /// <summary>Creates a mapping.</summary>
        /// <param name="placeholderName">Placeholder name without braces.</param>
        /// <param name="columnName">Excel column name, or null when unmapped.</param>
        public ColumnMapping(string placeholderName, string columnName)
        {
            PlaceholderName = placeholderName;
            ColumnName = columnName;
        }

        /// <summary>Placeholder name this mapping binds (no braces).</summary>
        public string PlaceholderName { get; set; }

        /// <summary>Excel column name bound to the placeholder; null when unmapped.</summary>
        public string ColumnName { get; set; }

        /// <summary>Whether the placeholder is bound to a column.</summary>
        public bool IsMapped => !string.IsNullOrWhiteSpace(ColumnName);

        /// <summary>Whether this placeholder must be mapped before generation.</summary>
        public bool IsRequired { get; set; }
    }
}
