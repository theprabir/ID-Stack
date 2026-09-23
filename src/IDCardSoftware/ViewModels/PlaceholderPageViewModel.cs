using System;
using IDCardSoftware.Core.Interfaces;

namespace IDCardSoftware.ViewModels
{
    /// <summary>
    /// Placeholder view model shown on pages whose features arrive in later phases.
    /// </summary>
    public class PlaceholderPageViewModel : ViewModelBase
    {
        private readonly ILocalizationService _localizationService;
        private string _title;

        /// <summary>
        /// Creates a placeholder page.
        /// </summary>
        /// <param name="titleKey">Resource key for the page title.</param>
        /// <param name="localizationService">Localization service.</param>
        public PlaceholderPageViewModel(string titleKey, ILocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _title = _localizationService.GetString(titleKey);
            _localizationService.LanguageChanged += OnLanguageChanged;
        }

        /// <summary>Page title.</summary>
        public string Title
        {
            get { return _title; }
            private set { SetProperty(ref _title, value); }
        }

        /// <summary>Resource key of the page title.</summary>
        public string TitleKey { get; set; }

        /// <summary>Placeholder body text shown under the title.</summary>
        public string PlaceholderText => _localizationService.GetString("Home.Phase1Placeholder");

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(TitleKey))
            {
                Title = _localizationService.GetString(TitleKey);
            }
        }
    }
}
