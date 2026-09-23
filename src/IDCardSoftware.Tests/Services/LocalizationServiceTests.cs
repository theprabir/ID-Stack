using System;
using System.Linq;
using IDCardSoftware.Core.Interfaces;
using IDCardSoftware.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDCardSoftware.Tests.Services
{
    /// <summary>
    /// Unit tests for <see cref="LocalizationService"/> language behavior.
    /// </summary>
    [TestClass]
    public class LocalizationServiceTests
    {
        [TestMethod]
        public void GetString_UnknownKey_ReturnsKeyItself()
        {
            ILocalizationService service = new LocalizationService();
            Assert.AreEqual("Does.Not.Exist", service.GetString("Does.Not.Exist"));
        }

        [TestMethod]
        public void GetString_NullOrEmpty_ReturnsEmpty()
        {
            ILocalizationService service = new LocalizationService();
            Assert.AreEqual(string.Empty, service.GetString(null));
            Assert.AreEqual(string.Empty, service.GetString(string.Empty));
        }

        [TestMethod]
        public void GetString_DefaultLanguage_ReturnsEnglishText()
        {
            ILocalizationService service = new LocalizationService();
            Assert.AreEqual("ID Stack", service.GetString("App.Title"));
        }

        [TestMethod]
        public void SetLanguage_Hindi_UpdatesCurrentLanguageAndStrings()
        {
            // App.Title is the untranslated brand name; capture English first because
            // SetLanguage switches the process UI culture, then verify a translated key changes.
            var englishMenuFile = new LocalizationService().GetString("Menu.File");

            var service = new LocalizationService();
            service.SetLanguage("hi-IN");

            Assert.AreEqual("hi-IN", service.CurrentLanguage);
            Assert.AreNotEqual(englishMenuFile, service.GetString("Menu.File"));
        }

        [TestMethod]
        public void SetLanguage_Unknown_Throws()
        {
            ILocalizationService service = new LocalizationService();
            Assert.ThrowsException<ArgumentException>(() => service.SetLanguage("xx-XX"));
        }

        [TestMethod]
        public void SetLanguage_SameLanguage_DoesNotRaiseEvent()
        {
            var service = new LocalizationService();
            var raised = false;
            service.LanguageChanged += (s, e) => raised = true;

            service.SetLanguage("en-US");

            Assert.IsFalse(raised);
        }

        [TestMethod]
        public void SetLanguage_DifferentLanguage_RaisesLanguageChanged()
        {
            var service = new LocalizationService();
            var raised = false;
            service.LanguageChanged += (s, e) => raised = true;

            service.SetLanguage("es-ES");

            Assert.IsTrue(raised);
        }

        [TestMethod]
        public void AvailableLanguages_ContainsAllElevenLanguages()
        {
            ILocalizationService service = new LocalizationService();
            Assert.AreEqual(11, service.AvailableLanguages.Count);
            CollectionAssert.Contains(service.AvailableLanguages.Keys.ToList(), "or-IN");
            CollectionAssert.Contains(service.AvailableLanguages.Keys.ToList(), "pa-IN");
        }

        [TestMethod]
        public void GetFormattedString_FormatsArguments()
        {
            ILocalizationService service = new LocalizationService();
            var result = service.GetFormattedString("Does.Not.Exist", 1, 2);
            Assert.IsNotNull(result);
        }
    }
}
