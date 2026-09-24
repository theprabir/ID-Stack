using System;
using System.IO;
using System.Threading.Tasks;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// Handles file picking, loading, and preview of the Excel/CSV data source.
    /// </summary>
    public class ExcelImportViewModel : ViewModelBase
    {
        private readonly IExcelService _excelService;

        private string _fileName;
        private string _statusText;
        private bool _isLoading;
        private ExcelData _data;

        /// <summary>Raised after the data file loads successfully.</summary>
        public event EventHandler DataLoaded;

        /// <summary>
        /// Creates the Excel import view model.
        /// </summary>
        /// <param name="excelService">Excel loading service.</param>
        public ExcelImportViewModel(IExcelService excelService)
        {
            _excelService = excelService ?? throw new ArgumentNullException(nameof(excelService));

            ImportFileCommand = new RelayCommand(_ => ImportRequested?.Invoke(this, EventArgs.Empty), _ => !IsLoading);
            ClearCommand = new RelayCommand(_ => Clear(), _ => Data != null);
        }

        /// <summary>Raised when the view model wants a file-open dialog.</summary>
        public event EventHandler ImportRequested;

        /// <summary>Import command (opens the file dialog via ImportRequested).</summary>
        public RelayCommand ImportFileCommand { get; }

        /// <summary>Clears the loaded data.</summary>
        public RelayCommand ClearCommand { get; }

        /// <summary>Loaded data file name for display.</summary>
        public string FileName
        {
            get { return _fileName; }
            private set { SetProperty(ref _fileName, value); }
        }

        /// <summary>Status line (row/column counts, errors).</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Whether a load is in progress.</summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            private set { SetProperty(ref _isLoading, value); }
        }

        /// <summary>The loaded data, or null.</summary>
        public ExcelData Data
        {
            get { return _data; }
            private set { SetProperty(ref _data, value); }
        }

        /// <summary>Whether a data file is loaded.</summary>
        public bool HasData => Data != null && Data.RowCount > 0;

        /// <summary>
        /// Loads the given file, updating status and raising DataLoaded on success.
        /// </summary>
        /// <param name="filePath">Path to a .xlsx, .xls, or .csv file.</param>
        /// <returns>Task completing when the load finishes.</returns>
        public async Task LoadFileAsync(string filePath)
        {
            if (!_excelService.ValidateExcelFile(filePath, out var error))
            {
                StatusText = error;
                return;
            }

            IsLoading = true;
            StatusText = "Loading…";
            try
            {
                var data = await _excelService.LoadExcelFileAsync(filePath).ConfigureAwait(true);
                if (data.RowCount == 0)
                {
                    StatusText = "The file contains no data rows.";
                    return;
                }

                Data = data;
                FileName = Path.GetFileName(filePath);
                StatusText = data.RowCount + " rows × " + data.ColumnNames.Count + " columns loaded (" +
                             data.Format.ToUpperInvariant() + ")";
                DataLoaded?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException ||
                                       ex is UnauthorizedAccessException)
            {
                StatusText = "Could not read the file: " + ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Clear()
        {
            Data = null;
            FileName = null;
            StatusText = "No data file loaded.";
        }

        /// <summary>Test hook: sets data directly without going through the service.</summary>
        /// <param name="data">Data to set.</param>
        public void TestSetData(ExcelData data)
        {
            Data = data;
            FileName = data?.SourceFilePath != null ? Path.GetFileName(data.SourceFilePath) : null;
            StatusText = Data == null ? "No data file loaded." : Data.RowCount + " rows loaded.";
        }
    }
}
