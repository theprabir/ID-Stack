using System;
using System.Linq;
using System.Threading.Tasks;
using IDCardSoftware.Core.Interfaces;
using IDCardSoftware.Core.Models;
using IDCardSoftware.Services;
using IDCardSoftware.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDCardSoftware.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for <see cref="MainViewModel"/> navigation and language behavior.
    /// </summary>
    [TestClass]
    public class MainViewModelTests
    {
        private Mock<ISettingsService> _settingsService;
        private Mock<ILocalizationService> _localizationService;
        private NavigationService _navigationService;
        private AppSettings _settings;

        [TestInitialize]
        public void Setup()
        {
            _settings = new AppSettings();
            _settingsService = new Mock<ISettingsService>();
            _settingsService.SetupGet(s => s.Settings).Returns(_settings);
            _settingsService.Setup(s => s.LoadAsync()).ReturnsAsync(_settings);

            _localizationService = new Mock<ILocalizationService>();
            _localizationService.SetupGet(l => l.CurrentLanguage).Returns("en-US");
            _localizationService.SetupGet(l => l.AvailableLanguages)
                .Returns(new System.Collections.Generic.Dictionary<string, string> { { "en-US", "English" } });
            _localizationService.Setup(l => l.GetString(It.IsAny<string>())).Returns<string>(key => key);

            _navigationService = new NavigationService();
            _navigationService.Register(NavigationKeys.Home, () => new object());
            _navigationService.Register(NavigationKeys.TemplateEditor, () => new object());
            _navigationService.Register(NavigationKeys.Settings, () => new object());
        }

        private MainViewModel CreateViewModel()
        {
            return new MainViewModel(_navigationService, _localizationService.Object, _settingsService.Object);
        }

        [TestMethod]
        public async Task InitializeAsync_NavigatesToHome()
        {
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            Assert.AreEqual(NavigationKeys.Home, viewModel.CurrentPageKey);
            Assert.IsNotNull(viewModel.CurrentViewModel);
        }

        [TestMethod]
        public async Task InitializeAsync_AppliesSavedLanguage()
        {
            _settings.Language = "es-ES";
            _localizationService.SetupGet(l => l.CurrentLanguage).Returns("en-US");

            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            _localizationService.Verify(l => l.SetLanguage("es-ES"), Times.Once);
        }

        [TestMethod]
        public async Task SettingCurrentLanguage_UpdatesSettings()
        {
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            viewModel.CurrentLanguage = "hi-IN";

            Assert.AreEqual("hi-IN", _settings.Language);
            _localizationService.Verify(l => l.SetLanguage("hi-IN"), Times.Once);
        }

        [TestMethod]
        public async Task NewTemplateCommand_NavigatesToTemplateEditor()
        {
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            viewModel.NewTemplateCommand.Execute(null);

            Assert.AreEqual(NavigationKeys.TemplateEditor, viewModel.CurrentPageKey);
        }

        [TestMethod]
        public async Task OpenSettingsCommand_NavigatesToSettings()
        {
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            viewModel.OpenSettingsCommand.Execute(null);

            Assert.AreEqual(NavigationKeys.Settings, viewModel.CurrentPageKey);
        }

        [TestMethod]
        public void ExitCommand_RaisesExitRequested()
        {
            var viewModel = CreateViewModel();
            var raised = false;
            viewModel.ExitRequested += (s, e) => raised = true;

            viewModel.ExitCommand.Execute(null);

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void NavigationItems_CoversAllRegisteredPages()
        {
            var viewModel = CreateViewModel();
            Assert.AreEqual(6, viewModel.NavigationItems.Count);
        }

        [TestMethod]
        public async Task CurrentPageTitle_MatchesNavigationKey()
        {
            var viewModel = CreateViewModel();
            await viewModel.InitializeAsync();

            viewModel.OpenSettingsCommand.Execute(null);

            Assert.AreEqual(NavigationKeys.Settings, viewModel.CurrentPageKey);
            // The mock returns the title resource key as the localized string.
            Assert.AreEqual("Nav.Settings", viewModel.CurrentPageTitle);
        }
    }
}
