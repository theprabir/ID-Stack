using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Commands;
using IDStack.Core.Interfaces;
using IDStack.Services;

namespace IDStack.ViewModels
{
    /// <summary>
    /// Root view model for the main window shell: owns navigation, language and status text.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly ILocalizationService _localizationService;
        private readonly ISettingsService _settingsService;

        private string _currentLanguage;
        private string _statusText;
        private string _currentPageKey;

        /// <summary>
        /// Creates the main view model.
        /// </summary>
        public MainViewModel(
            INavigationService navigationService,
            ILocalizationService localizationService,
            ISettingsService settingsService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            _currentLanguage = _localizationService.CurrentLanguage;
            _statusText = _localizationService.GetString("Status.Ready");
            _localizationService.LanguageChanged += OnLanguageChanged;

            AvailableLanguages = _localizationService.AvailableLanguages
                .Select(kvp => new LanguageOption(kvp.Key, kvp.Value))
                .ToList();

            NavigationItems = new List<NavigationItem>
            {
                new NavigationItem(NavigationKeys.Home, "Nav.Home", key => _localizationService.GetString(key)),
                new NavigationItem(NavigationKeys.TemplateEditor, "Nav.TemplateEditor", key => _localizationService.GetString(key)),
                new NavigationItem(NavigationKeys.TemplateLibrary, "Nav.TemplateLibrary", key => _localizationService.GetString(key)),
                new NavigationItem(NavigationKeys.DataImport, "Nav.DataImport", key => _localizationService.GetString(key)),
                new NavigationItem(NavigationKeys.BatchProcessing, "Nav.BatchProcessing", key => _localizationService.GetString(key)),
                new NavigationItem(NavigationKeys.Settings, "Nav.Settings", key => _localizationService.GetString(key))
            };

            NavigateCommand = new RelayCommand(param => NavigateTo(param as string));
            ExitCommand = new RelayCommand(_ => ExitRequested?.Invoke(this, EventArgs.Empty));
            NewTemplateCommand = new RelayCommand(_ => NavigateTo(NavigationKeys.TemplateEditor));
            OpenTemplateCommand = new RelayCommand(_ => NavigateTo(NavigationKeys.TemplateEditor));
            SaveTemplateCommand = new RelayCommand(_ => { /* Phase 2 */ }, _ => false);
            OpenTemplateLibraryCommand = new RelayCommand(_ => NavigateTo(NavigationKeys.TemplateLibrary));
            OpenSettingsCommand = new RelayCommand(_ => NavigateTo(NavigationKeys.Settings));
            ImportExcelCommand = new RelayCommand(_ => NavigateTo(NavigationKeys.DataImport));
        }

        /// <summary>Raised when the window should close itself.</summary>
        public event EventHandler ExitRequested;

        /// <summary>Available language options for the language selector.</summary>
        public IReadOnlyList<LanguageOption> AvailableLanguages { get; }

        /// <summary>Sidebar navigation entries.</summary>
        public IReadOnlyList<NavigationItem> NavigationItems { get; }

        /// <summary>Command switching the content page.</summary>
        public RelayCommand NavigateCommand { get; }

        /// <summary>Command closing the application.</summary>
        public RelayCommand ExitCommand { get; }

        /// <summary>Command creating a new template.</summary>
        public RelayCommand NewTemplateCommand { get; }

        /// <summary>Command opening an existing template.</summary>
        public RelayCommand OpenTemplateCommand { get; }

        /// <summary>Command saving the current template (enabled in Phase 2).</summary>
        public RelayCommand SaveTemplateCommand { get; }

        /// <summary>Command opening the template library.</summary>
        public RelayCommand OpenTemplateLibraryCommand { get; }

        /// <summary>Command opening settings.</summary>
        public RelayCommand OpenSettingsCommand { get; }

        /// <summary>Command starting the Excel import flow.</summary>
        public RelayCommand ImportExcelCommand { get; }

        /// <summary>Application display title.</summary>
        public string AppTitle => _localizationService.GetString("App.Title");

        /// <summary>Localized menu headers.</summary>
        public string MenuFileHeader => _localizationService.GetString("Menu.File");
        public string MenuEditHeader => _localizationService.GetString("Menu.Edit");
        public string MenuViewHeader => _localizationService.GetString("Menu.View");
        public string MenuHelpHeader => _localizationService.GetString("Menu.Help");
        public string MenuNewTemplateHeader => _localizationService.GetString("Menu.NewTemplate");
        public string MenuOpenTemplateHeader => _localizationService.GetString("Menu.OpenTemplate");
        public string MenuSaveTemplateHeader => _localizationService.GetString("Menu.SaveTemplate");
        public string MenuImportExcelHeader => _localizationService.GetString("Menu.ImportExcel");
        public string MenuImportPhotosHeader => _localizationService.GetString("Menu.ImportPhotos");
        public string MenuImportPsdHeader => _localizationService.GetString("Menu.ImportPSD");
        public string MenuExitHeader => _localizationService.GetString("Menu.Exit");
        public string MenuSettingsHeader => _localizationService.GetString("Menu.Settings");
        public string MenuAboutHeader => _localizationService.GetString("Menu.About");

