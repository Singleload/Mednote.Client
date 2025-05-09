using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mednote.Client.Services.Interfaces;
using Mednote.Client.Utils;
using System;
using System.Windows;

namespace Mednote.Client.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly ISettingsService _settingsService;
        private readonly IApiService _apiService;
        private readonly IEventAggregator _eventAggregator;

        private object? _currentView;
        private string _applicationTitle = $"{Utils.Constants.AppName} v{Utils.Constants.AppVersion}";
        private bool _isApiConnected;

        public RelayCommand NavigateToRecordingCommand { get; }
        public RelayCommand NavigateToHistoryCommand { get; }
        public RelayCommand NavigateToSettingsCommand { get; }
        public RelayCommand CheckApiConnectionCommand { get; }
        public RelayCommand<Window> CloseWindowCommand { get; }

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string ApplicationTitle
        {
            get => _applicationTitle;
            set => SetProperty(ref _applicationTitle, value);
        }

        public bool IsApiConnected
        {
            get => _isApiConnected;
            set => SetProperty(ref _isApiConnected, value);
        }

        public Type? CurrentViewModelType => _navigationService.CurrentViewModel;

        public MainViewModel(
            INavigationService navigationService,
            ISettingsService settingsService,
            IApiService apiService,
            IEventAggregator eventAggregator)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Initialize commands with proper syntax for CommunityToolkit.Mvvm
            NavigateToRecordingCommand = new RelayCommand(NavigateToRecording);
            NavigateToHistoryCommand = new RelayCommand(NavigateToHistory);
            NavigateToSettingsCommand = new RelayCommand(NavigateToSettings);
            CheckApiConnectionCommand = new RelayCommand(CheckApiConnectionAsync);
            CloseWindowCommand = new RelayCommand<Window>(CloseWindow);

            // Subscribe to navigation events
            _navigationService.CurrentViewModelChanged += OnCurrentViewModelChanged;

            // Initial API check
            CheckApiConnectionAsync();

            // Navigate to initial view
            NavigateToRecording();
        }

        private void NavigateToRecording()
        {
            _navigationService.NavigateTo<RecordingViewModel>();
        }

        private void NavigateToHistory()
        {
            _navigationService.NavigateTo<HistoryViewModel>();
        }

        private void NavigateToSettings()
        {
            _navigationService.NavigateTo<SettingsViewModel>();
        }

        private void OnCurrentViewModelChanged(object? sender, Type? viewModelType)
        {
            if (viewModelType != null)
            {
                var viewModel = App.Services.GetService(viewModelType);
                CurrentView = viewModel;
                OnPropertyChanged(nameof(CurrentViewModelType));
            }
        }

        private void CheckApiConnectionAsync()
        {
            try
            {
                // Start as a task without awaiting (fire and forget)
                _ = CheckApiConnectionAsyncImpl();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error checking API connection");
                IsApiConnected = false;
            }
        }

        private async System.Threading.Tasks.Task CheckApiConnectionAsyncImpl()
        {
            IsApiConnected = await _apiService.CheckApiAvailabilityAsync();
        }

        private void CloseWindow(Window? window)
        {
            if (window != null)
            {
                window.Close();
            }
        }

        public override void Dispose()
        {
            _navigationService.CurrentViewModelChanged -= OnCurrentViewModelChanged;
            base.Dispose();
        }
    }
}