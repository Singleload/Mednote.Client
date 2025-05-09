using CommunityToolkit.Mvvm.Input;
using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mednote.Client.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IAudioService _audioService;
        private readonly IApiService _apiService;

        private ObservableCollection<AudioDeviceInfo> _inputDevices = new();
        private ObservableCollection<AudioDeviceInfo> _outputDevices = new();
        private string _selectedInputDeviceId = string.Empty;
        private string _selectedOutputDeviceId = string.Empty;
        private string _storageDirectory = string.Empty;
        private bool _autoStartTranscription = true;
        private bool _saveRawAudio = true;
        private bool _isApiAvailable;

        // Commands
        public AsyncRelayCommand LoadSettingsCommand { get; }
        public AsyncRelayCommand SaveSettingsCommand { get; }
        public AsyncRelayCommand ResetSettingsCommand { get; }
        public AsyncRelayCommand RefreshDevicesCommand { get; }
        public AsyncRelayCommand CheckApiConnectionCommand { get; }
        public RelayCommand BrowseStorageDirectoryCommand { get; }
        public AsyncRelayCommand ClearTemporaryFilesCommand { get; }

        // Properties
        public ObservableCollection<AudioDeviceInfo> InputDevices
        {
            get => _inputDevices;
            set => SetProperty(ref _inputDevices, value);
        }

        public ObservableCollection<AudioDeviceInfo> OutputDevices
        {
            get => _outputDevices;
            set => SetProperty(ref _outputDevices, value);
        }

        public string SelectedInputDeviceId
        {
            get => _selectedInputDeviceId;
            set => SetProperty(ref _selectedInputDeviceId, value);
        }

        public string SelectedOutputDeviceId
        {
            get => _selectedOutputDeviceId;
            set => SetProperty(ref _selectedOutputDeviceId, value);
        }

        public string StorageDirectory
        {
            get => _storageDirectory;
            set => SetProperty(ref _storageDirectory, value);
        }

        public bool AutoStartTranscription
        {
            get => _autoStartTranscription;
            set => SetProperty(ref _autoStartTranscription, value);
        }

        public bool SaveRawAudio
        {
            get => _saveRawAudio;
            set => SetProperty(ref _saveRawAudio, value);
        }

        public bool IsApiAvailable
        {
            get => _isApiAvailable;
            set => SetProperty(ref _isApiAvailable, value);
        }

        public SettingsViewModel(
            ISettingsService settingsService,
            IAudioService audioService,
            IApiService apiService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Initialize commands
            LoadSettingsCommand = new AsyncRelayCommand(LoadSettingsAsync);
            SaveSettingsCommand = new AsyncRelayCommand(SaveSettingsAsync);
            ResetSettingsCommand = new AsyncRelayCommand(ResetSettingsAsync);
            RefreshDevicesCommand = new AsyncRelayCommand(RefreshDevicesAsync);
            CheckApiConnectionCommand = new AsyncRelayCommand(CheckApiConnectionAsync);
            BrowseStorageDirectoryCommand = new RelayCommand(BrowseStorageDirectory);
            ClearTemporaryFilesCommand = new AsyncRelayCommand(ClearTemporaryFilesAsync);
        }

        private async Task LoadSettingsAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    // Load settings
                    var settings = _settingsService.GetSettings();

                    // Update view model properties
                    SelectedInputDeviceId = settings.SelectedInputDeviceId;
                    SelectedOutputDeviceId = settings.SelectedOutputDeviceId;
                    StorageDirectory = settings.StorageDirectory;
                    AutoStartTranscription = settings.AutoStartTranscription;
                    SaveRawAudio = settings.SaveRawAudio;

                    // Load audio devices
                    await RefreshDevicesAsync();

                    // Check API connection
                    await CheckApiConnectionAsync();
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid inläsning av inställningar: {ex.Message}");
                }
            });
        }

        private async Task SaveSettingsAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    // Create settings object
                    var settings = new AppSettings
                    {
                        SelectedInputDeviceId = SelectedInputDeviceId,
                        SelectedOutputDeviceId = SelectedOutputDeviceId,
                        StorageDirectory = StorageDirectory,
                        AutoStartTranscription = AutoStartTranscription,
                        SaveRawAudio = SaveRawAudio
                    };

                    // Save settings
                    await _settingsService.SaveSettingsAsync(settings);

                    // Ensure directory exists
                    _settingsService.EnsureStorageDirectoryExists();

                    StatusMessage = "Inställningar sparade.";

                    // Clear status after a delay
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid sparande av inställningar: {ex.Message}");
                }
            });
        }

        private async Task ResetSettingsAsync()
        {
            // Ask for confirmation
            var result = System.Windows.MessageBox.Show(
                "Är du säker på att du vill återställa alla inställningar till standardvärden?",
                "Bekräfta återställning",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    // Create default settings
                    var settings = new AppSettings();

                    // Save settings
                    await _settingsService.SaveSettingsAsync(settings);

                    // Reload settings
                    await LoadSettingsAsync();

                    StatusMessage = "Inställningar återställda.";

                    // Clear status after a delay
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid återställning av inställningar: {ex.Message}");
                }
            });
        }

        private async Task RefreshDevicesAsync()
        {
            try
            {
                // Get input devices
                var inputDevices = _audioService.GetInputDevices();
                InputDevices = new ObservableCollection<AudioDeviceInfo>(inputDevices);

                // Select default input device if none selected
                if (string.IsNullOrEmpty(SelectedInputDeviceId) && InputDevices.Count > 0)
                {
                    var defaultDevice = InputDevices.FirstOrDefault(d => d.IsDefault);
                    SelectedInputDeviceId = defaultDevice?.DeviceId ?? InputDevices[0].DeviceId;
                }

                // Get output devices
                var outputDevices = _audioService.GetOutputDevices();
                OutputDevices = new ObservableCollection<AudioDeviceInfo>(outputDevices);

                // Select default output device if none selected
                if (string.IsNullOrEmpty(SelectedOutputDeviceId) && OutputDevices.Count > 0)
                {
                    var defaultDevice = OutputDevices.FirstOrDefault(d => d.IsDefault);
                    SelectedOutputDeviceId = defaultDevice?.DeviceId ?? OutputDevices[0].DeviceId;
                }

                await Task.CompletedTask; // For async compatibility
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid uppdatering av ljudenheter: {ex.Message}");
            }
        }

        private async Task CheckApiConnectionAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    StatusMessage = "Kontrollerar API-anslutning...";

                    // Actually check the API
                    IsApiAvailable = await _apiService.CheckApiAvailabilityAsync();

                    if (IsApiAvailable)
                    {
                        StatusMessage = "API-anslutning fungerar korrekt.";
                    }
                    else
                    {
                        StatusMessage = "API-anslutning misslyckades. Kontrollera internetanslutning eller serverstatus.";
                    }

                    // Log the result
                    Serilog.Log.Information($"API connection check from settings: {(IsApiAvailable ? "Connected" : "Disconnected")}");

                    // Clear the status message after a delay
                    await Task.Delay(3000);
                    StatusMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid kontroll av API-anslutning: {ex.Message}");
                    IsApiAvailable = false;
                }
            });
        }

        private void BrowseStorageDirectory()
        {
            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "Välj lagringsplats för inspelningar",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true,
                    InitialDirectory = StorageDirectory
                };

                var result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    StorageDirectory = dialog.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid val av lagringskatalog: {ex.Message}");
            }
        }

        private async Task ClearTemporaryFilesAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    await _settingsService.ClearTemporaryFilesAsync();

                    StatusMessage = "Temporära filer rensade.";

                    // Clear status after a delay
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid rensning av temporära filer: {ex.Message}");
                }
            });
        }
    }
}