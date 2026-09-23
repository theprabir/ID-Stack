using System;
using System.Collections.Generic;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Provides localized strings for the application UI.
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>Raised after the UI culture changes so views can refresh bindings.</summary>
        event EventHandler LanguageChanged;

        /// <summary>Gets the culture code of the current language (e.g. "en-US").</summary>
        string CurrentLanguage { get; }

        /// <summary>Gets the available languages keyed by culture code with native display names.</summary>
        IReadOnlyDictionary<string, string> AvailableLanguages { get; }

        /// <summary>Returns the localized string for the given key; falls back to the key itself when missing.</summary>
        /// <param name="key">Resource key.</param>
        /// <returns>Localized text.</returns>
        string GetString(string key);

        /// <summary>Returns the localized formatted string for the given key.</summary>
        /// <param name="key">Resource key.</param>
        /// <param name="args">Format arguments.</param>
        /// <returns>Localized formatted text.</returns>
        string GetFormattedString(string key, params object[] args);

        /// <summary>Changes the UI language and persists the choice.</summary>
        /// <param name="cultureCode">Culture code such as "hi-IN".</param>
        void SetLanguage(string cultureCode);
    }
}
