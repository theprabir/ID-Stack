using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IDCardSoftware.Core.Interfaces;
using IDCardSoftware.Core.Models;
using IDCardSoftware.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace IDCardSoftware.Tests.ViewModels
{
    /// <summary>
    /// Unit tests for <see cref="SettingsViewModel"/>.
    /// </summary>
    [TestClass]
    public class SettingsViewModelTests
    {
        private Mock<ISettingsService> _settingsService;
        private Mock<ILocalizationService> _localizationService;
        private AppSettings _settings;

        [TestInitialize]
        public void Setup()
        {
            _settings = new AppSettings();
            _settingsService = new Mock<ISettingsService>();
            _settingsService.SetupGet(s => s.Settings).Returns(_settings);
            _settingsService.Setup(s => s.SaveAsync()).Returns(Task.CompletedTask);

            _localizationService = new Mock<ILocalizationService>();
            _localizationService.SetupGet(l => l.CurrentLanguage).Returns("en-US");
            _localizationService.SetupGet(l => l.AvailableLanguages)
                .Returns(new Dictionary<string, string> { { "en-US", "English" }, { "hi-IN", "हिंदी" } });
            _localizationService.Setup(l => l.GetString(It.IsAny<string>())).Returns<string>(key => key);
        }

        [TestMethod]
        public void Constructor_LoadsCurrentSettings()
        {
            _settings.Language = "hi-IN";
            _settings.ShowGrid = false;
            _settings.GridSizeMm = 3.0;

            var viewModel = new SettingsViewModel(_settingsService.Object, _localizationService.Object);

            Assert.AreEqual("hi-IN", viewModel.SelectedLanguage);
            Assert.IsFalse(viewModel.ShowGrid);
            Assert.AreEqual(3.0, viewModel.GridSizeMm);
        }

        [TestMethod]
        public async Task SaveAsync_PersistsEditedValues()
        {
            var viewModel = new SettingsViewModel(_settingsService.Object, _localizationService.Object);
            viewModel.ShowGrid = false;
            viewModel.GridSizeMm = 5.0;
            viewModel.MeasurementUnit = "inch";

            await viewModel.SaveAsync();

            Assert.IsFalse(_settings.ShowGrid);
            Assert.AreEqual(5.0, _settings.GridSizeMm);
            Assert.AreEqual("inch", _settings.MeasurementUnit);
            _settingsService.Verify(s => s.SaveAsync(), Times.Once);
            Assert.IsTrue(viewModel.IsSaved);
        }

        [TestMethod]
        public async Task SaveAsync_WhenLanguageChanged_AppliesLanguage()
        {
            var viewModel = new SettingsViewModel(_settingsService.Object, _localizationService.Object);
            viewModel.SelectedLanguage = "hi-IN";

            await viewModel.SaveAsync();

            _localizationService.Verify(l => l.SetLanguage("hi-IN"), Times.Once);
        }

        [TestMethod]
        public async Task SaveAsync_WhenLanguageUnchanged_DoesNotReapply()
        {
            var viewModel = new SettingsViewModel(_settingsService.Object, _localizationService.Object);
            viewModel.SelectedLanguage = "en-US";

            await viewModel.SaveAsync();

            _localizationService.Verify(l => l.SetLanguage(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void LanguageOptions_ExposesAvailableLanguages()
        {
            var viewModel = new SettingsViewModel(_settingsService.Object, _localizationService.Object);
            Assert.AreEqual(2, viewModel.LanguageOptions.Count);
        }
    }
}
