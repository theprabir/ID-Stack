using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Import;
using IDStack.ViewModels.TemplateEditor;

namespace IDStack.ViewModels.DataImport
{
    /// <summary>
    /// Orchestrates the Phase 3 data-import flow:
    /// Excel file → column mapping → photo import → validation report.
    /// </summary>
    public class DataImportViewModel : ViewModelBase
    {
        private readonly IDataValidationService _validationService;

        private int _selectedStep;
        private string _statusText;
        private ObservableCollection<DataRow> _previewRows = new ObservableCollection<DataRow>();
        private ObservableCollection<ValidationIssue> _issues = new ObservableCollection<ValidationIssue>();
        private string _idColumnName;

        /// <summary>
        /// Creates the data import view model.
        /// </summary>
        /// <param name="excelService">Excel loading service.</param>
        /// <param name="photoService">Photo service.</param>
        /// <param name="validationService">Validation service.</param>
        /// <param name="editor">Template editor state (provides the current template).</param>
        public DataImportViewModel(
            IExcelService excelService,
            IPhotoService photoService,
            IDataValidationService validationService,
            TemplateEditorViewModel editor)
        {
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            Editor = editor ?? throw new ArgumentNullException(nameof(editor));

            Excel = new ExcelImportViewModel(excelService);
            Mapping = new ColumnMappingViewModel();
            Photos = new PhotoImportViewModel(photoService);

            Excel.ImportRequested += OnImportRequested;
            Excel.DataLoaded += OnDataLoaded;
            Photos.FolderBrowseRequested += OnFolderBrowseRequested;
            Photos.RematchRequested += OnRematchRequested;

            ImportExcelFileCommand = new RelayCommand(path => _ = Excel.LoadFileAsync(path as string));
            BrowsePhotosCommand = new RelayCommand(_ => _ = BrowsePhotosAsync());
            ValidateCommand = new RelayCommand(_ => RunValidation(), _ => Excel.HasData);

            StatusText = "Import an Excel or CSV file to begin.";
        }

        /// <summary>Editor view model providing the active template.</summary>
        public TemplateEditorViewModel Editor { get; }

        /// <summary>Step 1: Excel import.</summary>
        public ExcelImportViewModel Excel { get; }

        /// <summary>Step 2: Column mapping.</summary>
        public ColumnMappingViewModel Mapping { get; }

        /// <summary>Step 3: Photo import.</summary>
        public PhotoImportViewModel Photos { get; }

        /// <summary>Loads the picked data file.</summary>
        public RelayCommand ImportExcelFileCommand { get; }

        /// <summary>Browses for the photo folder.</summary>
        public RelayCommand BrowsePhotosCommand { get; }

        /// <summary>Runs data validation.</summary>
        public RelayCommand ValidateCommand { get; }

        /// <summary>Selected wizard step (0 Excel, 1 Mapping, 2 Photos, 3 Validation).</summary>
        public int SelectedStep
        {
            get { return _selectedStep; }
            set
            {
                if (SetProperty(ref _selectedStep, value))
                {
                    OnPropertyChanged(nameof(ShowExcelStep));
                    OnPropertyChanged(nameof(ShowMappingStep));
                    OnPropertyChanged(nameof(ShowPhotosStep));
                    OnPropertyChanged(nameof(ShowValidationStep));
                }
            }
        }

        /// <summary>Whether the Excel step is visible.</summary>
        public bool ShowExcelStep => SelectedStep == 0;

        /// <summary>Whether the mapping step is visible.</summary>
        public bool ShowMappingStep => SelectedStep == 1;

        /// <summary>Whether the photos step is visible.</summary>
        public bool ShowPhotosStep => SelectedStep == 2;

        /// <summary>Whether the validation step is visible.</summary>
        public bool ShowValidationStep => SelectedStep == 3;

        /// <summary>Overall status line.</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>First rows of loaded data for the preview grid.</summary>
        public ObservableCollection<DataRow> PreviewRows
        {
            get { return _previewRows; }
            private set { SetProperty(ref _previewRows, value); }
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

        /// <summary>Whether the flow can proceed to batch generation (Phase 4).</summary>
        public bool CanGenerate => Excel.HasData && Mapping.AllRequiredMapped;

        /// <summary>
        /// Runs validation over the loaded data and switches to the validation step.
        /// </summary>
        public void RunValidation()
        {
            var required = Mapping.GetRequiredColumns();
            var issues = _validationService.Validate(Excel.Data, required, IdColumnName, Photos.HasPhotos ? Photos.Photos.ToList() : null);
            Issues = new ObservableCollection<ValidationIssue>(issues);

            var errors = issues.Count(i => i.Severity == ValidationIssue.SeverityLevel.Error);
            var warnings = issues.Count(i => i.Severity == ValidationIssue.SeverityLevel.Warning);
            StatusText = errors == 0 && warnings == 0
                ? "All " + Excel.Data.RowCount + " rows are valid."
                : errors + " errors, " + warnings + " warnings found.";

            SelectedStep = 3;
            OnPropertyChanged(nameof(CanGenerate));
        }

        /// <summary>One-line validation summary for the report step.</summary>
        public string ValidationSummary => StatusText;

        /// <summary>Raised when Excel wants a file dialog; the view hosts the dialog.</summary>
        public event EventHandler ExcelFilePicked;

        /// <summary>Raised when Photos wants a folder dialog; the view hosts the dialog.</summary>
        public event EventHandler PhotoFolderPicked;

        private void OnImportRequested(object sender, EventArgs e)
        {
            ExcelFilePicked?.Invoke(this, null);
        }

        private void OnFolderBrowseRequested(object sender, EventArgs e)
        {
            PhotoFolderPicked?.Invoke(this, null);
        }

        private async void OnRematchRequested(object sender, EventArgs e)
        {
            if (Excel.Data != null)
            {
                await Photos.MatchToDataAsync(Excel.Data, Photos.MatchColumnName).ConfigureAwait(true);
            }
        }

        private async void OnDataLoaded(object sender, EventArgs e)
        {
            Mapping.BuildFrom(Editor.Template, Excel.Data);
            RefreshPreview();
            Photos.MatchColumnName = Mapping.AvailableColumns.FirstOrDefault();
            IdColumnName = Mapping.AvailableColumns.FirstOrDefault();
            StatusText = Excel.StatusText;
            SelectedStep = 0;
            OnPropertyChanged(nameof(CanGenerate));
        }

        private void RefreshPreview()
        {
            PreviewRows = new ObservableCollection<DataRow>(Excel.Data.Rows.Take(20));
        }

        private async System.Threading.Tasks.Task BrowsePhotosAsync()
        {
            PhotoFolderPicked?.Invoke(this, null);
            if (Excel.Data != null)
            {
                await Photos.MatchToDataAsync(Excel.Data, Photos.MatchColumnName).ConfigureAwait(true);
            }

            OnPropertyChanged(nameof(CanGenerate));
        }
    }
}
