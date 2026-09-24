using System.Collections.Generic;

namespace IDStack.Core.Models.Import
{
    /// <summary>
    /// Result of loading a tabular data file: columns plus all rows.
    /// </summary>
    public class ExcelData
    {
        /// <summary>Creates empty data.</summary>
        public ExcelData()
        {
            ColumnNames = new List<string>();
            Rows = new List<DataRow>();
        }

        /// <summary>Source file path the data was loaded from.</summary>
        public string SourceFilePath { get; set; }

        /// <summary>Detected format: xlsx, xls, or csv.</summary>
        public string Format { get; set; }

        /// <summary>Sheet or table name the data came from.</summary>
        public string SheetName { get; set; }

        /// <summary>Column headers in file order.</summary>
        public List<string> ColumnNames { get; set; }

        /// <summary>All data rows (excluding the header).</summary>
        public List<DataRow> Rows { get; set; }

        /// <summary>Total row count.</summary>
        public int RowCount => Rows.Count;
    }
}
