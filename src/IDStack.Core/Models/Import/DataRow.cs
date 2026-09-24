using System.Collections.Generic;
using System.Linq;

namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// One record of imported data: named column values for a single card.
    /// </summary>
    public class DataRow
    {
        /// <summary>Creates an empty row.</summary>
        public DataRow()
        {
            Values = new Dictionary<string, string>();
        }

        /// <summary>Creates a row from column/value pairs.</summary>
        /// <param name="values">Column name to cell text mapping.</param>
        public DataRow(IDictionary<string, string> values)
        {
            Values = new Dictionary<string, string>(values ?? new Dictionary<string, string>());
        }

        /// <summary>Zero-based row position in the source file (excluding the header).</summary>
        public int RowNumber { get; set; }

        /// <summary>Column name to cell text mapping (missing cells are empty strings).</summary>
        public Dictionary<string, string> Values { get; }

        /// <summary>Gets the text of a column or an empty string when absent.</summary>
        /// <param name="columnName">Column name (case-insensitive).</param>
        /// <returns>Cell text, or empty string.</returns>
        public string Get(string columnName)
        {
            if (string.IsNullOrEmpty(columnName))
            {
                return string.Empty;
            }

            foreach (var key in Values.Keys)
            {
                if (string.Equals(key, columnName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return Values[key] ?? string.Empty;
                }
            }

            return string.Empty;
        }

        /// <summary>Gets the distinct column names across the row.</summary>
        /// <returns>Sorted column names.</returns>
        public IEnumerable<string> GetColumnNames()
        {
            return Values.Keys.Distinct().OrderBy(k => k);
        }
    }
}
