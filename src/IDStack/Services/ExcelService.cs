using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;
using OfficeOpenXml;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace IDStack.Services
{
    /// <summary>
    /// Loads .xlsx (EPPlus), .xls (NPOI), and .csv files into ExcelData.
    /// Missing cells are returned as empty strings; all I/O is async off the UI thread.
    /// </summary>
    public class ExcelService : IExcelService
    {
        static ExcelService()
        {
            // EPPlus 4.x is LGPL and free for commercial use; no license context needed.
            // ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // EPPlus 5+
        }
        private static readonly string[] SupportedExtensions = { ".xlsx", ".xls", ".csv" };

        /// <inheritdoc />
        public Task<ExcelData> LoadExcelFileAsync(string filePath)
        {
            return Task.Run(() => Load(filePath, int.MaxValue));
        }

        /// <inheritdoc />
        public Task<List<string>> GetColumnNamesAsync(string filePath)
        {
            return Task.Run(() => Load(filePath, 1).ColumnNames);
        }

        /// <inheritdoc />
        public Task<ExcelData> GetPreviewAsync(string filePath, int rowCount)
        {
            return Task.Run(() => Load(filePath, rowCount));
        }

        /// <inheritdoc />
        public Task<List<DataRow>> GetAllRowsAsync(string filePath)
        {
            return Task.Run(() => Load(filePath, int.MaxValue).Rows);
        }

        /// <inheritdoc />
        public bool ValidateExcelFile(string filePath, out string errorMessage)
        {
            return Validate(filePath, out errorMessage);
        }

        private static bool Validate(string filePath, out string errorMessage)
        {
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "No file selected.";
                return false;
            }

            if (!File.Exists(filePath))
            {
                errorMessage = "File not found: " + filePath;
                return false;
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (!SupportedExtensions.Contains(extension))
            {
                errorMessage = "Unsupported file type \"" + extension + "\". Use .xlsx, .xls, or .csv.";
                return false;
            }

            return true;
        }

        private static ExcelData Load(string filePath, int maxRows)
        {
            if (!Validate(filePath, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".xlsx":
                    return LoadXlsx(filePath, maxRows);
                case ".xls":
                    return LoadXls(filePath, maxRows);
                default:
                    return LoadCsv(filePath, maxRows);
            }
        }

        private static ExcelData LoadXlsx(string filePath, int maxRows)
        {
            var file = new FileInfo(filePath);
            using (var package = new ExcelPackage(file))
            {
                var sheet = package.Workbook.Worksheets.Count > 0 ? package.Workbook.Worksheets[1] : null;
                if (sheet == null || sheet.Dimension == null)
                {
                    throw new InvalidOperationException("The workbook contains no data.");
                }

                return BuildData(
                    filePath,
                    "xlsx",
                    sheet.Name,
                    ReadRow(sheet, 1, sheet.Dimension.End.Column),
                    row => ReadRow(sheet, row + 1, sheet.Dimension.End.Column),
                    sheet.Dimension.End.Row - 1,
                    maxRows);
            }
        }

        private static ExcelData LoadXls(string filePath, int maxRows)
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var workbook = new HSSFWorkbook(stream))
            {
                if (workbook.NumberOfSheets == 0)
                {
                    throw new InvalidOperationException("The workbook contains no sheets.");
                }

                var sheet = workbook.GetSheetAt(0);
                var headerRow = sheet.GetRow(sheet.FirstRowNum);
                if (headerRow == null)
                {
                    throw new InvalidOperationException("The sheet contains no data.");
                }

                var lastCell = headerRow.LastCellNum - 1;
                return BuildData(
                    filePath,
                    "xls",
                    sheet.SheetName,
                    ReadNpoiRow(headerRow, lastCell),
                    row => ReadNpoiRow(sheet.GetRow(row + 1 + sheet.FirstRowNum), lastCell),
                    sheet.LastRowNum - sheet.FirstRowNum,
                    maxRows);
            }
        }

        private static ExcelData LoadCsv(string filePath, int maxRows)
        {
            var lines = File.ReadAllLines(filePath, DetectEncoding(filePath));
            var nonEmpty = lines.Where(l => l.Trim().Length > 0).ToList();
            if (nonEmpty.Count == 0)
            {
                throw new InvalidOperationException("The file contains no data.");
            }

            var columns = SplitCsvLine(nonEmpty[0]);
            return BuildData(
                filePath,
                "csv",
                Path.GetFileNameWithoutExtension(filePath),
                columns,
                row => SplitCsvLine(nonEmpty[row + 1]),
                nonEmpty.Count - 1,
                maxRows);
        }

        private static ExcelData BuildData(
            string filePath,
            string format,
            string sheetName,
            IList<string> columns,
            Func<int, IList<string>> readRow,
            int totalRows,
            int maxRows)
        {
            var data = new ExcelData
            {
                SourceFilePath = filePath,
                Format = format,
                SheetName = sheetName,
                ColumnNames = columns.Select((c, i) =>
                {
                    var name = (c ?? string.Empty).Trim();
                    return name.Length > 0 ? name : "Column" + (i + 1);
                }).ToList()
            };

            var rowCount = Math.Min(totalRows, maxRows);
            for (var row = 0; row < rowCount; row++)
            {
                var cells = readRow(row);
                var values = new Dictionary<string, string>();
                for (var col = 0; col < data.ColumnNames.Count; col++)
                {
                    var text = col < cells.Count ? (cells[col] ?? string.Empty).Trim() : string.Empty;
                    values[data.ColumnNames[col]] = text;
                }

                data.Rows.Add(new DataRow(values) { RowNumber = row + 1 });
            }

            return data;
        }

        private static IList<string> ReadRow(ExcelWorksheet sheet, int row, int columnCount)
        {
            var cells = new List<string>();
            for (var col = 1; col <= columnCount; col++)
            {
                cells.Add(sheet.Cells[row, col].Text);
            }

            return cells;
        }

        private static IList<string> ReadNpoiRow(IRow row, int lastCellIndex)
        {
            var cells = new List<string>();
            if (row == null)
            {
                return cells;
            }

            for (var col = 0; col <= lastCellIndex; col++)
            {
                var cell = row.GetCell(col);
                cells.Add(cell == null ? string.Empty : GetCellText(cell));
            }

            return cells;
        }

        private static string GetCellText(ICell cell)
        {
            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue ?? string.Empty;
                case CellType.Numeric:
                    return DateUtil.IsCellDateFormatted(cell)
                        ? cell.DateCellValue.ToString("yyyy-MM-dd")
                        : cell.NumericCellValue.ToString("0.######");
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                default:
                    return cell.ToString() ?? string.Empty;
            }
        }

        private static List<string> SplitCsvLine(string line)
        {
            var cells = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(ch);
                    }
                }
                else if (ch == '"')
                {
                    inQuotes = true;
                }
                else if (ch == ',' || ch == ';' || ch == '\t')
                {
                    cells.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            cells.Add(current.ToString().Trim());
            return cells;
        }

        private static Encoding DetectEncoding(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.Default, true))
            {
                reader.Peek();
                return reader.CurrentEncoding;
            }
        }
    }
}
