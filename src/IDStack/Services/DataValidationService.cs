using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;

namespace IDStack.Services
{
    /// <summary>
    /// Checks imported data for the problems that break batch generation:
    /// missing required values, duplicate IDs, and photos that no longer exist.
    /// </summary>
    public class DataValidationService : IDataValidationService
    {
        /// <inheritdoc />
        public List<ValidationIssue> Validate(
            ExcelData data,
            IEnumerable<string> requiredColumns,
            string idColumn,
            List<PhotoRecord> photos,
            string photoMatchColumn = null)
        {
            var issues = new List<ValidationIssue>();

            if (data == null)
            {
                issues.Add(new ValidationIssue(ValidationIssue.SeverityLevel.Error, 0, "No data loaded."));
                return issues;
            }

            if (data.Rows.Count == 0)
            {
                issues.Add(new ValidationIssue(ValidationIssue.SeverityLevel.Error, 0, "The data file contains no rows."));
                return issues;
            }

            CheckRequiredColumns(data, requiredColumns, issues);
            CheckDuplicateIds(data, idColumn, issues);
            CheckPhotoFiles(data, photos, photoMatchColumn, issues);

            return issues
                .OrderBy(i => i.RowNumber)
                .ThenBy(i => i.Severity)
                .ToList();
        }

        private static void CheckRequiredColumns(
            ExcelData data, IEnumerable<string> requiredColumns, List<ValidationIssue> issues)
        {
            if (requiredColumns == null)
            {
                return;
            }

            foreach (var column in requiredColumns.Where(c => !string.IsNullOrWhiteSpace(c)))
            {
                if (!data.ColumnNames.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(new ValidationIssue(
                        ValidationIssue.SeverityLevel.Error, 0,
                        "Required column \"" + column + "\" is missing from the data file."));
                    continue;
                }

                foreach (var row in data.Rows)
                {
                    if (string.IsNullOrWhiteSpace(row.Get(column)))
                    {
                        issues.Add(new ValidationIssue(
                            ValidationIssue.SeverityLevel.Error, row.RowNumber,
                            "\"" + column + "\" is empty."));
                    }
                }
            }
        }

        private static void CheckDuplicateIds(
            ExcelData data, string idColumn, List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(idColumn))
            {
                return;
            }

            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in data.Rows)
            {
                var id = row.Get(idColumn);
                if (id.Length == 0)
                {
                    continue;
                }

                if (seen.TryGetValue(id, out var firstRow))
                {
                    issues.Add(new ValidationIssue(
                        ValidationIssue.SeverityLevel.Warning, row.RowNumber,
                        "Duplicate " + idColumn + " \"" + id + "\" (first seen in row " + firstRow + ")."));
                }
                else
                {
                    seen[id] = row.RowNumber;
                }
            }
        }

        private static void CheckPhotoFiles(
            ExcelData data, List<PhotoRecord> photos, string photoMatchColumn, List<ValidationIssue> issues)
        {
            if (photos == null || string.IsNullOrWhiteSpace(photoMatchColumn))
            {
                return;
            }

            var keys = new HashSet<string>(
                photos.Select(p => Path.GetFileNameWithoutExtension(p.FileName)),
                StringComparer.OrdinalIgnoreCase);

            foreach (var row in data.Rows)
            {
                var value = row.Get(photoMatchColumn);
                if (value.Length == 0)
                {
                    continue;
                }

                if (!keys.Contains(Path.GetFileNameWithoutExtension(value)) && !File.Exists(value))
                {
                    issues.Add(new ValidationIssue(
                        ValidationIssue.SeverityLevel.Warning, row.RowNumber,
                        "Photo \"" + value + "\" (column \"" + photoMatchColumn + "\") was not found in the photo folder."));
                }
            }
        }

        private static bool LooksLikeFileName(string value)
        {
            return value.IndexOf('.') > 0 &&
                   value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
                   value.Length < 260;
        }
    }
}
