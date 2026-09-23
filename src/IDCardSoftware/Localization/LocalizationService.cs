using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using IDCardSoftware.Core.Interfaces;

namespace IDCardSoftware.Localization
{
    /// <summary>
    /// Provides localized strings using embedded resx resources and switches the UI
    /// culture application-wide at runtime.
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private readonly ResourceManager _resourceManager;
        private readonly Dictionary<string, string> _availableLanguages;
        private string _currentLanguage;

        /// <summary>
        /// Creates the service bound to <see cref="Strings"/> resources.
        /// </summary>
        public LocalizationService()
        {
            _resourceManager = new ResourceManager(
                "IDCardSoftware.Localization.Strings",
                Assembly.GetExecutingAssembly());

            _availableLanguages = new Dictionary<string, string>
            {
                { "en-US", "English" },
                { "hi-IN", "हिंदी" },
                { "mr-IN", "मराठी" },
                { "or-IN", "ଓଡ଼ିଆ" },
                { "bn-IN", "বাংলা" },
                { "ta-IN", "தமிழ்" },
                { "te-IN", "తెలుగు" },
                { "kn-IN", "ಕನ್ನಡ" },
                { "gu-IN", "ગુજરાતી" },
                { "pa-IN", "ਪੰਜਾਬੀ" },
                { "es-ES", "Español" }
            };

            _currentLanguage = "en-US";
        }

        /// <inheritdoc />
        public event EventHandler LanguageChanged;

        /// <inheritdoc />
        public string CurrentLanguage => _currentLanguage;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> AvailableLanguages => _availableLanguages;

        /// <inheritdoc />
        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            var value = _resourceManager.GetString(key, CultureInfo.CurrentUICulture);
            return value ?? key;
        }

        /// <inheritdoc />
        public string GetFormattedString(string key, params object[] args)
        {
            var format = GetString(key);
            if (args == null || args.Length == 0)
            {
                return format;
            }

            try
            {
                return string.Format(CultureInfo.CurrentUICulture, format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        /// <inheritdoc />
        public void SetLanguage(string cultureCode)
        {
            if (string.IsNullOrEmpty(cultureCode) || !_availableLanguages.ContainsKey(cultureCode))
            {
                throw new ArgumentException("Unsupported language: " + cultureCode, nameof(cultureCode));
            }

            if (cultureCode == _currentLanguage)
            {
                return;
            }

            _currentLanguage = cultureCode;
            var culture = new CultureInfo(cultureCode);

            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
