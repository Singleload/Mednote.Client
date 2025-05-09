using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace Mednote.Client.ViewModels
{
    public abstract class BaseViewModel : ObservableObject, IDisposable
    {
        private bool _isBusy;
        private string _statusMessage = string.Empty;
        private bool _hasError;
        private string _errorMessage = string.Empty;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Add a RelayCommand for clearing errors
        public RelayCommand ClearErrorCommand { get; }

        protected BaseViewModel()
        {
            // Initialize the command
            ClearErrorCommand = new RelayCommand(ClearError);
        }

        protected async Task ExecuteWithBusyIndicatorAsync(Func<Task> action, string busyMessage = "")
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;
                StatusMessage = busyMessage;

                await action();
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Ett fel inträffade: {ex.Message}";
                // Log the exception
                Serilog.Log.Error(ex, "En oväntad exception inträffade");
            }
            finally
            {
                IsBusy = false;
                StatusMessage = string.Empty;
            }
        }

        protected virtual void ShowError(string message)
        {
            HasError = true;
            ErrorMessage = message;
            Serilog.Log.Error(message);
        }

        protected virtual void ClearError()
        {
            HasError = false;
            ErrorMessage = string.Empty;
        }

        public virtual void Dispose()
        {
            // Base disposal logic
            GC.SuppressFinalize(this);
        }
    }
}