using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IDStack.Commands;
using IDStack.Core.Interfaces;

namespace IDStack.ViewModels
{
    /// <summary>
    /// View model for the settings page.
    /// </summary>
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        private string _selectedLanguage;
        private bool _showGrid;
        private bool _snapToGrid;
        private double _gridSizeMm;
        private string _measurementUnit;
        private bool _isSaved;

        /// <summary>
        /// Creates the settings view model.
        /// </summary>
        public SettingsViewModel(ISettingsService settingsService, ILocalizationService localizationService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

            LanguageOptions = _localizationService.AvailableLanguages
                .Select(kvp => new LanguageOption(kvp.Key, kvp.Value))
                .ToList();

            SaveCommand = new RelayCommand(async _ => await SaveAsync().ConfigureAwait(true));

            var settings = _settingsService.Settings;
            _selectedLanguage = settings.Language;
            _showGrid = settings.ShowGrid;
            _snapToGrid = settings.SnapToGrid;
            _gridSizeMm = settings.GridSizeMm;
            _measurementUnit = settings.MeasurementUnit;
        }

        /// <summary>Available language options.</summary>
        public IReadOnlyList<LanguageOption> LanguageOptions { get; }

        /// <summary>Save command.</summary>
        public RelayCommand SaveCommand { get; }

        /// <summary>Selected UI language.</summary>
        public string SelectedLanguage
        {
            get { return _selectedLanguage; }
            set { SetProperty(ref _selectedLanguage, value); }
        }

        /// <summary>Show grid toggle.</summary>
        public bool ShowGrid
        {
            get { return _showGrid; }
            set { SetProperty(ref _showGrid, value); }
        }

        /// <summary>Snap to grid toggle.</summary>
        public bool SnapToGrid
        {
            get { return _snapToGrid; }
            set { SetProperty(ref _snapToGrid, value); }
        }

        /// <summary>Grid size in millimeters.</summary>
        public double GridSizeMm
        {
            get { return _gridSizeMm; }
            set { SetProperty(ref _gridSizeMm, value); }
        }

        /// <summary>Measurement unit ("mm" or "inch").</summary>
        public string MeasurementUnit
        {
            get { return _measurementUnit; }
            set { SetProperty(ref _measurementUnit, value); }
        }

        /// <summary>True after a successful save (drives the saved indicator).</summary>
        public bool IsSaved
        {
            get { return _isSaved; }
            private set { SetProperty(ref _isSaved, value); }
        }

        /// <summary>Localized page title.</summary>
        public string SettingsTitle => _localizationService.GetString("Settings.Title");

        /// <summary>Localized language label.</summary>
        public string LanguageLabel => _localizationService.GetString("Settings.Language");

        /// <summary>Localized show-grid label.</summary>
        public string ShowGridLabel => _localizationService.GetString("Settings.ShowGrid");

        /// <summary>Localized snap-to-grid label.</summary>
        public string SnapToGridLabel => _localizationService.GetString("Settings.SnapToGrid");

        /// <summary>Localized grid-size label.</summary>
        public string GridSizeLabel => _localizationService.GetString("Settings.GridSize");

        /// <summary>Localized measurement-unit label.</summary>
        public string MeasurementUnitLabel => _localizationService.GetString("Settings.MeasurementUnit");

        /// <summary>Localized save button text.</summary>
        public string SaveButtonText => _localizationService.GetString("Button.Save");

        /// <summary>Localized saved confirmation text.</summary>
        public string SavedText => _localizationService.GetString("Settings.Saved");

        /// <summary>Persists the edited values and applies the chosen language.</summary>
        public async Task SaveAsync()
        {
            var settings = _settingsService.Settings;
            settings.Language = SelectedLanguage;
            settings.ShowGrid = ShowGrid;
            settings.SnapToGrid = SnapToGrid;
            settings.GridSizeMm = GridSizeMm;
            settings.MeasurementUnit = MeasurementUnit;

            await _settingsService.SaveAsync().ConfigureAwait(true);

            if (!string.Equals(_localizationService.CurrentLanguage, SelectedLanguage, StringComparison.Ordinal))
            {
                _localizationService.SetLanguage(SelectedLanguage);
            }

            IsSaved = true;
        }
    }
}
