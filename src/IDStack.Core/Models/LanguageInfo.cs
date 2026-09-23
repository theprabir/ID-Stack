using System;
using System.Collections.Generic;

namespace IDStack.Core.Models
{
    /// <summary>
    /// Metadata describing one of the languages supported by the application.
    /// </summary>
    public class LanguageInfo
    {
        /// <summary>Creates a language descriptor.</summary>
        /// <param name="cultureCode">Culture code (e.g. "hi-IN").</param>
        /// <param name="displayName">Native display name (e.g. "हिंदी").</param>
        public LanguageInfo(string cultureCode, string displayName)
        {
            CultureCode = cultureCode;
            DisplayName = displayName;
        }

        /// <summary>Culture code such as "hi-IN".</summary>
        public string CultureCode { get; }

        /// <summary>Native display name shown in the language selector.</summary>
        public string DisplayName { get; }
    }
}
