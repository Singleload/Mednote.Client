using Mednote.Client.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Interfaces
{
    public interface ITranscriptionService
    {
        /// <summary>
        /// Gets all transcription items
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
        /// Saves a transcription item
        /// </summary>
        /// <param name="transcription">Transcription item to save</param>
        /// <returns>Task representing the async operation</returns>
        Task SaveTranscriptionAsync(TranscriptionItem transcription);

        /// <summary>
        /// Deletes a transcription item and its associated files
        /// </summary>
        /// <param name="id">ID of the transcription to delete</param>
        /// <returns>True if deleted, false otherwise</returns>
        Task<bool> DeleteTranscriptionAsync(string id);

        /// <summary>
        /// Creates a new transcription item for a recording
        /// </summary>
        /// <param name="filePath">Path to the recording file</param>
        /// <param name="duration">Duration of the recording</param>
        /// <returns>New transcription item</returns>
        Task<TranscriptionItem> CreateTranscriptionItemAsync(string filePath, System.TimeSpan duration);

        /// <summary>
        /// Processes a recording by transcribing and then sending to ChatGPT
        /// </summary>
        /// <param name="transcriptionId">ID of the transcription to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Updated transcription item with results</returns>
        Task<TranscriptionItem> ProcessRecordingAsync(string transcriptionId, CancellationToken cancellationToken = default);
    }
}