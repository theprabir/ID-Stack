using System;
using System.Windows;
using System.Windows.Controls;
using IDCardSoftware.ViewModels;

namespace IDCardSoftware.Views
{
    /// <summary>
    /// Main application shell: hosts the sidebar navigation, header, and content pages.
    /// Code-behind is intentionally minimal; all behavior comes from MainViewModel.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        /// <summary>
        /// Creates the main window and binds its view model.
        /// </summary>
        /// <param name="viewModel">Main view model.</param>
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            DataContext = _viewModel;

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }
    }
}
