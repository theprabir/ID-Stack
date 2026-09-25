using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Import;
using IDStack.Core.Models.Template;
using IDStack.Services;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// One design slot (Front or Back) in the import step: file, template,
    /// detected placeholders, and a live preview thumbnail.
    /// </summary>
    public class DesignSlotViewModel : ViewModelBase
    {
        private readonly IDesignImportService _designService;
        private string _fileName;
        private string _statusText;
        private string _designReport;
        private BitmapSource _preview;
        private ObservableCollection<DesignPlaceholder> _placeholders =
            new ObservableCollection<DesignPlaceholder>();

        /// <summary>
        /// Creates a design slot.
        /// </summary>
        /// <param name="side">Front or Back.</param>
        /// <param name="designService">Design import service.</param>
        public DesignSlotViewModel(SideType side, IDesignImportService designService)
        {
            Side = side;
            _designService = designService ?? throw new ArgumentNullException(nameof(designService));
            StatusText = "No design loaded.";
        }

        /// <summary>Which side this slot holds.</summary>
        public SideType Side { get; }

        /// <summary>Display label ("Front design" / "Back design").</summary>
        public string Label => Side == SideType.Front ? "Front design" : "Back design";

        /// <summary>Loaded file name for display.</summary>
        public string FileName
        {
            get { return _fileName; }
            private set { SetProperty(ref _fileName, value); }
        }

        /// <summary>Status line / import report.</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Layer-by-layer PSD report (null for .idcard).</summary>
        public string DesignReport
        {
            get { return _designReport; }
            private set { SetProperty(ref _designReport, value); }
        }

        /// <summary>Live preview thumbnail of the loaded design.</summary>
        public BitmapSource Preview
        {
            get { return _preview; }
            private set { SetProperty(ref _preview, value); }
        }

        /// <summary>Whether a preview thumbnail is available.</summary>
        public bool HasPreview => Preview != null;

        /// <summary>Auto-detected placeholders for this side's design.</summary>
        public ObservableCollection<DesignPlaceholder> Placeholders => _placeholders;

        /// <summary>The loaded design template, or null.</summary>
        public CardTemplate Design { get; private set; }

        /// <summary>Whether a design is loaded in this slot.</summary>
        public bool HasDesign => Design != null;

        /// <summary>All placeholders detected on this side.</summary>
        public int PlaceholderCount => _placeholders.Count;

        /// <summary>
        /// Loads a .idcard or .psd design into this slot and detects placeholders.
        /// </summary>
        /// <param name="path">Design file path.</param>
        /// <returns>Task completing when loaded.</returns>
        public async Task LoadAsync(string path)
        {
            try
            {
                var extension = Path.GetExtension(path).ToLowerInvariant();
                CardTemplate template;
                if (extension == ".psd")
                {
                    var result = await _designService.ImportPsdAsync(path).ConfigureAwait(true);
                    var psdResult = (PsdImportResult)result;
                    template = psdResult.Template;
                    DesignReport = string.Join(Environment.NewLine, psdResult.LayerReport);
                }
                else if (extension == ".idcard")
                {
                    template = await _designService.LoadIdcardAsync(path).ConfigureAwait(true);
                    DesignReport = null;
                }
                else
                {
                    StatusText = "Unsupported design type \"" + extension + "\". Use .idcard or .psd.";
                    return;
                }

                Design = template;
                FileName = Path.GetFileName(path);

                var detected = _designService.DetectPlaceholders(Design);
                if (Side == SideType.Front)
                {
                    // The front slot template contains the front elements.
                    detected = detected.Where(p => p.Side == SideType.Front).ToList();
                }
                else
                {
                    // For the back slot, detect from its own template's front side
                    // (the back design is authored as a standalone file).
                    detected = detected.Where(p => p.Side == SideType.Front)
                        .Select(p => new DesignPlaceholder(p.Kind, p.ElementId, p.SuggestedName, SideType.Back))
                        .ToList();
                }

                _placeholders.Clear();
                foreach (var placeholder in detected)
                {
                    _placeholders.Add(placeholder);
                }

                LoadPreview();
                StatusText = _placeholders.Count + " placeholders detected.";
                OnPropertyChanged(nameof(HasDesign));
                OnPropertyChanged(nameof(PlaceholderCount));
            }
            catch (Exception ex) when (ex is IOException || ex is InvalidOperationException ||
                                       ex is UnauthorizedAccessException ||
                                       ex is Newtonsoft.Json.JsonSerializationException ||
                                       ex is FileNotFoundException || ex is NotSupportedException)
            {
                Design = null;
                FileName = null;
                Preview = null;
                StatusText = "Could not load the design: " + ex.Message;
                OnPropertyChanged(nameof(HasDesign));
            }
        }

        /// <summary>
        /// Replaces the placeholder list (used when merging front/back slots).
        /// </summary>
        /// <param name="placeholders">New list.</param>
        public void SetPlaceholders(IEnumerable<DesignPlaceholder> placeholders)
        {
            _placeholders.Clear();
            foreach (var placeholder in placeholders)
            {
                _placeholders.Add(placeholder);
            }

            OnPropertyChanged(nameof(PlaceholderCount));
        }

        /// <summary>
        /// Renders the design (background + elements) to a live preview.
        /// </summary>
        private void LoadPreview()
        {
            try
            {
                Preview = DesignPreviewRenderer.Render(Design);
            }
            catch
            {
                Preview = null;
            }
            finally
            {
                OnPropertyChanged(nameof(HasPreview));
            }
        }
    }

    /// <summary>
    /// The data-import flow (v0.3.2):
    /// ① Import the front design, the back design (optional), ONE Excel sheet,
    ///    and the photo folder — with live design previews.
    /// ② Mapping shows front and back placeholder groups separately.
    /// ③ Preview the data, validate, and the rows are ready for processing.
    /// </summary>
    public class DataImportViewModel : ViewModelBase
    {
        private readonly IDesignImportService _designService;
        private readonly IDataValidationService _validationService;

        private int _selectedStep;
        private string _statusText;
        private System.Data.DataView _previewTable;
        private ObservableCollection<ValidationIssue> _issues = new ObservableCollection<ValidationIssue>();
        private ObservableCollection<string> _availableColumns = new ObservableCollection<string>();
        private string _idColumnName;
        private bool _showFrontMapping = true;

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
            FrontDesign = new DesignSlotViewModel(SideType.Front, designService);
            BackDesign = new DesignSlotViewModel(SideType.Back, designService);

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
            GoToMappingCommand = new RelayCommand(_ => { SelectedStep = 1; ShowFrontMapping = true; }, _ => HasAnyDesign);
            BackToImportCommand = new RelayCommand(_ => SelectedStep = 0);
            GoToPreviewCommand = new RelayCommand(_ => { RunValidation(); SelectedStep = 2; }, _ => HasAnyDesign && Excel.HasData);
            ShowFrontMappingCommand = new RelayCommand(_ => ShowFrontMapping = true);
            ShowBackMappingCommand = new RelayCommand(_ => ShowFrontMapping = false);

            StatusText = "Import the front design, back design (optional), the Excel sheet, and the photo folder.";
        }

        /// <summary>Excel/CSV data source (one file shared by both sides).</summary>
        public ExcelImportViewModel Excel { get; }

        /// <summary>Photo folder source.</summary>
        public PhotoImportViewModel Photos { get; }

        /// <summary>Front design slot with preview.</summary>
        public DesignSlotViewModel FrontDesign { get; }

        /// <summary>Back design slot with preview (optional).</summary>
        public DesignSlotViewModel BackDesign { get; }

        /// <summary>Runs data validation.</summary>
        public RelayCommand ValidateCommand { get; }

        /// <summary>Advances to the mapping step.</summary>
        public RelayCommand GoToMappingCommand { get; }

        /// <summary>Returns to the import step.</summary>
        public RelayCommand BackToImportCommand { get; }

        /// <summary>Advances to the preview/validate step.</summary>
        public RelayCommand GoToPreviewCommand { get; }

        /// <summary>Shows the front-side mapping group.</summary>
        public RelayCommand ShowFrontMappingCommand { get; }

        /// <summary>Shows the back-side mapping group.</summary>
        public RelayCommand ShowBackMappingCommand { get; }

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

        /// <summary>Whether the front mapping group is visible (vs back).</summary>
        public bool ShowFrontMapping
        {
            get { return _showFrontMapping; }
            set
            {
                if (SetProperty(ref _showFrontMapping, value))
                {
                    OnPropertyChanged(nameof(ShowBackMapping));
                }
            }
        }

        /// <summary>Whether the back mapping group is visible.</summary>
        public bool ShowBackMapping => !ShowFrontMapping;

        /// <summary>Placeholders of the currently visible mapping group.</summary>
        public ObservableCollection<DesignPlaceholder> ActivePlaceholders =>
            ShowFrontMapping ? FrontDesign.Placeholders : BackDesign.Placeholders;

        /// <summary>Overall status line.</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

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

        /// <summary>Whether at least one design is loaded.</summary>
        public bool HasAnyDesign => FrontDesign.HasDesign || BackDesign.HasDesign;

        /// <summary>Whether all inputs are loaded (back design optional).</summary>
        public bool IsImportComplete => HasAnyDesign && Excel.HasData && Photos.HasPhotos;

        /// <summary>Whether the data is ready for processing (all text placeholders on both sides mapped).</summary>
        public bool ReadyForProcessing => Excel.HasData &&
                                          FrontDesign.Placeholders.Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text).All(p => p.IsBound) &&
                                          BackDesign.Placeholders.Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text).All(p => p.IsBound) &&
                                          (FrontDesign.Placeholders.Any() || BackDesign.Placeholders.Any());

        /// <summary>One-line validation summary.</summary>
        public string ValidationSummary => StatusText;

        /// <summary>Raised when Excel wants a file dialog; the view hosts the dialog.</summary>
        public event EventHandler ExcelFilePicked;

        /// <summary>Raised when Photos wants a folder dialog; the view hosts the dialog.</summary>
        public event EventHandler PhotoFolderPicked;

        /// <summary>Raised when a design slot wants a file dialog (tag = slot).</summary>
        public event EventHandler<DesignSlotViewModel> DesignFilePicked;

        /// <summary>Browse for a design file into the given slot.</summary>
        /// <param name="slot">Front or back slot.</param>
        public void PickDesign(DesignSlotViewModel slot)
        {
            DesignFilePicked?.Invoke(this, slot);
        }

        /// <summary>
        /// Loads a design file into the given slot.
        /// </summary>
        /// <param name="slot">Target slot.</param>
        /// <param name="path">Design file path.</param>
        /// <returns>Task completing when loaded.</returns>
        public async Task LoadDesignAsync(DesignSlotViewModel slot, string path)
        {
            StatusText = "Loading design…";
            await slot.LoadAsync(path).ConfigureAwait(true);

            AutoBindByName();
            UpdateStatus();
            OnPropertyChanged(nameof(HasAnyDesign));
            OnPropertyChanged(nameof(ReadyForProcessing));
            OnPropertyChanged(nameof(ActivePlaceholders));
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

            var required = FrontDesign.Placeholders
                .Concat(BackDesign.Placeholders)
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
            var all = FrontDesign.Placeholders.Concat(BackDesign.Placeholders);
            if (all.Any(p => p != placeholder &&
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
            if (Excel.Data == null)
            {
                return;
            }

            foreach (var placeholder in FrontDesign.Placeholders.Concat(BackDesign.Placeholders)
                         .Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Text))
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
            foreach (var placeholder in FrontDesign.Placeholders.Concat(BackDesign.Placeholders)
                         .Where(p => p.Kind == DesignPlaceholder.PlaceholderKind.Image))
            {
                placeholder.BoundColumn = Photos.HasPhotos ? Photos.MatchColumnName : null;
            }
        }

        private void RebuildPreview()
        {
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
            parts.Add(HasAnyDesign
                ? (FrontDesign.PlaceholderCount + BackDesign.PlaceholderCount) + " placeholders detected"
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
