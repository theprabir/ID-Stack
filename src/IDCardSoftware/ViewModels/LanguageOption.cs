using System;

namespace IDCardSoftware.ViewModels
{
    /// <summary>
    /// Binds a culture code with its native display name for UI lists.
    /// </summary>
    public class LanguageOption
    {
        /// <summary>Creates a language option.</summary>
        /// <param name="cultureCode">Culture code.</param>
        /// <param name="displayName">Native display name.</param>
        public LanguageOption(string cultureCode, string displayName)
        {
            CultureCode = cultureCode;
            DisplayName = displayName;
        }

        /// <summary>Culture code such as "hi-IN".</summary>
        public string CultureCode { get; }

        /// <summary>Native display name.</summary>
        public string DisplayName { get; }
    }
}
