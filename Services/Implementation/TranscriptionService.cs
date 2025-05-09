using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Implementation
{
    public class TranscriptionService : ITranscriptionService
    {
        private readonly IStorageService _storageService;
        private readonly IAudioService _audioService;
        private readonly IApiService _apiService;

        public TranscriptionService(
            IStorageService storageService,
            IAudioService audioService,
            IApiService apiService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
        }

        public async Task<IEnumerable<TranscriptionItem>> GetAllTranscriptionsAsync()
        {
            return await _storageService.GetAllTranscriptionsAsync();
        }

        public async Task<TranscriptionItem?> GetTranscriptionByIdAsync(string id)
        {
            return await _storageService.GetTranscriptionByIdAsync(id);
        }

        public async Task SaveTranscriptionAsync(TranscriptionItem transcription)
        {
            if (transcription == null)
                throw new ArgumentNullException(nameof(transcription));

            await _storageService.SaveTranscriptionAsync(transcription);
        }

        public async Task<bool> DeleteTranscriptionAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            return await _storageService.DeleteTranscriptionAsync(id);
        }

        public async Task<TranscriptionItem> CreateTranscriptionItemAsync(string filePath, TimeSpan duration)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("Filsökväg kan inte vara tom", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException("Ljudfil hittades inte", filePath);

            // Create a new transcription item
            var transcription = new TranscriptionItem
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.Now,
                FilePath = filePath,
                Duration = duration,
                Title = $"Inspelning {DateTime.Now:yyyy-MM-dd HH:mm}"
            };

            // Save to storage
            await _storageService.SaveTranscriptionAsync(transcription);

            Serilog.Log.Information($"Created new transcription: {transcription.Id}");

            return transcription;
        }

        public async Task<TranscriptionItem> ProcessRecordingAsync(string transcriptionId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(transcriptionId))
                throw new ArgumentException("Transkriberings-ID kan inte vara tomt", nameof(transcriptionId));

            // Get the transcription item
            var transcription = await _storageService.GetTranscriptionByIdAsync(transcriptionId);
            if (transcription == null)
                throw new InvalidOperationException($"Transkribering med ID {transcriptionId} hittades inte");

            try
            {
                // Update status
                transcription.IsProcessing = true;
                await _storageService.SaveTranscriptionAsync(transcription);

                // Convert to mono (Google requires mono audio)
                Serilog.Log.Information($"Converting audio to mono: {transcription.FilePath}");
                var monoFilePath = await _audioService.ConvertToMonoAsync(transcription.FilePath);

                // Send to Google Speech-to-Text for transcription
                Serilog.Log.Information("Sending audio to Google Speech-to-Text");
                var transcriptionResult = await _apiService.TranscribeAudioAsync(monoFilePath, cancellationToken);

                // Update transcription with raw text
                transcription.TranscriptionText = transcriptionResult.RawText;
                await _storageService.SaveTranscriptionAsync(transcription);

                // Process with ChatGPT
                Serilog.Log.Information("Sending transcription to ChatGPT for processing");
                var processedResult = await _apiService.ProcessTranscriptionAsync(
                    transcription.Id,
                    transcription.TranscriptionText,
                    cancellationToken);

                // Update with processed text
                transcription.ProcessedText = processedResult.ProcessedText;
                transcription.IsProcessing = false;
                transcription.IsCompleted = true;

                // Save final result
                await _storageService.SaveTranscriptionAsync(transcription);

                Serilog.Log.Information($"Completed processing transcription: {transcription.Id}");

                return transcription;
            }
            catch (Exception ex)
            {
                // Mark as not processing on error
                transcription.IsProcessing = false;
                await _storageService.SaveTranscriptionAsync(transcription);

                Serilog.Log.Error(ex, $"Error processing transcription: {transcription.Id}");
                throw;
            }
        }
    }
}