        /// <summary>Localized title of the current content page.</summary>
        public string CurrentPageTitle
        {
            get
            {
                var item = NavigationItems.FirstOrDefault(n => n.Key == _currentPageKey);
                return item == null ? string.Empty : _localizationService.GetString(item.TitleKey);
            }
        }

        /// <summary>Currently selected sidebar key.</summary>
        public string CurrentPageKey
        {
            get { return _currentPageKey; }
            set
            {
                if (SetProperty(ref _currentPageKey, value))
                {
                    NavigateTo(value);
                }
            }
        }

        /// <summary>Currently selected language culture code.</summary>
        public string CurrentLanguage
        {
            get { return _currentLanguage; }
            set
            {
                if (SetProperty(ref _currentLanguage, value) && !string.IsNullOrEmpty(value))
                {
                    _localizationService.SetLanguage(value);
                    _settingsService.Settings.Language = value;
                    SaveSettingsInBackground();
                }
            }
        }

        /// <summary>Status bar text.</summary>
        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        /// <summary>ViewModel shown in the main content region.</summary>
        public object CurrentViewModel => _navigationService.CurrentViewModel;

        /// <summary>Loads settings, applies the saved language, and shows the start page.</summary>
        public async Task InitializeAsync()
        {
            await _settingsService.LoadAsync().ConfigureAwait(true);

            var savedLanguage = _settingsService.Settings.Language;
            if (!string.IsNullOrEmpty(savedLanguage) && savedLanguage != _localizationService.CurrentLanguage)
            {
                try
                {
                    _localizationService.SetLanguage(savedLanguage);
                    _currentLanguage = savedLanguage;
                    OnPropertyChanged(nameof(CurrentLanguage));
                }
                catch (ArgumentException)
                {
                    // Unknown saved language: keep the default.
                }
            }

            _navigationService.NavigateTo(NavigationKeys.Home);
            _currentPageKey = NavigationKeys.Home;
            OnPropertyChanged(nameof(CurrentPageKey));
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CurrentPageTitle));
        }

        private void NavigateTo(string key)
        {
            if (string.IsNullOrEmpty(key) || key == _navigationService.CurrentKey)
            {
                return;
            }

            try
            {
                _navigationService.NavigateTo(key);
                _currentPageKey = key;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            OnPropertyChanged(nameof(CurrentPageKey));
            OnPropertyChanged(nameof(CurrentViewModel));
            OnPropertyChanged(nameof(CurrentPageTitle));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            StatusText = _localizationService.GetString("Status.Ready");
            OnPropertyChanged(nameof(AppTitle));
            OnPropertyChanged(nameof(MenuFileHeader));
            OnPropertyChanged(nameof(MenuEditHeader));
            OnPropertyChanged(nameof(MenuViewHeader));
            OnPropertyChanged(nameof(MenuHelpHeader));
            OnPropertyChanged(nameof(MenuNewTemplateHeader));
            OnPropertyChanged(nameof(MenuOpenTemplateHeader));
            OnPropertyChanged(nameof(MenuSaveTemplateHeader));
            OnPropertyChanged(nameof(MenuImportExcelHeader));
            OnPropertyChanged(nameof(MenuImportPhotosHeader));
            OnPropertyChanged(nameof(MenuImportPsdHeader));
            OnPropertyChanged(nameof(MenuExitHeader));
            OnPropertyChanged(nameof(MenuSettingsHeader));
            OnPropertyChanged(nameof(MenuAboutHeader));
            OnPropertyChanged(nameof(CurrentPageTitle));
        }

        private void SaveSettingsInBackground()
        {
            // Persist language choice without blocking the UI thread; failures are non-fatal.
            Task.Run(async () =>
            {
                try
                {
                    await _settingsService.SaveAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Settings persistence failures must never crash the app; logging arrives later.
                }
            });
        }
    }

    /// <summary>
    /// Binds a sidebar entry to its page key, title resource key, and localized display name.
    /// </summary>
    public class NavigationItem
    {
        private readonly Func<string, string> _localize;

        /// <summary>Creates a navigation item.</summary>
        /// <param name="key">Page key.</param>
        /// <param name="titleKey">Title resource key.</param>
        /// <param name="localize">Function translating a resource key into display text.</param>
        public NavigationItem(string key, string titleKey, Func<string, string> localize)
        {
            Key = key;
            TitleKey = titleKey;
            _localize = localize ?? (k => k);
        }

        /// <summary>Page key.</summary>
        public string Key { get; }

        /// <summary>Title resource key.</summary>
        public string TitleKey { get; }

        /// <summary>Localized display name.</summary>
        public string DisplayName => _localize(TitleKey);
    }
}
