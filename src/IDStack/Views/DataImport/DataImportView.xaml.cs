using System;
using System.Windows;
using System.Windows.Controls;
using IDStack.ViewModels.DataImport;
using Microsoft.Win32;

namespace IDStack.Views.DataImport
{
    /// <summary>
    /// Data-import view: hosts the design/data/folder dialogs and forwards picks
    /// to the view model. The view model holds all state and logic (MVVM).
    /// </summary>
    public partial class DataImportView : UserControl
    {
        private DataImportViewModel _subscribedViewModel;

        /// <summary>
        /// Creates the view.
        /// </summary>
        public DataImportView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnViewUnloaded;
        }

        private DataImportViewModel ViewModel => DataContext as DataImportViewModel;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // The VM is a singleton re-attached across navigations; keep exactly one
            // subscription per view so stale handlers never fire on a cleared DataContext.
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.ExcelFilePicked -= OnExcelFilePicked;
                _subscribedViewModel.PhotoFolderPicked -= OnPhotoFolderPicked;
                _subscribedViewModel.DesignFilePicked -= OnDesignFilePicked;
                _subscribedViewModel = null;
            }

            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            viewModel.ExcelFilePicked += OnExcelFilePicked;
            viewModel.PhotoFolderPicked += OnPhotoFolderPicked;
            viewModel.DesignFilePicked += OnDesignFilePicked;
            _subscribedViewModel = viewModel;
        }

        private void OnViewUnloaded(object sender, RoutedEventArgs e)
        {
            if (_subscribedViewModel != null)
            {
                _subscribedViewModel.ExcelFilePicked -= OnExcelFilePicked;
                _subscribedViewModel.PhotoFolderPicked -= OnPhotoFolderPicked;
                _subscribedViewModel.DesignFilePicked -= OnDesignFilePicked;
                _subscribedViewModel = null;
            }
        }

        private void OnDesignFilePicked(object sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            // UI-test hook: IDSTACK_AUTO_DESIGN skips the native dialog.
            var autoPath = Environment.GetEnvironmentVariable("IDSTACK_AUTO_DESIGN");
            if (!string.IsNullOrEmpty(autoPath))
            {
                _ = viewModel.LoadDesignAsync(autoPath);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Import card design",
                Filter = "Card designs (*.idcard;*.psd)|*.idcard;*.psd|ID Stack design (*.idcard)|*.idcard|Photoshop design (*.psd)|*.psd"
            };

            if (dialog.ShowDialog(GetOwner()) == true)
            {
                _ = viewModel.LoadDesignAsync(dialog.FileName);
            }
        }

        private void OnExcelFilePicked(object sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            // UI-test hook: IDSTACK_AUTO_IMPORT skips the native dialog.
            var autoPath = Environment.GetEnvironmentVariable("IDSTACK_AUTO_IMPORT");
            if (!string.IsNullOrEmpty(autoPath))
            {
                _ = viewModel.ImportExcelFileAsync(autoPath);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Import data file",
                Filter = "Data files (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Excel workbook (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls|CSV (*.csv)|*.csv"
            };

            if (dialog.ShowDialog(GetOwner()) == true)
            {
                _ = viewModel.ImportExcelFileAsync(dialog.FileName);
            }
        }

        private void OnPhotoFolderPicked(object sender, EventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
            {
                return;
            }

            // UI-test hook: IDSTACK_AUTO_PHOTOS skips the native dialog.
            var autoPath = Environment.GetEnvironmentVariable("IDSTACK_AUTO_PHOTOS");
            if (!string.IsNullOrEmpty(autoPath))
            {
                _ = viewModel.ImportPhotoFolderAsync(autoPath);
                return;
            }

            // FolderBrowserDialog (WinForms) is the Win7-compatible folder picker.
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the folder containing photos.",
                ShowNewFolderButton = false
            })
            {
                var owner = GetOwner();
                var result = owner != null
                    ? dialog.ShowDialog(new Win32Window(owner))
                    : dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _ = viewModel.ImportPhotoFolderAsync(dialog.SelectedPath);
                }
            }
        }

        private void OnBrowseDesignClick(object sender, RoutedEventArgs e)
        {
            ViewModel?.PickDesign();
        }

        private Window GetOwner()
        {
            return Window.GetWindow(this) ?? Application.Current?.MainWindow;
        }

        /// <summary>Adapter so FolderBrowserDialog can be owned by a WPF window.</summary>
        private sealed class Win32Window : System.Windows.Forms.IWin32Window
        {
            private readonly IntPtr _handle;

            public Win32Window(Window window)
            {
                _handle = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            }

            public IntPtr Handle => _handle;
        }
    }
}
