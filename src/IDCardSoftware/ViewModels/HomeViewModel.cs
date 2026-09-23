using System;
using IDCardSoftware.Commands;
using IDCardSoftware.Core.Interfaces;
using IDCardSoftware.Services;

namespace IDCardSoftware.ViewModels
{
    /// <summary>
    /// Start page view model with quick actions for the main workflows.
    /// </summary>
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly ILocalizationService _localizationService;

        /// <summary>
        /// Creates the home view model.
        /// </summary>
        /// <param name="navigationService">Navigation service.</param>
        /// <param name="localizationService">Localization service.</param>
        public HomeViewModel(INavigationService navigationService, ILocalizationService localizationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

            NewTemplateCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationKeys.TemplateEditor));
            OpenTemplateLibraryCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationKeys.TemplateLibrary));
            ImportExcelCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationKeys.DataImport));
            ImportPsdCommand = new RelayCommand(_ => _navigationService.NavigateTo(NavigationKeys.TemplateEditor));
        }

        /// <summary>Command creating a new template.</summary>
        public RelayCommand NewTemplateCommand { get; }

        /// <summary>Command opening the template library.</summary>
        public RelayCommand OpenTemplateLibraryCommand { get; }

        /// <summary>Command starting the Excel import flow.</summary>
        public RelayCommand ImportExcelCommand { get; }

        /// <summary>Command starting the PSD import flow.</summary>
        public RelayCommand ImportPsdCommand { get; }

        /// <summary>Welcome heading text.</summary>
        public string WelcomeText => _localizationService.GetString("Home.Welcome");

        /// <summary>Welcome subtitle text.</summary>
        public string SubtitleText => _localizationService.GetString("Home.Subtitle");

        /// <summary>Quick actions card header.</summary>
        public string QuickActionsHeader => _localizationService.GetString("Home.QuickActions");

        /// <summary>Coming soon card header.</summary>
        public string ComingSoonHeader => _localizationService.GetString("Home.ComingSoon");

        /// <summary>Coming soon body text.</summary>
        public string ComingSoonText => _localizationService.GetString("Home.Phase1Placeholder");

        /// <summary>New template action label.</summary>
        public string NewTemplateActionText => _localizationService.GetString("Home.NewTemplate");

        /// <summary>Template library action label.</summary>
        public string TemplateLibraryActionText => _localizationService.GetString("Home.TemplateLibrary");

        /// <summary>Import Excel action label.</summary>
        public string ImportExcelActionText => _localizationService.GetString("Home.ImportExcel");

        /// <summary>Import PSD action label.</summary>
        public string ImportPsdActionText => _localizationService.GetString("Home.ImportPSD");
    }
}
