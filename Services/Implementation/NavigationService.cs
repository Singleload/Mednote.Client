using Mednote.Client.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Mednote.Client.Services.Implementation
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private Type? _currentViewModel;

        public event EventHandler<Type?>? CurrentViewModelChanged;

        public Type? CurrentViewModel
        {
            get => _currentViewModel;
            private set
            {
                if (_currentViewModel != value)
                {
                    _currentViewModel = value;
                    CurrentViewModelChanged?.Invoke(this, _currentViewModel);
                }
            }
        }

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public void NavigateTo<T>(object? parameter = null)
        {
            NavigateTo(typeof(T), parameter);
        }

        public void NavigateTo(Type viewModelType, object? parameter = null)
        {
            if (viewModelType == null)
                throw new ArgumentNullException(nameof(viewModelType));

            // Resolve the view model from the service provider
            var viewModel = _serviceProvider.GetRequiredService(viewModelType);

            // Initialize view model with parameter if it has a method for it
            if (parameter != null)
            {
                var initMethod = viewModelType.GetMethod("Initialize");
                if (initMethod != null)
                {
                    initMethod.Invoke(viewModel, new[] { parameter });
                }
            }

            // Update current view model
            CurrentViewModel = viewModelType;

            Serilog.Log.Information($"Navigated to {viewModelType.Name}");
        }

        public void NavigateBack()
        {
            // In a real application, you would maintain a navigation stack
            // For simplicity, we'll navigate to the main view
            NavigateTo(typeof(ViewModels.RecordingViewModel));

            Serilog.Log.Information("Navigated back");
        }
    }
}