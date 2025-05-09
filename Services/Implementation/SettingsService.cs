using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Implementation
{
    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _cachedSettings;
        private readonly object _lockObject = new object();

        public SettingsService()
        {
            // Set up the settings file path in the app data folder
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mednote");

            // Ensure the directory exists
            Directory.CreateDirectory(appDataPath);

            _settingsFilePath = Path.Combine(appDataPath, "settings.json");

            // Initialize settings - ensure we have a valid cached settings object
            _cachedSettings = LoadSettings() ?? CreateDefaultSettings();

            // Always ensure the storage directory exists after initializing settings
            EnsureStorageDirectoryExists();
        }

        public AppSettings GetSettings()
        {
            lock (_lockObject)
            {
                return _cachedSettings;
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                // Update cached settings
                lock (_lockObject)
                {
                    _cachedSettings = settings;
                }

                // Serialize settings to JSON
                var json = JsonConvert.SerializeObject(settings, Formatting.Indented);

                // Write to file
                await File.WriteAllTextAsync(_settingsFilePath, json);

                // Ensure storage directory exists
                EnsureStorageDirectoryExists();

                Serilog.Log.Information("Settings saved successfully");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error saving settings");
                throw;
            }
        }

        private AppSettings? LoadSettings()
        {
            try
            {
                // Check if settings file exists
                if (!File.Exists(_settingsFilePath))
                    return null;

                // Read and deserialize the settings
                var json = File.ReadAllText(_settingsFilePath);
                return JsonConvert.DeserializeObject<AppSettings>(json);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error loading settings. Using defaults.");
                return null;
            }
        }

        private AppSettings CreateDefaultSettings()
        {
            var settings = new AppSettings();

            // Set the default storage directory
            var storageDir = Path.Combine(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mednote"),
                "Recordings");

            settings.StorageDirectory = storageDir;

            return settings;
        }

        public bool EnsureStorageDirectoryExists()
        {
            try
            {
                // Guard against null _cachedSettings
                if (_cachedSettings == null)
                {
                    _cachedSettings = CreateDefaultSettings();
                }

                // Guard against null or empty StorageDirectory
                if (string.IsNullOrEmpty(_cachedSettings.StorageDirectory))
                {
                    var defaultDir = Path.Combine(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "Mednote"),
                        "Recordings");

                    _cachedSettings.StorageDirectory = defaultDir;
                }

                // Create the storage directory if it doesn't exist
                Directory.CreateDirectory(_cachedSettings.StorageDirectory);
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error creating storage directory");
                return false;
            }
        }

        public async Task ClearTemporaryFilesAsync()
        {
            try
            {
                // Guard against null cached settings
                if (_cachedSettings == null || string.IsNullOrEmpty(_cachedSettings.StorageDirectory))
                {
                    Serilog.Log.Warning("Cannot clear temporary files: Storage directory is not defined");
                    return;
                }

                var storageDir = _cachedSettings.StorageDirectory;

                // Make sure the directory exists before trying to find files in it
                if (!Directory.Exists(storageDir))
                {
                    return;
                }

                // Find all mono files (which are temporary conversions)
                var monoFiles = Directory.GetFiles(storageDir, "*_mono.wav");

                // Delete each file
                foreach (var file in monoFiles)
                {
                    File.Delete(file);
                    Serilog.Log.Information($"Deleted temporary file: {file}");
                }

                await Task.CompletedTask; // For async compatibility
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error clearing temporary files");
            }
        }
    }
}