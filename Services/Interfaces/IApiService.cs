using Mednote.Client.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Interfaces
{
    public interface IApiService
    {
        /// <summary>
        /// Sends audio file to the API for transcription with Google Speech-to-Text
        /// </summary>
        /// <param name="filePath">Path to the audio file</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Transcription result with the raw text from Google</returns>
        Task<TranscriptionResult> TranscribeAudioAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends transcribed text to the API for processing with ChatGPT
        /// </summary>
        /// <param name="transcriptionId">ID of the transcription</param>
        /// <param name="text">Raw transcription text to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with processed text from ChatGPT</returns>
        Task<TranscriptionResult> ProcessTranscriptionAsync(string transcriptionId, string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the API is available
        /// </summary>
        /// <returns>True if API is available, false otherwise</returns>
        Task<bool> CheckApiAvailabilityAsync();
    }
}