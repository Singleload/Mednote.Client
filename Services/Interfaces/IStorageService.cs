using Mednote.Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Interfaces
{
    public interface IStorageService
    {
        /// <summary>
        /// Gets all transcription items from storage
        /// </summary>
        /// <returns>List of all transcription items</returns>
        Task<IEnumerable<TranscriptionItem>> GetAllTranscriptionsAsync();

        /// <summary>
        /// Gets a transcription item by ID
        /// </summary>
        /// <param name="id">ID of the transcription to get</param>
        /// <returns>Transcription item or null if not found</returns>
        Task<TranscriptionItem?> GetTranscriptionByIdAsync(string id);

        /// <summary>
        /// Saves a transcription item to storage
        /// </summary>
        /// <param name="transcription">Transcription item to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveTranscriptionAsync(TranscriptionItem transcription);

        /// <summary>
        /// Deletes a transcription item from storage
        /// </summary>
        /// <param name="id">ID of the transcription to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteTranscriptionAsync(string id);

        /// <summary>
        /// Deletes an audio file
        /// </summary>
        /// <param name="filePath">Path to the audio file to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteAudioFileAsync(string filePath);

        /// <summary>
        /// Gets all audio files in the storage directory
        /// </summary>
        /// <returns>List of audio file paths</returns>
        Task<IEnumerable<string>> GetAllAudioFilesAsync();
    }
}