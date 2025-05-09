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
    public class RecordingViewModel : BaseViewModel
    {
        private readonly IAudioService _audioService;
        private readonly ISettingsService _settingsService;
        private readonly ITranscriptionService _transcriptionService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IApiService _apiService;
        private readonly INavigationService _navigationService;

        private bool _isRecording;
        private bool _isPaused;
        private bool _isTranscribing;
        private bool _canStartTranscription;
        private string _recordingTime = "00:00:00";
        private string _recordingTitle = string.Empty;
        private string _currentTranscriptionId = string.Empty;
        private TranscriptionItem? _currentTranscription;
        private string _recordingStatusText = Constants.RecordingInstructionsMessage;
        private string _recordingStatusColor = Constants.StoppedColor;
        private bool _isApiConnected;

        // Commands
        public AsyncRelayCommand StartRecordingCommand { get; }
        public RelayCommand PauseResumeRecordingCommand { get; }
        public AsyncRelayCommand StopRecordingCommand { get; }
        public AsyncRelayCommand StartTranscriptionCommand { get; }
        public RelayCommand CancelCommand { get; }
        public AsyncRelayCommand ViewTranscriptionDetailsCommand { get; }
        public AsyncRelayCommand CheckApiConnectionCommand { get; }

        // Properties
        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                if (SetProperty(ref _isRecording, value))
                {
                    // Ensure commands are notified when this property changes
                    PauseResumeRecordingCommand.NotifyCanExecuteChanged();
                    StopRecordingCommand.NotifyCanExecuteChanged();
                    StartRecordingCommand.NotifyCanExecuteChanged();
                    Serilog.Log.Debug($"IsRecording property changed to {value}");
                }
            }
        }

        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    PauseResumeRecordingCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool IsTranscribing
        {
            get => _isTranscribing;
            set
            {
                if (SetProperty(ref _isTranscribing, value))
                {
                    PauseResumeRecordingCommand.NotifyCanExecuteChanged();
                    StopRecordingCommand.NotifyCanExecuteChanged();
                    StartRecordingCommand.NotifyCanExecuteChanged();
                    StartTranscriptionCommand.NotifyCanExecuteChanged();
                    CancelCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public bool CanStartTranscription
        {
            get => _canStartTranscription;
            set
            {
                if (SetProperty(ref _canStartTranscription, value))
                {
                    StartTranscriptionCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public string RecordingTime
        {
            get => _recordingTime;
            set => SetProperty(ref _recordingTime, value);
        }

        public string RecordingTitle
        {
            get => _recordingTitle;
            set => SetProperty(ref _recordingTitle, value);
        }

        public string RecordingStatusText
        {
            get => _recordingStatusText;
            set => SetProperty(ref _recordingStatusText, value);
        }

        public string RecordingStatusColor
        {
            get => _recordingStatusColor;
            set => SetProperty(ref _recordingStatusColor, value);
        }

        public TranscriptionItem? CurrentTranscription
        {
            get => _currentTranscription;
            set => SetProperty(ref _currentTranscription, value);
        }

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        private CancellationTokenSource? _transcriptionCancellationTokenSource;

        public RecordingViewModel(
            IAudioService audioService,
            ISettingsService settingsService,
            ITranscriptionService transcriptionService,
            IEventAggregator eventAggregator,
            IApiService apiService,
            INavigationService navigationService)
        {
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _transcriptionService = transcriptionService ?? throw new ArgumentNullException(nameof(transcriptionService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            // Initialize commands with proper can-execute functions
            StartRecordingCommand = new AsyncRelayCommand(StartRecordingAsync, CanStartRecording);
            PauseResumeRecordingCommand = new RelayCommand(PauseResumeRecording, CanPauseResumeRecording);
            StopRecordingCommand = new AsyncRelayCommand(StopRecordingAsync, CanStopRecording);
            StartTranscriptionCommand = new AsyncRelayCommand(StartTranscriptionAsync, CanStartTranscriptionExecution);
            CancelCommand = new RelayCommand(CancelTranscription, CanCancelTranscription);
            ViewTranscriptionDetailsCommand = new AsyncRelayCommand(ViewTranscriptionDetailsAsync);
            CheckApiConnectionCommand = new AsyncRelayCommand(CheckApiConnectionAsync);

            // Subscribe to events
            _audioService.RecordingTimeUpdated += OnRecordingTimeUpdated;
            _audioService.RecordingStatusChanged += OnRecordingStatusChanged;

            // Initialize
            ResetState();

            // Log initial state
            Serilog.Log.Debug("RecordingViewModel initialized");
            Serilog.Log.Debug($"Initial state: IsRecording={IsRecording}, IsPaused={IsPaused}, IsTranscribing={IsTranscribing}");
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

        private bool CanStartRecording()
        {
            return !IsRecording && !IsTranscribing;
        }

        private bool CanPauseResumeRecording()
        {
            bool canExecute = IsRecording && !IsTranscribing;
            Serilog.Log.Debug($"CanPauseResumeRecording: IsRecording={IsRecording}, IsTranscribing={IsTranscribing}, result={canExecute}");
            return canExecute;
        }

        private bool CanStopRecording()
        {
            bool canExecute = IsRecording && !IsTranscribing;
            Serilog.Log.Debug($"CanStopRecording: IsRecording={IsRecording}, IsTranscribing={IsTranscribing}, result={canExecute}");
            return canExecute;
        }

        private bool CanStartTranscriptionExecution()
        {
            return CanStartTranscription && !IsTranscribing && IsApiConnected;
        }

        private bool CanCancelTranscription()
        {
            return IsTranscribing;
        }

        private async Task StartRecordingAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    var settings = _settingsService.GetSettings();

                    if (string.IsNullOrEmpty(settings.SelectedInputDeviceId))
                    {
                        // No input device selected, try to get the default
                        var devices = _audioService.GetInputDevices();
                        if (devices.Count > 0)
                        {
                            settings.SelectedInputDeviceId = devices[0].DeviceId;
                            await _settingsService.SaveSettingsAsync(settings);
                        }
                        else
                        {
                            throw new InvalidOperationException("Ingen mikrofon hittades.");
                        }
                    }

                    // Start recording
                    await _audioService.StartRecordingAsync(settings.SelectedInputDeviceId);

                    // Update UI state
                    IsRecording = true;
                    IsPaused = false;
                    CanStartTranscription = false;
                    RecordingTime = "00:00:00";
                    RecordingTitle = $"Inspelning {DateTime.Now:yyyy-MM-dd HH:mm}";
                    RecordingStatusText = "Spelar in...";
                    RecordingStatusColor = Constants.RecordingColor;

                    // Log state after starting recording
                    Serilog.Log.Debug($"After StartRecordingAsync: IsRecording={IsRecording}, IsPaused={IsPaused}");

                    // Clear any previous transcription
                    CurrentTranscription = null;
                    _currentTranscriptionId = string.Empty;

                    // Explicitly notify command availability changed
                    PauseResumeRecordingCommand.NotifyCanExecuteChanged();
                    StopRecordingCommand.NotifyCanExecuteChanged();
                    StartRecordingCommand.NotifyCanExecuteChanged();

                    // Publish event
                    _eventAggregator.Publish(new RecordingStateChangedEvent(IsRecording, IsPaused));
                }
                catch (Exception ex)
                {
                    ShowError($"Kunde inte starta inspelningen: {ex.Message}");
                    ResetState();
                }
            }, "Startar inspelning...");
        }

        private void PauseResumeRecording()
        {
            try
            {
                Serilog.Log.Debug($"PauseResumeRecording called. Current IsPaused={IsPaused}");

                if (IsPaused)
                {
                    _audioService.ResumeRecording();
                    RecordingStatusText = "Spelar in...";
                    RecordingStatusColor = Constants.RecordingColor;
                }
                else
                {
                    _audioService.PauseRecording();
                    RecordingStatusText = "Pausad";
                    RecordingStatusColor = Constants.PausedColor;
                }

                IsPaused = _audioService.IsPaused;
                Serilog.Log.Debug($"After PauseResumeRecording: IsPaused={IsPaused}");

                // Publish event
                _eventAggregator.Publish(new RecordingStateChangedEvent(IsRecording, IsPaused));
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid paus/återupptagning av inspelning: {ex.Message}");
            }
        }

        private async Task StopRecordingAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    Serilog.Log.Debug("StopRecordingAsync called");

                    // Stop recording
                    var filePath = await _audioService.StopAndSaveRecordingAsync();

                    if (string.IsNullOrEmpty(filePath))
                    {
                        throw new InvalidOperationException("Ingen inspelningsfil skapades.");
                    }

                    // Create a transcription item
                    var duration = _audioService.CurrentRecordingTime;
                    var transcription = await _transcriptionService.CreateTranscriptionItemAsync(filePath, duration);

                    // Store the ID for later use
                    _currentTranscriptionId = transcription.Id;

                    // Update the UI state
                    IsRecording = false;
                    IsPaused = false;
                    CanStartTranscription = true;
                    CurrentTranscription = transcription;
                    RecordingTitle = transcription.Title;
                    RecordingStatusText = "Inspelning avslutad";
                    RecordingStatusColor = Constants.StoppedColor;

                    Serilog.Log.Debug($"After StopRecordingAsync: IsRecording={IsRecording}, CanStartTranscription={CanStartTranscription}");

                    // Check API status for transcription availability
                    await CheckApiConnectionAsync();

                    // Automatically start transcription if configured and API is available
                    var settings = _settingsService.GetSettings();
                    if (settings.AutoStartTranscription && IsApiConnected)
                    {
                        await StartTranscriptionAsync();
                    }
                    else if (settings.AutoStartTranscription && !IsApiConnected)
                    {
                        ShowError("Automatisk transkribering avbruten: API är inte tillgänglig. Kontrollera din internetanslutning.");
                    }

                    // Publish event
                    _eventAggregator.Publish(new RecordingStateChangedEvent(IsRecording, IsPaused));
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid avslutning av inspelning: {ex.Message}");
                    ResetState();
                }
            }, "Avslutar och sparar inspelning...");
        }

        private async Task StartTranscriptionAsync()
        {
            // First check if API is available
            await CheckApiConnectionAsync();

            if (!IsApiConnected)
            {
                ShowError("Kan inte transkribera: API är inte tillgänglig. Kontrollera din internetanslutning.");
                return;
            }

            // Create cancellation token source
            _transcriptionCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _transcriptionCancellationTokenSource.Token;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(_currentTranscriptionId))
                    {
                        throw new InvalidOperationException("Ingen inspelning att transkribera.");
                    }

                    // Update state
                    IsTranscribing = true;
                    RecordingStatusText = Constants.TranscribingMessage;

                    // Process the recording (transcribe + ChatGPT)
                    CurrentTranscription = await _transcriptionService.ProcessRecordingAsync(_currentTranscriptionId, cancellationToken);

                    // Update state
                    IsTranscribing = false;
                    CanStartTranscription = false;
                    RecordingStatusText = "Transkribering slutförd";

                    // Publish event
                    _eventAggregator.Publish(new TranscriptionCompletedEvent(_currentTranscriptionId));
                }
                catch (OperationCanceledException)
                {
                    // Cancelled by user
                    RecordingStatusText = "Transkribering avbruten";
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid transkribering: {ex.Message}");
                }
                finally
                {
                    IsTranscribing = false;
                    _transcriptionCancellationTokenSource?.Dispose();
                    _transcriptionCancellationTokenSource = null;
                }
            }, "Transkriberar inspelning...");
        }

        private void CancelTranscription()
        {
            try
            {
                _transcriptionCancellationTokenSource?.Cancel();
                RecordingStatusText = "Avbryter transkribering...";
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid avbrytning av transkribering: {ex.Message}");
            }
        }

        private async Task ViewTranscriptionDetailsAsync()
        {
            if (CurrentTranscription == null)
                return;

            _navigationService.NavigateTo<TranscriptionDetailsViewModel>(CurrentTranscription.Id);

            await Task.CompletedTask; // For async compatibility
        }

        private void OnRecordingTimeUpdated(object? sender, TimeSpan time)
        {
            // Update the recording time in the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                RecordingTime = $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
            });
        }

        private void OnRecordingStatusChanged(object? sender, bool isRecording)
        {
            // Update the recording status in the UI thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                Serilog.Log.Debug($"OnRecordingStatusChanged called with isRecording={isRecording}");
                IsRecording = isRecording;

                if (!isRecording)
                {
                    IsPaused = false;
                }
            });
        }

        private void ResetState()
        {
            IsRecording = false;
            IsPaused = false;
            IsTranscribing = false;
            CanStartTranscription = false;
            RecordingTime = "00:00:00";
            RecordingTitle = string.Empty;
            RecordingStatusText = Constants.RecordingInstructionsMessage;
            RecordingStatusColor = Constants.StoppedColor;
            CurrentTranscription = null;
            _currentTranscriptionId = string.Empty;

            // Make sure commands update their state
            PauseResumeRecordingCommand.NotifyCanExecuteChanged();
            StopRecordingCommand.NotifyCanExecuteChanged();
            StartRecordingCommand.NotifyCanExecuteChanged();
            StartTranscriptionCommand.NotifyCanExecuteChanged();
            CancelCommand.NotifyCanExecuteChanged();
        }

        public override void Dispose()
        {
            // Unsubscribe from events
            _audioService.RecordingTimeUpdated -= OnRecordingTimeUpdated;
            _audioService.RecordingStatusChanged -= OnRecordingStatusChanged;

            // Cancel ongoing transcriptions
            _transcriptionCancellationTokenSource?.Cancel();
            _transcriptionCancellationTokenSource?.Dispose();

            base.Dispose();
        }
    }
}