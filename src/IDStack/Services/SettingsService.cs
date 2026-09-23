using System;
using System.IO;
using System.Threading.Tasks;
using IDStack.Core.Constants;
using IDStack.Core.Interfaces;
using IDStack.Core.Models;
using Newtonsoft.Json;

namespace IDStack.Services
{
    /// <summary>
    /// Persists application settings as JSON under %APPDATA%\IDStack.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private readonly object _saveLock = new object();
        private AppSettings _settings;

        /// <summary>
        /// Creates the service, storing settings in the given directory.
        /// </summary>
        /// <param name="settingsDirectory">Directory for the settings file; defaults to %APPDATA%\IDStack.</param>
        public SettingsService(string settingsDirectory = null)
        {
            var directory = settingsDirectory
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    AppConstants.AppDataFolderName);

            Directory.CreateDirectory(directory);
            _settingsFilePath = Path.Combine(directory, AppConstants.SettingsFileName);
            _settings = CreateDefault();
        }

        /// <inheritdoc />
        public AppSettings Settings => _settings;

        /// <inheritdoc />
        public async Task<AppSettings> LoadAsync()
        {
            if (!File.Exists(_settingsFilePath))
            {
                _settings = CreateDefault();
                await SaveAsync().ConfigureAwait(false);
                return _settings;
            }

            try
            {
                var json = await ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
                _settings = JsonConvert.DeserializeObject<AppSettings>(json) ?? CreateDefault();
            }
            catch (Exception ex) when (ex is JsonException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // Corrupt or unreadable settings must never prevent the app from starting.
                _settings = CreateDefault();
            }

            return _settings;
        }

        /// <inheritdoc />
        public async Task SaveAsync()
        {
            var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);

            // Write to a temp file first, then replace, so a crash cannot corrupt settings.
            var tempPath = _settingsFilePath + ".tmp";
            using (var writer = new StreamWriter(tempPath, false))
            {
                await writer.WriteAsync(json).ConfigureAwait(false);
            }

            lock (_saveLock)
            {
                if (File.Exists(_settingsFilePath))
                {
                    File.Replace(tempPath, _settingsFilePath, null);
                }
                else
                {
                    File.Move(tempPath, _settingsFilePath);
                }
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            _settings = CreateDefault();
        }

        private static AppSettings CreateDefault()
        {
            return new AppSettings();
        }

        private static async Task<string> ReadAllTextAsync(string path)
        {
            using (var reader = new StreamReader(path))
            {
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
        }
    }
}
