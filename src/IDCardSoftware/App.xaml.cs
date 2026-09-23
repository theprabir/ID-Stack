using System;
using System.Windows;
using IDCardSoftware.Services;
using IDCardSoftware.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IDCardSoftware
{
    /// <summary>
    /// Application entry point: configures the DI container and shows the main window.
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceProvider;

        /// <summary>
        /// Builds the service container and creates the main window.
        /// </summary>
        /// <param name="e">Startup event arguments.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            DispatcherUnhandledException += OnDispatcherUnhandledException;

            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            RegisterNavigation((ServiceProvider)_serviceProvider);

            var mainWindow = ((ServiceProvider)_serviceProvider).GetRequiredService<Views.MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Services
            services.AddSingleton<Core.Interfaces.ISettingsService, Services.SettingsService>();
            services.AddSingleton<Core.Interfaces.ILocalizationService, Localization.LocalizationService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // View models
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<HomeViewModel>();
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
            navigation.Register(NavigationKeys.TemplateEditor, () => placeholders("Nav.TemplateEditor"));
            navigation.Register(NavigationKeys.TemplateLibrary, () => placeholders("Nav.TemplateLibrary"));
            navigation.Register(NavigationKeys.DataImport, () => placeholders("Nav.DataImport"));
            navigation.Register(NavigationKeys.BatchProcessing, () => placeholders("Nav.BatchProcessing"));
            navigation.Register(NavigationKeys.Settings, () => provider.GetRequiredService<SettingsViewModel>());
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                "An unexpected error occurred. Please try again.",
                "ID Stack",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        /// <summary>
        /// Disposes the DI container on exit.
        /// </summary>
        /// <param name="e">Exit event arguments.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            (_serviceProvider as IDisposable)?.Dispose();
            base.OnExit(e);
        }
    }
}
