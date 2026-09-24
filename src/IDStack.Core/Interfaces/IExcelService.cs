using System.Collections.Generic;
using System.Threading.Tasks;
using IDStack.Core.Models.Import;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Reads tabular data files (.xlsx, .xls, .csv) for card generation.
    /// </summary>
    public interface IExcelService
    {
        /// <summary>Loads a data file and returns columns plus all rows.</summary>
        /// <param name="filePath">Path to a .xlsx, .xls, or .csv file.</param>
        /// <returns>Parsed data.</returns>
        Task<ExcelData> LoadExcelFileAsync(string filePath);

        /// <summary>Reads only the column headers.</summary>
        /// <param name="filePath">Path to a data file.</param>
        /// <returns>Column names in file order.</returns>
        Task<List<string>> GetColumnNamesAsync(string filePath);

        /// <summary>Reads the first rows for preview purposes.</summary>
        /// <param name="filePath">Path to a data file.</param>
        /// <param name="rowCount">Maximum rows to read.</param>
        /// <returns>Parsed preview data.</returns>
        Task<ExcelData> GetPreviewAsync(string filePath, int rowCount);

        /// <summary>Reads all data rows.</summary>
        /// <param name="filePath">Path to a data file.</param>
        /// <returns>All rows excluding the header.</returns>
        Task<List<DataRow>> GetAllRowsAsync(string filePath);

        /// <summary>Quick structural validation of a data file.</summary>
        /// <param name="filePath">Path to check.</param>
        /// <param name="errorMessage">Failure reason when the result is false.</param>
        /// <returns>True when the file can be loaded.</returns>
        bool ValidateExcelFile(string filePath, out string errorMessage);
    }
}
