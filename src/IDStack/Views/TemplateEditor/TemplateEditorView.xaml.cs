using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDStack.Core.Interfaces;
using IDStack.ViewModels;
using IDStack.ViewModels.TemplateEditor;

namespace IDStack.Views.TemplateEditor
{
    /// <summary>
    /// Hosts the design surface and wires shell services (dialogs, shortcuts) to the editor.
    /// </summary>
    public partial class TemplateEditorView : UserControl
    {
        private TemplateEditorViewModel _viewModel;
        private CanvasControl _canvas;
        private ISettingsService _settingsService;

        /// <summary>
        /// Creates the editor view.
        /// </summary>
        public TemplateEditorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            KeyDown += OnViewKeyDown;
            Focusable = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_canvas == null && _viewModel != null)
            {
                _settingsService = (App.Current as App).Services.GetService(typeof(ISettingsService)) as ISettingsService;
                _canvas = new CanvasControl();
                CanvasHost.Content = _canvas;
                _canvas.Attach(_viewModel, _settingsService);
            }
            Focus();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The editor VM is a singleton re-attached across navigations; keep exactly
            // one subscription per view so stale handlers never fire on a cleared DataContext.
            if (_viewModel != null)
            {
                _viewModel.SaveRequested -= OnSaveRequested;
                _viewModel.OpenRequested -= OnOpenRequested;
                _viewModel.NewRequested -= OnNewRequested;
                _viewModel = null;
            }

            _viewModel = DataContext as TemplateEditorViewModel;
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.SaveRequested += OnSaveRequested;
            _viewModel.OpenRequested += OnOpenRequested;
            _viewModel.NewRequested += OnNewRequested;
        }

        private void OnViewKeyDown(object sender, KeyEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            var ctrl = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control);
            var shift = e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Shift);

            if (ctrl && e.Key == Key.Z) { if (shift) { _viewModel.RedoCommand.Execute(null); } else { _viewModel.UndoCommand.Execute(null); } e.Handled = true; }
            else if (ctrl && e.Key == Key.Y) { _viewModel.RedoCommand.Execute(null); e.Handled = true; }
            else if (ctrl && e.Key == Key.N) { NewDocument(); e.Handled = true; }
            else if (ctrl && e.Key == Key.O) { OpenTemplate(); e.Handled = true; }
            else if (ctrl && e.Key == Key.S) { _viewModel.SaveCommand.Execute(null); e.Handled = true; }
            else if (ctrl && e.Key == Key.J) { _viewModel.DuplicateSelectedCommand.Execute(null); e.Handled = true; }
            else if (ctrl && e.Key == Key.D) { _viewModel.DuplicateSelectedCommand.Execute(null); e.Handled = true; }
            else if (e.Key == Key.V) { /* select tool: default */ }
        }

        private void NewDocument()
        {
            var dialog = new NewDocumentDialog { Owner = Window.GetWindow(this) };
            if (dialog.ShowDialog() == true)
            {
                _viewModel.NewDocument(dialog.WidthMm, dialog.HeightMm);
            }
        }

        private void OpenTemplate()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ID Stack Template (*.idcard)|*.idcard|All files (*.*)|*.*",
                Title = "Open Template"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                _ = LoadTemplateAsync(dialog.FileName);
            }
            catch (Exception)
            {
                ShowError("The template file could not be opened.");
            }
        }

        private async System.Threading.Tasks.Task LoadTemplateAsync(string path)
        {
            try
            {
                await _viewModel.LoadFromFileAsync(path).ConfigureAwait(true);
            }
            catch (Exception)
            {
                ShowError("The template file could not be opened.");
            }
        }

        private async void OnOpenRequested(object sender, EventArgs e)
        {
            // UI-test hook: IDSTACK_AUTO_OPEN skips the native dialog.
            var autoPath = Environment.GetEnvironmentVariable("IDSTACK_AUTO_OPEN");
            if (!string.IsNullOrEmpty(autoPath))
            {
                try
                {
                    if (autoPath.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))
                    {
                        var importer = (App.Current as App).Services.GetService(typeof(IDStack.Services.PsdDesignImporter))
                            as IDStack.Services.PsdDesignImporter;
                        await _viewModel.LoadFromPsdAsync(autoPath, importer).ConfigureAwait(true);
                    }
                    else
                    {
                        await _viewModel.LoadFromFileAsync(autoPath).ConfigureAwait(true);
                    }
                }
                catch (Exception ex)
                {
                    ShowError("The design could not be opened: " + ex.Message);
                }
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Card designs (*.idcard;*.psd)|*.idcard;*.psd|ID Stack design (*.idcard)|*.idcard|Photoshop design (*.psd)|*.psd|All files (*.*)|*.*",
                Title = "Open Template"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                if (dialog.FileName.EndsWith(".psd", StringComparison.OrdinalIgnoreCase))
                {
                    var importer = (App.Current as App).Services.GetService(typeof(IDStack.Services.PsdDesignImporter))
                        as IDStack.Services.PsdDesignImporter;
                    await _viewModel.LoadFromPsdAsync(dialog.FileName, importer).ConfigureAwait(true);
                }
                else
                {
                    await _viewModel.LoadFromFileAsync(dialog.FileName).ConfigureAwait(true);
                }
            }
            catch (Exception ex)
            {
                ShowError("The design could not be opened: " + ex.Message);
            }
        }

        private void OnNewRequested(object sender, EventArgs e)
        {
            NewDocument();
        }

        private async void OnSaveRequested(object sender, SaveRequestedEventArgs e)
        {
            try
            {
                string path;
                if (e.AskForPath)
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        Filter = "ID Stack Template (*.idcard)|*.idcard",
                        FileName = (_viewModel.Template?.Name ?? "template") + ".idcard"
                    };

                    if (dialog.ShowDialog() != true)
                    {
                        return;
                    }
                    path = dialog.FileName;
                }
                else
                {
                    path = _viewModel.FilePath;
                }

                await _viewModel.SaveToFileAsync(path).ConfigureAwait(true);
            }
            catch (Exception)
            {
                ShowError("The template could not be saved.");
            }
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "ID Stack", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
