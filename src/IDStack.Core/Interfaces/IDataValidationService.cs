using System.Collections.Generic;
using IDStack.Core.Models.Import;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Validates imported data before batch generation.
    /// </summary>
    public interface IDataValidationService
    {
        /// <summary>Runs all checks over imported data.</summary>
        /// <param name="data">Imported rows.</param>
        /// <param name="requiredColumns">Columns that must be non-empty.</param>
        /// <param name="idColumn">Column checked for duplicates, or null to skip the check.</param>
        /// <param name="photos">Photos used for photo-path checking, or null to skip.</param>
        /// <returns>All issues found, ordered by row.</returns>
        List<ValidationIssue> Validate(
            ExcelData data,
            IEnumerable<string> requiredColumns,
            string idColumn,
            List<PhotoRecord> photos);
    }
}
