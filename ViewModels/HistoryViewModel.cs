using CommunityToolkit.Mvvm.Input;
using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using Mednote.Client.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Mednote.Client.ViewModels
{
    public class HistoryViewModel : BaseViewModel
    {
        private readonly ITranscriptionService _transcriptionService;
        private readonly INavigationService _navigationService;
        private readonly IEventAggregator _eventAggregator;

        private ObservableCollection<TranscriptionItem> _transcriptions = new();
        private TranscriptionItem? _selectedTranscription;
        private bool _isLoading;
        private string _searchText = string.Empty;
        private bool _hasTranscriptions;

        // Commands
        public AsyncRelayCommand LoadTranscriptionsCommand { get; }
        public AsyncRelayCommand<TranscriptionItem> ViewTranscriptionDetailsCommand { get; }
        public AsyncRelayCommand<TranscriptionItem> DeleteTranscriptionCommand { get; }
        public AsyncRelayCommand RefreshCommand { get; }
        public RelayCommand ClearSearchCommand { get; }

        // Properties
        public ObservableCollection<TranscriptionItem> Transcriptions
        {
            get => _transcriptions;
            set => SetProperty(ref _transcriptions, value);
        }

        public TranscriptionItem? SelectedTranscription
        {
            get => _selectedTranscription;
            set => SetProperty(ref _selectedTranscription, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplySearch();
                }
            }
        }

        public bool HasTranscriptions
        {
            get => _hasTranscriptions;
            set => SetProperty(ref _hasTranscriptions, value);
        }

        public HistoryViewModel(
            ITranscriptionService transcriptionService,
            INavigationService navigationService,
            IEventAggregator eventAggregator)
        {
            _transcriptionService = transcriptionService ?? throw new ArgumentNullException(nameof(transcriptionService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Initialize commands
            LoadTranscriptionsCommand = new AsyncRelayCommand(LoadTranscriptionsAsync);
            ViewTranscriptionDetailsCommand = new AsyncRelayCommand<TranscriptionItem>(ViewTranscriptionDetailsAsync);
            DeleteTranscriptionCommand = new AsyncRelayCommand<TranscriptionItem>(DeleteTranscriptionAsync);
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            ClearSearchCommand = new RelayCommand(ClearSearch);

            // Subscribe to events
            _eventAggregator.Subscribe<TranscriptionCompletedEvent>(OnTranscriptionCompleted);
            _eventAggregator.Subscribe<TranscriptionDeletedEvent>(OnTranscriptionDeleted);
        }

        private async Task LoadTranscriptionsAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                IsLoading = true;

                try
                {
                    var transcriptions = await _transcriptionService.GetAllTranscriptionsAsync();

                    Transcriptions = new ObservableCollection<TranscriptionItem>(
                        transcriptions.OrderByDescending(t => t.CreatedAt)
                    );

                    HasTranscriptions = Transcriptions.Count > 0;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid laddning av transkriptioner: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        private async Task ViewTranscriptionDetailsAsync(TranscriptionItem? transcription)
        {
            if (transcription == null)
                return;

            try
            {
                _navigationService.NavigateTo<TranscriptionDetailsViewModel>(transcription.Id);
                // Add an await to make this truly asynchronous
                await Task.CompletedTask; // This makes it properly async
            }
            catch (Exception ex)
            {
                ShowError($"Fel vid visning av transkriptionsdetaljer: {ex.Message}");
                // Log the error
                await Task.CompletedTask; // This makes it properly async
            }
        }

        private async Task DeleteTranscriptionAsync(TranscriptionItem? transcription)
        {
            if (transcription == null)
                return;

            // Ask for confirmation
            var result = MessageBox.Show(
                $"Är du säker på att du vill ta bort transkriberingen '{transcription.Title}'? Både ljudfilen och transkriberingen kommer att tas bort permanent.",
                "Bekräfta borttagning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                try
                {
                    var success = await _transcriptionService.DeleteTranscriptionAsync(transcription.Id);

                    if (success)
                    {
                        // Remove from the collection
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Transcriptions.Remove(transcription);
                            HasTranscriptions = Transcriptions.Count > 0;
                        });

                        // Publish event
                        _eventAggregator.Publish(new TranscriptionDeletedEvent(transcription.Id));
                    }
                    else
                    {
                        ShowError("Kunde inte ta bort transkriberingen.");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid borttagning av transkribering: {ex.Message}");
                }
            }, "Tar bort transkribering...");
        }

        private async Task RefreshAsync()
        {
            await LoadTranscriptionsAsync();
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private async void OnTranscriptionCompleted(TranscriptionCompletedEvent evt)
        {
            await RefreshAsync();
        }

        private async void OnTranscriptionDeleted(TranscriptionDeletedEvent evt)
        {
            await RefreshAsync();
        }

        private void ApplySearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                // Reset filter
                _ = RefreshAsync();
                return;
            }

            // Apply filter
            _ = LoadFilteredTranscriptionsAsync();
        }

        private async Task LoadFilteredTranscriptionsAsync()
        {
            await ExecuteWithBusyIndicatorAsync(async () =>
            {
                IsLoading = true;

                try
                {
                    var allTranscriptions = await _transcriptionService.GetAllTranscriptionsAsync();

                    var searchTerm = SearchText.ToLower();
                    var filteredTranscriptions = allTranscriptions.Where(t =>
                        t.Title.ToLower().Contains(searchTerm) ||
                        t.TranscriptionText.ToLower().Contains(searchTerm) ||
                        t.ProcessedText.ToLower().Contains(searchTerm) ||
                        t.PatientId.ToLower().Contains(searchTerm) ||
                        t.Notes.ToLower().Contains(searchTerm) ||
                        t.CreatedAt.ToString("yyyy-MM-dd").Contains(searchTerm)
                    ).OrderByDescending(t => t.CreatedAt);

                    Transcriptions = new ObservableCollection<TranscriptionItem>(filteredTranscriptions);
                    HasTranscriptions = Transcriptions.Count > 0;
                }
                catch (Exception ex)
                {
                    ShowError($"Fel vid filtrering av transkriptioner: {ex.Message}");
                }
                finally
                {
                    IsLoading = false;
                }
            });
        }

        public override void Dispose()
        {
            // Unsubscribe from events
            _eventAggregator.Unsubscribe<TranscriptionCompletedEvent>(OnTranscriptionCompleted);
            _eventAggregator.Unsubscribe<TranscriptionDeletedEvent>(OnTranscriptionDeleted);

            base.Dispose();
        }
    }
}