using Mednote.Client.Models;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Interfaces
{
    public interface ISettingsService
    {
        /// <summary>
        /// Gets the current application settings
        /// </summary>
        /// <returns>Current application settings</returns>
        AppSettings GetSettings();

        /// <summary>
        /// Saves the application settings
        /// </summary>
        /// <param name="settings">Settings to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveSettingsAsync(AppSettings settings);

        /// <summary>
        /// Ensures the storage directory exists and is accessible
        /// </summary>
        /// <returns>True if directory exists or was created, false otherwise</returns>
        bool EnsureStorageDirectoryExists();

        /// <summary>
        /// Clears temporary files
        /// </summary>
        /// <returns>Task representing the async operation</returns>
        Task ClearTemporaryFilesAsync();
    }
}