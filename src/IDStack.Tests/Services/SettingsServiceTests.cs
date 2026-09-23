using System;
using System.IO;
using System.Threading.Tasks;
using IDStack.Core.Constants;
using IDStack.Core.Interfaces;
using IDStack.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDStack.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="SettingsService"/> persistence behavior.
    /// </summary>
    [TestClass]
    public class SettingsServiceTests
    {
        private string _tempDirectory;

        [TestInitialize]
        public void Setup()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "IDCardTests_" + Guid.NewGuid().ToString("N"));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [TestMethod]
        public void Constructor_CreatesSettingsDirectory()
        {
            var service = new SettingsService(_tempDirectory);
            Assert.IsTrue(Directory.Exists(_tempDirectory));
            Assert.IsNotNull(service.Settings);
        }

        [TestMethod]
        public async Task LoadAsync_WhenNoFileExists_CreatesDefaults()
        {
            var service = new SettingsService(_tempDirectory);
            var settings = await service.LoadAsync();

            Assert.IsNotNull(settings);
            Assert.AreEqual("en-US", settings.Language);
            Assert.IsTrue(settings.ShowGrid);
            Assert.IsTrue(File.Exists(Path.Combine(_tempDirectory, AppConstants.SettingsFileName)));
        }

        [TestMethod]
        public async Task SaveThenLoad_RoundTripsAllValues()
        {
            var service = new SettingsService(_tempDirectory);
            await service.LoadAsync();

            service.Settings.Language = "hi-IN";
            service.Settings.ShowGrid = false;
            service.Settings.GridSizeMm = 2.5;
            service.Settings.MeasurementUnit = "inch";
            service.Settings.WindowWidth = 1600;
            service.Settings.RecentFiles.Add(@"C:\templates\demo.idcard");
            await service.SaveAsync();

            var second = new SettingsService(_tempDirectory);
            await second.LoadAsync();

            Assert.AreEqual("hi-IN", second.Settings.Language);
            Assert.IsFalse(second.Settings.ShowGrid);
            Assert.AreEqual(2.5, second.Settings.GridSizeMm);
            Assert.AreEqual("inch", second.Settings.MeasurementUnit);
            Assert.AreEqual(1600, second.Settings.WindowWidth);
            Assert.AreEqual(1, second.Settings.RecentFiles.Count);
        }

        [TestMethod]
        public async Task LoadAsync_WhenFileIsCorrupt_ReturnsDefaults()
        {
            var service = new SettingsService(_tempDirectory);
            await service.LoadAsync();

            File.WriteAllText(Path.Combine(_tempDirectory, AppConstants.SettingsFileName), "{ not valid json !!!");

            var second = new SettingsService(_tempDirectory);
            var settings = await second.LoadAsync();

            Assert.IsNotNull(settings);
            Assert.AreEqual("en-US", settings.Language);
            Assert.IsTrue(settings.ShowGrid);
        }

        [TestMethod]
        public void Reset_RestoresDefaultValues()
        {
            var service = new SettingsService(_tempDirectory);
            service.Settings.Language = "es-ES";
            service.Settings.ShowGrid = false;

            service.Reset();

            Assert.AreEqual("en-US", service.Settings.Language);
            Assert.IsTrue(service.Settings.ShowGrid);
        }
    }
}
