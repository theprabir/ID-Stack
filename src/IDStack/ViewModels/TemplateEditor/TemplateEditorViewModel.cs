using System;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Core.Interfaces;
using IDStack.Core.Models.Elements;
using IDStack.Core.Models.Template;
using IDStack.Commands;
using IDStack.Services;

namespace IDStack.ViewModels.TemplateEditor
{
    /// <summary>
    /// View model for the template editor: owns the current template, side switching,
    /// element add/delete/selection, and undo/redo.
    /// </summary>
    public class TemplateEditorViewModel : ViewModelBase
    {
        private readonly ITemplateService _templateService;
        private readonly IHistoryService<EditorState> _history;
        private readonly ILocalizationService _localization;

        private CardTemplate _template;
        private SideType _selectedSide;
        private CanvasElement _selectedElement;
        private double _zoomLevel = 4.0;
        private bool _isModified;
        private string _filePath;
        private bool _suppressHistory;
        private string _statusText = string.Empty;

        /// <summary>Raised when the editor requests a file-save dialog. Parameter: SaveAs?</summary>
        public event EventHandler<SaveRequestedEventArgs> SaveRequested;

        /// <summary>Raised when the editor requests a file-open dialog.</summary>
        public event EventHandler OpenRequested;

        /// <summary>
        /// Creates the editor view model.
        /// </summary>
        public TemplateEditorViewModel(
            ITemplateService templateService,
            IHistoryService<EditorState> history,
            ILocalizationService localization)
        {
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _history = history ?? throw new ArgumentNullException(nameof(history));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));

            _template = _templateService.CreateNew("Untitled Template");
            _selectedSide = SideType.Front;
            _history.Reset(Snapshot());

            NewCommand = new RelayCommand(_ => NewTemplate());
            OpenCommand = new RelayCommand(_ => OpenRequested?.Invoke(this, EventArgs.Empty));
            SaveCommand = new RelayCommand(_ => RequestSave(false));
            SaveAsCommand = new RelayCommand(_ => RequestSave(true));
            UndoCommand = new RelayCommand(_ => Undo(), _ => _history.CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => _history.CanRedo);

            SwitchToFrontCommand = new RelayCommand(_ => SelectedSideType = SideType.Front);
            SwitchToBackCommand = new RelayCommand(_ => SelectedSideType = SideType.Back);

            ZoomInCommand = new RelayCommand(_ => ZoomLevel += 0.6);
            ZoomOutCommand = new RelayCommand(_ => ZoomLevel -= 0.6);
            FitToScreenCommand = new RelayCommand(_ => ZoomLevel = 4.0);

            AddTextCommand = new RelayCommand(_ => AddElement(new TextElement()));
            AddImageCommand = new RelayCommand(_ => AddElement(new ImageElement()));
            AddRectangleCommand = new RelayCommand(_ => AddElement(new ShapeElement { ShapeKind = ShapeKind.Rectangle }));
            AddEllipseCommand = new RelayCommand(_ => AddElement(new ShapeElement { ShapeKind = ShapeKind.Ellipse }));
            AddLineCommand = new RelayCommand(_ => AddElement(new ShapeElement { ShapeKind = ShapeKind.Line, Height = 0.5 }));
            AddBarcodeCommand = new RelayCommand(_ => AddElement(new BarcodeElement()));
            AddPlaceholderCommand = new RelayCommand(_ => AddElement(new PlaceholderElement()));

