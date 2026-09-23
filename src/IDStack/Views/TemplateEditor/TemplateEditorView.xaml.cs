using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using IDStack.ViewModels;
using IDStack.ViewModels.TemplateEditor;

namespace IDStack.Views.TemplateEditor
{
    /// <summary>
    /// Hosts the interactive canvas and wires shell services (file dialogs) to the editor.
    /// </summary>
    public partial class TemplateEditorView : UserControl
    {
        private TemplateEditorViewModel _viewModel;
        private CanvasControl _canvas;

        /// <summary>
        /// Creates the editor view.
        /// </summary>
        public TemplateEditorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_canvas == null && _viewModel != null)
            {
                _canvas = new CanvasControl();
                CanvasHost.Content = _canvas;
                _canvas.Attach(_viewModel);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _viewModel = DataContext as TemplateEditorViewModel;
            if (_viewModel == null)
            {
                return;
            }

            _viewModel.SaveRequested += OnSaveRequested;
            _viewModel.OpenRequested += OnOpenRequested;
        }

        private async void OnOpenRequested(object sender, EventArgs e)
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
                await _viewModel.LoadFromFileAsync(dialog.FileName).ConfigureAwait(true);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "The template file could not be opened.",
                    "ID Stack",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
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
                MessageBox.Show(
                    "The template could not be saved.",
                    "ID Stack",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
