using System;
using System.Windows;
using IDStack.Core.Interfaces;
using IDStack.Services;
using IDStack.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IDStack
{
    /// <summary>
    /// Application entry point: configures the DI container and shows the main window.
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;
        private static ILogger _logger;

        /// <summary>
        /// Gets the application service provider for view code that needs services.
        /// </summary>
        public IServiceProvider Services => _serviceProvider;

        /// <summary>
        /// Builds the service container and creates the main window.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _logger = new LogService();
            _logger.Info("Application starting. Version " + Core.Constants.AppConstants.AppVersion);

            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                RegisterNavigation((ServiceProvider)_serviceProvider);

                var mainWindow = ((ServiceProvider)_serviceProvider).GetRequiredService<Views.MainWindow>();
                MainWindow = mainWindow;
                mainWindow.Show();

                _logger.Info("Main window shown.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Startup failed");
                MessageBox.Show(
                    "Startup failed: " + ex.Message,
                    "ID Stack",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Infrastructure
            services.AddSingleton<Core.Interfaces.ILogger, LogService>();
            services.AddSingleton<Core.Interfaces.ISettingsService, SettingsService>();
            services.AddSingleton<Core.Interfaces.ITemplateService, TemplateService>();
            services.AddSingleton(typeof(Core.Interfaces.IHistoryService<>), typeof(HistoryService<>));
            services.AddSingleton<Core.Interfaces.ILocalizationService, Localization.LocalizationService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Data import (Phase 3)
            services.AddSingleton<Core.Interfaces.IExcelService, ExcelService>();
            services.AddSingleton<Core.Interfaces.IImageProcessingService, ImageProcessingService>();
            services.AddSingleton<Core.Interfaces.IPhotoService, PhotoService>();
            services.AddSingleton<Core.Interfaces.IDataValidationService, DataValidationService>();
            services.AddSingleton<Services.PsdDesignImporter>();
            services.AddSingleton<Core.Interfaces.IDesignImportService, DesignImportService>();
            services.AddSingleton<ViewModels.DataImport.DataImportViewModel>();

            // View models
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<HomeViewModel>();
            services.AddSingleton<ViewModels.TemplateEditor.TemplateEditorViewModel>();
            services.AddSingleton<ViewModels.TemplateEditor.ToolsPanelViewModel>();
            services.AddSingleton<ViewModels.TemplateEditor.CanvasViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<PlaceholderPageViewModel>();
            services.AddTransient<Func<string, PlaceholderPageViewModel>>(
                sp => titleKey => new PlaceholderPageViewModel(
                    titleKey,
                    sp.GetRequiredService<Core.Interfaces.ILocalizationService>()));

            // Main window
            services.AddSingleton<Views.MainWindow>();
        }

        private static void RegisterNavigation(ServiceProvider provider)
        {
            var navigation = provider.GetRequiredService<INavigationService>();
            var placeholders = provider.GetRequiredService<Func<string, PlaceholderPageViewModel>>();

            navigation.Register(NavigationKeys.Home, () => provider.GetRequiredService<HomeViewModel>());
            navigation.Register(NavigationKeys.TemplateEditor, () =>
            {
                var editor = provider.GetRequiredService<ViewModels.TemplateEditor.TemplateEditorViewModel>();
                return editor;
            });
            navigation.Register(NavigationKeys.TemplateLibrary, () => placeholders("Nav.TemplateLibrary"));
            navigation.Register(NavigationKeys.DataImport, () => provider.GetRequiredService<ViewModels.DataImport.DataImportViewModel>());
            navigation.Register(NavigationKeys.BatchProcessing, () => placeholders("Nav.BatchProcessing"));
            navigation.Register(NavigationKeys.Settings, () => provider.GetRequiredService<SettingsViewModel>());
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            _logger?.Error(e.Exception, "Unhandled UI exception");
            MessageBox.Show(
                "An unexpected error occurred: " + e.Exception.Message,
                "ID Stack",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            _logger?.Error(e.ExceptionObject as Exception, "Unhandled non-UI exception");
        }

        /// <summary>
        /// Disposes the DI container on exit.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            _logger?.Info("Application exiting. Code " + e.ApplicationExitCode);
            (_serviceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }
    }
}