            DeleteSelectedCommand = new RelayCommand(
                _ => DeleteSelected(),
                _ => SelectedElement != null && !SelectedElement.IsLocked);
            DuplicateSelectedCommand = new RelayCommand(
                _ => DuplicateSelected(),
                _ => SelectedElement != null);
        }

        /// <summary>The template being edited.</summary>
        public CardTemplate Template
        {
            get { return _template; }
            private set { SetProperty(ref _template, value); }
        }

        /// <summary>Currently edited side.</summary>
        public SideType SelectedSideType
        {
            get { return _selectedSide; }
            set
            {
                if (SetProperty(ref _selectedSide, value))
                {
                    SelectedElement = null;
                    _history.Reset(Snapshot());
                    OnPropertyChanged(nameof(CurrentSide));
                }
            }
        }

        /// <summary>Elements of the currently edited side.</summary>
        public System.Collections.ObjectModel.ObservableCollection<CanvasElement> Elements =>
            CurrentSide.Elements;

        /// <summary>The currently edited side model.</summary>
        public TemplateSide CurrentSide => Template.GetSide(_selectedSide);

        /// <summary>Selected element on the canvas (for the properties panel).</summary>
        public CanvasElement SelectedElement
        {
            get { return _selectedElement; }
            set
            {
                if (SetProperty(ref _selectedElement, value))
                {
                    OnPropertyChanged(nameof(HasSelection));
                }
            }
        }

        /// <summary>Whether an element is selected.</summary>
        public bool HasSelection => SelectedElement != null;

        /// <summary>Zoom factor (screen pixels per millimeter at 100% zoom = 3.78).</summary>
        public double ZoomLevel
        {
            get { return _zoomLevel; }
            set
            {
                var clamped = Math.Max(0.4, Math.Min(16.0, value));
                if (SetProperty(ref _zoomLevel, clamped))
                {
                    OnPropertyChanged(nameof(ZoomPercent));
                    OnPropertyChanged(nameof(ZoomPercentText));
                }
            }
        }

        /// <summary>Zoom level as a percentage for display.</summary>
        public double ZoomPercent => Math.Round(ZoomLevel / 3.7795 * 100.0);

        /// <summary>Zoom percentage as preformatted text (avoids XAML brace escaping).</summary>
        public string ZoomPercentText => ZoomPercent + "%";

        /// <summary>Whether the template has unsaved changes.</summary>
        public bool IsModified
        {
            get { return _isModified; }
            private set { SetProperty(ref _isModified, value); }
        }

        /// <summary>Current file path (null until saved).</summary>
        public string FilePath
        {
            get { return _filePath; }
            private set { SetProperty(ref _filePath, value); }
        }

        /// <summary>Editor status line.</summary>
        public string StatusText
        {
            get { return _statusText; }
            private set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Undo step count.</summary>
        public int UndoCount => _history.UndoCount;

        /// <summary>Redo step count.</summary>
        public int RedoCount => _history.RedoCount;

        // Commands
        /// <summary>Starts a new empty template.</summary>
        public RelayCommand NewCommand { get; }
        /// <summary>Requests the open-template flow.</summary>
        public RelayCommand OpenCommand { get; }
        /// <summary>Saves to the current path (asks for a path when none).</summary>
        public RelayCommand SaveCommand { get; }
        /// <summary>Always asks for a path before saving.</summary>
        public RelayCommand SaveAsCommand { get; }
        /// <summary>Undoes the last edit.</summary>
        public RelayCommand UndoCommand { get; }
        /// <summary>Redoes the last undone edit.</summary>
        public RelayCommand RedoCommand { get; }
        /// <summary>Switches to the front side.</summary>
        public RelayCommand SwitchToFrontCommand { get; }
        /// <summary>Switches to the back side.</summary>
        public RelayCommand SwitchToBackCommand { get; }
        /// <summary>Zooms the canvas in.</summary>
        public RelayCommand ZoomInCommand { get; }

        /// <summary>Zooms the canvas out.</summary>
        public RelayCommand ZoomOutCommand { get; }

        /// <summary>Resets zoom to fit a CR80 card.</summary>
        public RelayCommand FitToScreenCommand { get; }

        /// <summary>Adds a text element.</summary>
        public RelayCommand AddTextCommand { get; }
        /// <summary>Adds an image element.</summary>
        public RelayCommand AddImageCommand { get; }
        /// <summary>Adds a rectangle.</summary>
        public RelayCommand AddRectangleCommand { get; }
        /// <summary>Adds an ellipse.</summary>
        public RelayCommand AddEllipseCommand { get; }
        /// <summary>Adds a line.</summary>
        public RelayCommand AddLineCommand { get; }
        /// <summary>Adds a barcode.</summary>
        public RelayCommand AddBarcodeCommand { get; }
        /// <summary>Adds a data placeholder.</summary>
        public RelayCommand AddPlaceholderCommand { get; }
        /// <summary>Deletes the selected element.</summary>
        public RelayCommand DeleteSelectedCommand { get; }
        /// <summary>Duplicates the selected element with a small offset.</summary>
        public RelayCommand DuplicateSelectedCommand { get; }

        /// <summary>Adds an element to the current side and selects it.</summary>
        /// <param name="element">Element to add.</param>
        public void AddElement(CanvasElement element)
        {
            if (element == null)
            {
                return;
            }

            CenterNewElement(element);
            CurrentSide.Elements.Add(element);
            SelectedElement = element;
            CommitChange();
            StatusText = "Added " + element.Name;
        }

        /// <summary>Removes the selected element (when not locked).</summary>
        public void DeleteSelected()
        {
            if (SelectedElement == null || SelectedElement.IsLocked)
            {
                return;
            }

            CurrentSide.Elements.Remove(SelectedElement);
            SelectedElement = null;
            CommitChange();
            StatusText = "Deleted element";
        }

        /// <summary>Clones the selected element, offset by 3 mm.</summary>
        public void DuplicateSelected()
        {
            if (SelectedElement == null)
            {
                return;
            }

            var copy = SelectedElement.Clone();
            copy.X += 3;
            copy.Y += 3;
            CurrentSide.Elements.Add(copy);
            SelectedElement = copy;
            CommitChange();
            StatusText = "Duplicated " + copy.Name;
        }

        /// <summary>Records a property edit on the current side into history.</summary>
        public void CommitChange()
        {
            if (_suppressHistory)
            {
                return;
            }

            _history.Push(Snapshot());
            IsModified = true;
            OnPropertyChanged(nameof(UndoCount));
            OnPropertyChanged(nameof(RedoCount));
        }

        /// <summary>Loads a template file into the editor.</summary>
        /// <param name="filePath">Path to a .idcard file.</param>
        public async Task LoadFromFileAsync(string filePath)
        {
            var template = await _templateService.LoadAsync(filePath).ConfigureAwait(true);
            Template = template;
            FilePath = filePath;
            SelectedSideType = SideType.Front;
            IsModified = false;
            StatusText = "Loaded " + template.Name;
        }

        /// <summary>Saves the template to the given path.</summary>
        /// <param name="path">Destination .idcard path.</param>
        public async Task SaveToFileAsync(string path)
        {
            await _templateService.SaveAsync(Template, path).ConfigureAwait(true);
            FilePath = path;
            IsModified = false;
            StatusText = "Saved";
        }

        private EditorState Snapshot()
        {
            return new EditorState(CurrentSide);
        }

        private void Undo()
        {
            if (!_history.CanUndo)
            {
                return;
            }

            ApplySnapshot(() => _history.Undo());
            StatusText = "Undo";
        }

        private void Redo()
        {
            if (!_history.CanRedo)
            {
                return;
            }

            ApplySnapshot(() => _history.Redo());
            StatusText = "Redo";
        }

        private void ApplySnapshot(Func<EditorState> getSnapshot)
        {
            var restored = getSnapshot();
            _suppressHistory = true;
            try
            {
                restored.ApplyTo(CurrentSide);
                if (SelectedElement != null &&
                    CurrentSide.Elements.All(e => e.Id != SelectedElement.Id))
                {
                    SelectedElement = null;
                }
            }
            finally
            {
                _suppressHistory = false;
            }
            OnPropertyChanged(nameof(Elements));
            OnPropertyChanged(nameof(UndoCount));
            OnPropertyChanged(nameof(RedoCount));
        }

        private void CenterNewElement(CanvasElement element)
        {
            element.X = Math.Max(0, (CurrentSide.CanvasWidth - element.Width) / 2.0);
            element.Y = Math.Max(0, (CurrentSide.CanvasHeight - element.Height) / 2.0);
        }

        private void NewTemplate()
        {
            Template = _templateService.CreateNew("Untitled Template");
            FilePath = null;
            SelectedElement = null;
            SelectedSideType = SideType.Front;
            IsModified = false;
            _history.Reset(Snapshot());
            StatusText = "New template";
        }

        private void RequestSave(bool saveAs)
        {
            var handler = SaveRequested;
            if (handler == null)
            {
                return;
            }
            handler(this, new SaveRequestedEventArgs(saveAs || string.IsNullOrEmpty(FilePath)));
        }
    }

    /// <summary>
    /// Arguments for the save-requested event raised by the editor.
    /// </summary>
    public class SaveRequestedEventArgs : EventArgs
    {
        /// <summary>Creates the event args.</summary>
        /// <param name="askForPath">Whether the shell must show a save dialog.</param>
        public SaveRequestedEventArgs(bool askForPath)
        {
            AskForPath = askForPath;
        }

        /// <summary>Whether the shell must show a save dialog.</summary>
        public bool AskForPath { get; }
    }
}
