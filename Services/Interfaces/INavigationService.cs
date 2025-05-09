using System;

namespace Mednote.Client.Services.Interfaces
{
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to the specified view model
        /// </summary>
        /// <typeparam name="T">Type of view model to navigate to</typeparam>
        /// <param name="parameter">Optional parameter to pass to the view model</param>
        void NavigateTo<T>(object? parameter = null);

        /// <summary>
        /// Navigates to the view model for the specified type
        /// </summary>
        /// <param name="viewModelType">Type of view model to navigate to</param>
        /// <param name="parameter">Optional parameter to pass to the view model</param>
        void NavigateTo(Type viewModelType, object? parameter = null);

        /// <summary>
        /// Navigates back to the previous view model
        /// </summary>
        void NavigateBack();

        /// <summary>
        /// Event raised when current view model changes
        /// </summary>
        event EventHandler<Type?> CurrentViewModelChanged;

        /// <summary>
        /// Gets the current view model type
        /// </summary>
        Type? CurrentViewModel { get; }
    }
}