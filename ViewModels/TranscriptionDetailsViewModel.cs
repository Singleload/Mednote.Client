using CommunityToolkit.Mvvm.Input;
using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using Mednote.Client.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Mednote.Client.ViewModels
{
    public class TranscriptionDetailsViewModel : BaseViewModel
    {
        private readonly ITranscriptionService _transcriptionService;
        private readonly IAudioService _audioService;
        private readonly INavigationService _navigationService;
        private readonly ISettingsService _settingsService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IStorageService _storageService;
        private readonly IApiService _apiService;

        private string _transcriptionId = string.Empty;
        private TranscriptionItem? _transcription;
        private bool _isPlaying;
        private bool _isProcessing;
        private bool _isEditing;
        private string _editedTitle = string.Empty;
        private string _editedNotes = string.Empty;
        private string _editedPatientId = string.Empty;
        private bool _isApiConnected;

        // Commands
        public AsyncRelayCommand LoadTranscriptionCommand { get; }
        public AsyncRelayCommand PlayAudioCommand { get; }
        public RelayCommand StopAudioCommand { get; }
        public AsyncRelayCommand<string> CopyTextCommand { get; }
        public AsyncRelayCommand EditCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }
        public RelayCommand CancelEditCommand { get; }
        public AsyncRelayCommand DeleteCommand { get; }
        public RelayCommand BackCommand { get; }
        public AsyncRelayCommand RetranscribeCommand { get; }
        public AsyncRelayCommand CheckApiCommand { get; }

        // Properties
        public TranscriptionItem? Transcription
        {
            get => _transcription;
            set => SetProperty(ref _transcription, value);
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public string EditedTitle
        {
            get => _editedTitle;
            set => SetProperty(ref _editedTitle, value);
        }

        public string EditedNotes
        {
            get => _editedNotes;
            set => SetProperty(ref _editedNotes, value);
        }

        public string EditedPatientId
        {
            get => _editedPatientId;
            set => SetProperty(ref _editedPatientId, value);
        }

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        private CancellationTokenSource? _processingCancellationTokenSource;

        public TranscriptionDetailsViewModel(
            ITranscriptionService transcriptionService,
            IAudioService audioService,
            INavigationService navigationService,
            ISettingsService settingsService,
            IEventAggregator eventAggregator,
            IStorageService storageService,
            IApiService apiService)
        {
            _transcriptionService = transcriptionService ?? throw new ArgumentNullException(nameof(transcriptionService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));

            // Initialize commands correctly with lambda expressions
            LoadTranscriptionCommand = new AsyncRelayCommand(LoadTranscriptionAsync);
            PlayAudioCommand = new AsyncRelayCommand(PlayAudioAsync);
            StopAudioCommand = new RelayCommand(() => StopAudio());
            CopyTextCommand = new AsyncRelayCommand<string>(CopyTextAsync);
            EditCommand = new AsyncRelayCommand(BeginEditAsync);
            SaveCommand = new AsyncRelayCommand(SaveChangesAsync);
            CancelEditCommand = new RelayCommand(() => CancelEdit());
            DeleteCommand = new AsyncRelayCommand(DeleteTranscriptionAsync);
            BackCommand = new RelayCommand(() => NavigateBack());
            RetranscribeCommand = new AsyncRelayCommand(RetranscribeAsync);
            CheckApiCommand = new AsyncRelayCommand(CheckApiConnectionAsync);

            // Check API status on initialization
            _ = CheckApiConnectionAsync();
        }

        public void Initialize(string transcriptionId)
        {
            _transcriptionId = transcriptionId;
            LoadTranscriptionCommand.ExecuteAsync(null);
        }

        private async Task LoadTranscriptionAsync()
        {
            if (string.IsNullOrEmpty(_transcriptionId))
                return;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    Transcription = await _transcriptionService.GetTranscriptionByIdAsync(_transcriptionId);

                    if (Transcription == null)
                    {
                        ShowError("Transkriberingen kunde inte hittas.");
                        NavigateBack();
                        return;
                    }

                    // Initialize edit fields
                    EditedTitle = Transcription.Title;
                    EditedNotes = Transcription.Notes;
                    EditedPatientId = Transcription.PatientId;

                    // Log transcription details
                    Serilog.Log.Information($"Loaded transcription: {Transcription.Id}, File: {Transcription.FilePath}");
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid laddning av transkribering: {ex.Message}");
                    NavigateBack();
                }
            });
        }

        private async Task PlayAudioAsync()
        {
            if (Transcription == null || string.IsNullOrEmpty(Transcription.FilePath))
                return;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    var settings = _settingsService.GetSettings();

                    if (string.IsNullOrEmpty(settings.SelectedOutputDeviceId))
                    {
                        // No output device selected, try to get the default
                        var devices = _audioService.GetOutputDevices();
                        if (devices.Count > 0)
                        {
                            settings.SelectedOutputDeviceId = devices[0].DeviceId;
                            await _settingsService.SaveSettingsAsync(settings);
                        }
                        else
                        {
                            throw new InvalidOperationException("Ingen högtalare hittades.");
                        }
                    }

                    // Check if file exists
                    if (!System.IO.File.Exists(Transcription.FilePath))
                    {
                        throw new System.IO.FileNotFoundException($"Ljudfilen hittades inte: {Transcription.FilePath}");
                    }

                    await _audioService.PlayAudioAsync(Transcription.FilePath, settings.SelectedOutputDeviceId);
                    IsPlaying = true;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid uppspelning av ljud: {ex.Message}");
                }
            });
        }

        private void StopAudio()
        {
            try
            {
                _audioService.StopPlayback();
                IsPlaying = false;
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid stopp av uppspelning: {ex.Message}");
            }
        }

        private async Task CopyTextAsync(string? textType)
        {
            if (Transcription == null)
                return;

            try
            {
                string textToCopy = string.Empty;

                switch (textType)
                {
                    case "raw":
                        textToCopy = Transcription.TranscriptionText;
                        break;
                    case "processed":
                        textToCopy = Transcription.ProcessedText;
                        break;
                    default:
                        return;
                }

                if (string.IsNullOrEmpty(textToCopy))
                {
                    ShowError("Ingen text att kopiera.");
                    return;
                }

                Clipboard.SetText(textToCopy);
                StatusMessage = "Text kopierad till urklipp.";

                // Clear status after a delay
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid kopiering av text: {ex.Message}");
            }
        }

        private async Task BeginEditAsync()
        {
            if (Transcription == null)
                return;

            // Initialize edit fields
            EditedTitle = Transcription.Title;
            EditedNotes = Transcription.Notes;
            EditedPatientId = Transcription.PatientId;

            IsEditing = true;

            await Task.CompletedTask; // For async compatibility
        }

        private async Task SaveChangesAsync()
        {
            if (Transcription == null)
                return;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    // Update the transcription with edited values
                    Transcription.Title = EditedTitle;
                    Transcription.Notes = EditedNotes;
                    Transcription.PatientId = EditedPatientId;

                    // Save changes
                    await _transcriptionService.SaveTranscriptionAsync(Transcription);

                    IsEditing = false;
                    StatusMessage = "Ändringar sparade.";

                    // Clear status after a delay
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid sparande av ändringar: {ex.Message}");
                }
            });
        }

        private void CancelEdit()
        {
            IsEditing = false;
        }

        private async Task DeleteTranscriptionAsync()
        {
            if (Transcription == null)
                return;

            // Ask for confirmation
            var result = MessageBox.Show(
                $"Är du säker på att du vill ta bort transkriberingen '{Transcription.Title}'? Både ljudfilen och transkriberingen kommer att tas bort permanent.",
                "Bekräfta borttagning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    // Stop any playback
                    StopAudio();

                    // Use the secure delete method from storage service
                    var success = await _storageService.SecurelyDeleteAllDataAsync(Transcription.Id);

                    if (success)
                    {
                        // Publish event
                        _eventAggregator.Publish(new TranscriptionDeletedEvent(Transcription.Id));

                        // Display success message
                        StatusMessage = "Transkriberingen har raderats permanent.";

                        // Navigate back after a short delay
                        await Task.Delay(1000);
                        NavigateBack();
                    }
                    else
                    {
                        ShowError("Kunde inte ta bort alla filer relaterade till transkriberingen.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid borttagning av transkribering: {ex.Message}");
                }
            }, "Tar bort transkribering...");
        }

        private void NavigateBack()
        {
            // Stop any playback
            StopAudio();

            // Navigate back
            _navigationService.NavigateTo<HistoryViewModel>();
        }

        public async Task CheckApiConnectionAsync()
        {
            try
            {
                IsApiConnected = await _apiService.CheckApiAvailabilityAsync();
                Serilog.Log.Information($"API connection status: {(IsApiConnected ? "Connected" : "Disconnected")}");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error checking API connection");
                IsApiConnected = false;
            }
        }

        private async Task RetranscribeAsync()
        {
            if (Transcription == null)
                return;

            // First check if API is available
            await CheckApiConnectionAsync();

            if (!IsApiConnected)
            {
                ShowError("Kan inte transkribera: API är inte tillgänglig. Kontrollera din internetanslutning.");
                return;
            }

            // Ask for confirmation
            var result = MessageBox.Show(
                "Är du säker på att du vill göra om transkriberingen? Detta kommer att ersätta den befintliga transkriberingen.",
                "Bekräfta omtranskribering",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Create cancellation token source
            _processingCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _processingCancellationTokenSource.Token;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    IsProcessing = true;
                    StatusMessage = "Bearbetar transkribering...";

                    // Process the recording again
                    Transcription = await _transcriptionService.ProcessRecordingAsync(Transcription.Id, cancellationToken);

                    IsProcessing = false;
                    StatusMessage = "Transkribering slutförd.";

                    // Clear status after a delay
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                catch (OperationCanceledException)
                {
                    // Cancelled by user
                    StatusMessage = "Transkribering avbruten";
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid transkribering: {ex.Message}");
                }
                finally
                {
                    IsProcessing = false;
                    _processingCancellationTokenSource?.Dispose();
                    _processingCancellationTokenSource = null;
                }
            }, "Transkriberar inspelning...");
        }

        public override void Dispose()
        {
            // Stop any playback
            StopAudio();

            // Cancel any processing
            _processingCancellationTokenSource?.Cancel();
            _processingCancellationTokenSource?.Dispose();

            base.Dispose();
        }
    }
}