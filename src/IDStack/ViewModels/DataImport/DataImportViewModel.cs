using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;
using IDStack.Services;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// The data-import flow, exactly as the user works:
    /// ① Import the .idcard/psd design, the Excel sheet, and the photo folder.
    /// ② App auto-detects every text and image placeholder in the design; the
    ///    user maps each Excel column to a text placeholder and each photo to
    ///    an image placeholder (photos are matched by file name automatically).
    /// ③ Preview the data, validate, and the rows are ready for processing.
    /// </summary>
    public class DataImportViewModel : ViewModelBase
    {
        private readonly IDesignImportService _designService;
        private readonly IDataValidationService _validationService;

        private int _selectedStep;
        private string _statusText;
        private string _designName;
        private ObservableCollection<DesignPlaceholder> _placeholders = new ObservableCollection<DesignPlaceholder>();
        private ObservableCollection<DataRow> _previewRows = new ObservableCollection<DataRow>();
        private System.Data.DataView _previewTable;
        private ObservableCollection<ValidationIssue> _issues = new ObservableCollection<ValidationIssue>();
        private ObservableCollection<string> _availableColumns = new ObservableCollection<string>();
        private string _idColumnName;
        private string _designReport;

        /// <summary>
        /// Creates the data import view model.
        /// </summary>
        /// <param name="designService">Design import + placeholder detection.</param>
        /// <param name="excelService">Excel loading.</param>
        /// <param name="photoService">Photo loading + matching.</param>
        /// <param name="validationService">Data validation.</param>
        public DataImportViewModel(
            IDesignImportService designService,
            IExcelService excelService,
            IPhotoService photoService,
            IDataValidationService validationService)
        {
            _designService = designService ?? throw new ArgumentNullException(nameof(designService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));

            Excel = new ExcelImportViewModel(excelService);
            Photos = new PhotoImportViewModel(photoService);

            Excel.ImportRequested += (s, e) => ExcelFilePicked?.Invoke(this, EventArgs.Empty);
            Excel.DataLoaded += (s, e) => OnExcelLoaded();
            Photos.FolderBrowseRequested += (s, e) => PhotoFolderPicked?.Invoke(this, EventArgs.Empty);
            Photos.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PhotoImportViewModel.HasPhotos) ||
                    e.PropertyName == nameof(PhotoImportViewModel.MatchedCount))
                {
                    UpdateStatus();
                }
            };

            ValidateCommand = new RelayCommand(_ => RunValidation(), _ => Excel.HasData);
            GoToMappingCommand = new RelayCommand(_ => SelectedStep = 1, _ => HasDesign);
            BackToImportCommand = new RelayCommand(_ => SelectedStep = 0);
            GoToPreviewCommand = new RelayCommand(_ => { RunValidation(); SelectedStep = 2; }, _ => HasDesign && Excel.HasData);

            StatusText = "Import the design (.idcard or .psd), the Excel sheet, and the photo folder to begin.";
        }

        /// <summary>Excel/CSV data source.</summary>
        public ExcelImportViewModel Excel { get; }

        /// <summary>Photo folder source.</summary>
        public PhotoImportViewModel Photos { get; }

        /// <summary>Runs data validation.</summary>
        public RelayCommand ValidateCommand { get; }

        /// <summary>Advances to the mapping step.</summary>
        public RelayCommand GoToMappingCommand { get; }

        /// <summary>Returns to the import step.</summary>
        public RelayCommand BackToImportCommand { get; }

        /// <summary>Advances to the preview/validate step.</summary>
        public RelayCommand GoToPreviewCommand { get; }

        /// <summary>Selected step: 0 Import, 1 Mapping, 2 Preview & Validate.</summary>
        public int SelectedStep
        {
            get { return _selectedStep; }
            set
            {
                if (SetProperty(ref _selectedStep, value))
                {
                    OnPropertyChanged(nameof(ShowImportStep));
                    OnPropertyChanged(nameof(ShowMappingStep));
                    OnPropertyChanged(nameof(ShowPreviewStep));
                }
            }
        }

        /// <summary>Whether the import step is visible.</summary>
        public bool ShowImportStep => SelectedStep == 0;

        /// <summary>Whether the mapping step is visible.</summary>
        public bool ShowMappingStep => SelectedStep == 1;

        /// <summary>Whether the preview/validate step is visible.</summary>
        public bool ShowPreviewStep => SelectedStep == 2;

        /// <summary>Overall status line.</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Loaded design display name.</summary>
        public string DesignName
        {
            get { return _designName; }
            private set { SetProperty(ref _designName, value); }
        }

        /// <summary>Layer-by-layer report from a PSD import (empty for .idcard).</summary>
        public string DesignReport
        {
            get { return _designReport; }
            private set { SetProperty(ref _designReport, value); }
        }

        /// <summary>Auto-detected placeholders from the design.</summary>
        public ObservableCollection<DesignPlaceholder> Placeholders => _placeholders;

        /// <summary>Excel column names available for mapping.</summary>
        public ObservableCollection<string> AvailableColumns => _availableColumns;

        /// <summary>First rows of loaded data for the preview grid (one column per Excel header).</summary>
        public System.Data.DataView PreviewTable
        {
            get { return _previewTable; }
            private set { SetProperty(ref _previewTable, value); }
        }

        /// <summary>Validation issues from the latest run.</summary>
        public ObservableCollection<ValidationIssue> Issues
        {
            get { return _issues; }
            private set { SetProperty(ref _issues, value); }
        }

        /// <summary>Column treated as the unique card ID for duplicate checking.</summary>
        public string IdColumnName
        {
            get { return _idColumnName; }
            set { SetProperty(ref _idColumnName, value); }
        }

        /// <summary>The loaded design template (null until imported).</summary>
        public CardTemplate Design { get; private set; }

        /// <summary>Whether a design is loaded.</summary>
        public bool HasDesign => Design != null;

        /// <summary>Whether all three inputs are loaded.</summary>
        public bool IsImportComplete => HasDesign && Excel.HasData && Photos.HasPhotos;

        /// <summary>Whether the data is ready for processing (all text placeholders mapped).</summary>
        public bool ReadyForProcessing => Excel.HasData && Placeholders.Count > 0 &&
                                          Placeholders.Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text)
                                              .All(p => p.IsBound);

        /// <summary>One-line validation summary.</summary>
        public string ValidationSummary => StatusText;

        /// <summary>Raised when Excel wants a file dialog; the view hosts the dialog.</summary>
        public event EventHandler ExcelFilePicked;

        /// <summary>Raised when Photos wants a folder dialog; the view hosts the dialog.</summary>
        public event EventHandler PhotoFolderPicked;

        /// <summary>Raised when the design wants a file dialog; the view hosts the dialog.</summary>
        public event EventHandler DesignFilePicked;

        /// <summary>Browse for the design file (view shows the dialog).</summary>
        public void PickDesign()
        {
            DesignFilePicked?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Loads a .idcard or .psd design and auto-detects its placeholders.
        /// </summary>
        /// <param name="path">Design file path.</param>
        /// <returns>Task completing when the design is loaded.</returns>
        public async Task LoadDesignAsync(string path)
        {
            StatusText = "Loading design…";
            try
            {
                var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".psd")
                {
                    var result = await _designService.ImportPsdAsync(path).ConfigureAwait(true);
                    var psdResult = (PsdImportResult)result;
                    Design = psdResult.Template;
                    DesignReport = string.Join(Environment.NewLine, psdResult.LayerReport);
                }
                else if (extension == ".idcard")
                {
                    Design = await _designService.LoadIdcardAsync(path).ConfigureAwait(true);
                    DesignReport = null;
                }
                else
                {
                    StatusText = "Unsupported design type \"" + extension + "\". Use .idcard or .psd.";
                    return;
                }

                DesignName = System.IO.Path.GetFileName(path);

                var detected = _designService.DetectPlaceholders(Design);
                Placeholders.Clear();
                foreach (var placeholder in detected)
                {
                    Placeholders.Add(placeholder);
                }

                AutoBindByName();
                RebuildPreview();
                UpdateStatus();
                OnPropertyChanged(nameof(HasDesign));
                OnPropertyChanged(nameof(ReadyForProcessing));
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException ||
                                       ex is UnauthorizedAccessException || ex is Newtonsoft.Json.JsonSerializationException ||
                                       ex is FileNotFoundException || ex is NotSupportedException)
            {
                Design = null;
                DesignName = null;
                StatusText = "Could not load the design: " + ex.Message;
                OnPropertyChanged(nameof(HasDesign));
            }
        }

        /// <summary>
        /// Test/automation hook: loads the Excel file directly.
        /// </summary>
        /// <param name="path">Data file path.</param>
        /// <returns>Task completing when loaded.</returns>
        public Task ImportExcelFileAsync(string path)
        {
            return Excel.LoadFileAsync(path);
        }

        /// <summary>
        /// Test/automation hook: loads the photo folder directly and matches to rows.
        /// </summary>
        /// <param name="path">Photo folder path.</param>
        /// <returns>Task completing when scanned and matched.</returns>
        public async Task ImportPhotoFolderAsync(string path)
        {
            await Photos.LoadFolderAsync(path).ConfigureAwait(true);
            if (Excel.Data != null)
            {
                await Photos.MatchToDataAsync(Excel.Data, Photos.MatchColumnName).ConfigureAwait(true);
                RebindImagePlaceholders();
                UpdateStatus();
            }
        }

        /// <summary>Runs validation. Stays on the current step.</summary>
        public void RunValidation()
        {
            if (Excel.Data == null)
            {
                return;
            }

            var required = Placeholders
                .Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text && p.IsBound)
                .Select(p => p.BoundColumn)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var issues = _validationService.Validate(
                Excel.Data, required, IdColumnName,
                Photos.HasPhotos ? Photos.Photos.ToList() : null,
                Photos.MatchColumnName);
            Issues = new ObservableCollection<ValidationIssue>(issues);

            var errors = issues.Count(i => i.Severity == ValidationIssue.SeverityLevel.Error);
            var warnings = issues.Count(i => i.Severity == ValidationIssue.SeverityLevel.Warning);
            StatusText = errors == 0 && warnings == 0
                ? "All " + Excel.Data.RowCount + " rows are valid. Ready for processing."
                : errors + " errors, " + warnings + " warnings found.";
            OnPropertyChanged(nameof(ReadyForProcessing));
        }

        /// <summary>Binds a placeholder to a column (two-way from the mapping grid).</summary>
        /// <param name="placeholder">The placeholder.</param>
        /// <param name="column">The column, or null to unbind.</param>
        public void BindPlaceholder(DesignPlaceholder placeholder, string column)
        {
            if (placeholder == null)
            {
                return;
            }

            placeholder.BoundColumn = string.IsNullOrWhiteSpace(column) ? null : column;
            UpdateStatus();
            OnPropertyChanged(nameof(ReadyForProcessing));
        }

        /// <summary>Renames a placeholder so mapping stays unambiguous.</summary>
        /// <param name="placeholder">The placeholder.</param>
        /// <param name="name">New unique name.</param>
        public void RenamePlaceholder(DesignPlaceholder placeholder, string name)
        {
            if (placeholder == null || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var trimmed = name.Trim();
            if (Placeholders.Any(p => p != placeholder &&
                string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                StatusText = "The name \"" + trimmed + "\" is already used by another placeholder.";
                return;
            }

            placeholder.Name = trimmed;
        }

        private void OnExcelLoaded()
        {
            _availableColumns = new ObservableCollection<string>(Excel.Data.ColumnNames);
            OnPropertyChanged(nameof(AvailableColumns));

            if (IdColumnName == null)
            {
                IdColumnName = Excel.Data.ColumnNames.FirstOrDefault();
            }

            Photos.MatchColumnName = PickPhotoColumn(Excel.Data.ColumnNames);
            AutoBindByName();
            if (Photos.HasPhotos)
            {
                _ = MatchPhotosAsync();
            }

            RebuildPreview();
            UpdateStatus();
            OnPropertyChanged(nameof(ReadyForProcessing));
        }

        private async Task MatchPhotosAsync()
        {
            await Photos.MatchToDataAsync(Excel.Data, Photos.MatchColumnName).ConfigureAwait(true);
            RebindImagePlaceholders();
            UpdateStatus();
        }

        /// <summary>
        /// Picks the best photo-match column: prefers a column literally named
        /// like photos (PhotoFile, Photo, Image…), else any column whose values
        /// look like image file names, else the first column.
        /// </summary>
        private static string PickPhotoColumn(IList<string> columns)
        {
            var preferredNames = new[] { "PhotoFile", "Photo", "Image", "Picture", "Img", "FileName" };
            foreach (var preferred in preferredNames)
            {
                var exact = columns.FirstOrDefault(c =>
                    string.Equals(c, preferred, StringComparison.OrdinalIgnoreCase));
                if (exact != null)
                {
                    return exact;
                }
            }

            foreach (var column in columns)
            {
                if (column.IndexOf("photo", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    column.IndexOf("image", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return column;
                }
            }

            return columns.FirstOrDefault();
        }

        /// <summary>
        /// Auto-binds text placeholders to columns whose names match the
        /// placeholder name (case-insensitive) — "Full Name" binds to "Name" etc.
        /// </summary>
        private void AutoBindByName()
        {
            if (Excel.Data == null || Placeholders.Count == 0)
            {
                return;
            }

            foreach (var placeholder in Placeholders.Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text))
            {
                var match = Excel.Data.ColumnNames.FirstOrDefault(c =>
                    string.Equals(c, placeholder.Name, StringComparison.OrdinalIgnoreCase)) ??
                    Excel.Data.ColumnNames.FirstOrDefault(c =>
                    c.IndexOf(placeholder.Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    placeholder.Name.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0);
                placeholder.BoundColumn = match;
            }
        }

        private void RebindImagePlaceholders()
        {
            // Image placeholders consume the photo mapping from PhotoService;
            // their BoundColumn holds the match-column name for the report.
            foreach (var placeholder in Placeholders.Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Image))
            {
                placeholder.BoundColumn = Photos.HasPhotos ? Photos.MatchColumnName : null;
            }
        }

        private void RebuildPreview()
        {
            // Build a DataTable so the DataGrid generates one column per Excel header
            // (binding directly to DataRow would only show RowNumber + Values).
            var table = new System.Data.DataTable();
            if (Excel.Data != null)
            {
                foreach (var column in Excel.Data.ColumnNames)
                {
                    table.Columns.Add(column, typeof(string));
                }

                foreach (var row in Excel.Data.Rows.Take(20))
                {
                    table.Rows.Add(Excel.Data.ColumnNames.Select(c => (object)row.Get(c)).ToArray());
                }
            }

            PreviewTable = table.DefaultView;
        }

        private void UpdateStatus()
        {
            var parts = new List<string>();
            parts.Add(HasDesign
                ? Placeholders.Count + " placeholders detected"
                : "no design");
            parts.Add(Excel.HasData
                ? Excel.Data.RowCount + " rows"
                : "no data file");
            parts.Add(Photos.HasPhotos
                ? Photos.MatchedCount + "/" + Photos.Photos.Count + " photos matched"
                : "no photos");

            StatusText = "Imported: " + string.Join(", ", parts) + ".";
        }
    }
}
