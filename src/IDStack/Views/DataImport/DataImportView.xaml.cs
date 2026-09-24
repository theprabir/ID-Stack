using System;
using System.Windows;
using System.Windows.Controls;
using IDStack.ViewModels.DataImport;
using Microsoft.Win32;

namespace IDStack.Views.DataImport
{
    /// <summary>
    /// Data-import wizard: hosts file/folder dialogs and forwards picks to the view model.
    /// </summary>
    public partial class DataImportView : UserControl
    {
        /// <summary>
        /// Creates the view.
        /// </summary>
        public DataImportView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private DataImportViewModel ViewModel => DataContext as DataImportViewModel;

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.ExcelFilePicked += OnExcelFilePicked;
            ViewModel.PhotoFolderPicked += OnPhotoFolderPicked;
        }

        private void OnExcelFilePicked(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import data file",
                Filter = "Data files (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Excel workbook (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls|CSV (*.csv)|*.csv"
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            {
                _ = ViewModel.Excel.LoadFileAsync(dialog.FileName);
            }
        }

        private void OnPhotoFolderPicked(object sender, EventArgs e)
        {
            // FolderBrowserDialog (WinForms) is the Win7-compatible folder picker;
            // WPF has no built-in folder dialog on .NET Framework 4.8.
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select the folder containing photos.",
                ShowNewFolderButton = false
            })
            {
                var owner = Window.GetWindow(this);
                var result = owner != null
                    ? dialog.ShowDialog(new Win32Window(owner))
                    : dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _ = ViewModel.Photos.LoadFolderAsync(dialog.SelectedPath);
                }
            }
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
