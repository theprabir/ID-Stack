using System;
using System.Threading.Tasks;

namespace IDStack.Core.Interfaces
{
    /// <summary>
    /// Persists and retrieves application settings.
    /// </summary>
    public interface ISettingsService
    {
        /// <summary>Gets the current settings instance (never null).</summary>
        Models.AppSettings Settings { get; }

        /// <summary>Loads settings from disk, creating defaults when no file exists.</summary>
        /// <returns>The loaded settings.</returns>
        Task<Models.AppSettings> LoadAsync();

        /// <summary>Saves the current settings to disk atomically.</summary>
        /// <returns>A task that completes when the save finishes.</returns>
        Task SaveAsync();

        /// <summary>Resets settings to their default values.</summary>
        void Reset();
    }
}
