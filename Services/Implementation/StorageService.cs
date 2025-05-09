using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Implementation
{
    public class StorageService : IStorageService
    {
        private readonly ISettingsService _settingsService;
        private readonly string _transcriptionsFile;

        public StorageService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            var settings = _settingsService.GetSettings();
            var storageDir = settings.StorageDirectory;

            // Ensure directory exists
            _settingsService.EnsureStorageDirectoryExists();

            // Set file path for transcriptions data
            _transcriptionsFile = Path.Combine(storageDir, "transcriptions.json");

            // Create the file if it doesn't exist
            if (!File.Exists(_transcriptionsFile))
            {
                File.WriteAllText(_transcriptionsFile, JsonConvert.SerializeObject(new List<TranscriptionItem>()));
            }
        }

        public async Task<IEnumerable<TranscriptionItem>> GetAllTranscriptionsAsync()
        {
            try
            {
                // Read and parse the JSON file
                var json = await File.ReadAllTextAsync(_transcriptionsFile);
                var transcriptions = JsonConvert.DeserializeObject<List<TranscriptionItem>>(json) ?? new List<TranscriptionItem>();

                // Validate each transcription (check if the files exist)
                foreach (var transcription in transcriptions)
                {
                    // Check if the file exists
                    if (!string.IsNullOrEmpty(transcription.FilePath) && !File.Exists(transcription.FilePath))
                    {
                        Serilog.Log.Warning($"Audio file not found for transcription {transcription.Id}: {transcription.FilePath}");
                        // Mark the file path as missing
                        transcription.FilePath = "MISSING: " + transcription.FilePath;
                    }
                }

                // Return sorted by date (newest first)
                return transcriptions.OrderByDescending(t => t.CreatedAt);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error reading transcriptions");
                return Enumerable.Empty<TranscriptionItem>();
            }
        }

        public async Task<TranscriptionItem?> GetTranscriptionByIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            try
            {
                // Read all transcriptions
                var transcriptions = await GetAllTranscriptionsAsync();

                // Find by ID
                return transcriptions.FirstOrDefault(t => t.Id == id);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error getting transcription by ID: {id}");
                return null;
            }
        }

        public async Task SaveTranscriptionAsync(TranscriptionItem transcription)
        {
            if (transcription == null)
                throw new ArgumentNullException(nameof(transcription));

            try
            {
                // Read all existing transcriptions
                var transcriptions = (await GetAllTranscriptionsAsync()).ToList();

                // Find existing or add new
                var existingIndex = transcriptions.FindIndex(t => t.Id == transcription.Id);
                if (existingIndex >= 0)
                {
                    // Update existing
                    transcriptions[existingIndex] = transcription;
                }
                else
                {
                    // Add new
                    transcriptions.Add(transcription);
                }

                // Save back to file
                await File.WriteAllTextAsync(
                    _transcriptionsFile,
                    JsonConvert.SerializeObject(transcriptions, Formatting.Indented)
                );

                Serilog.Log.Information($"Saved transcription: {transcription.Id}");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error saving transcription: {transcription.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteTranscriptionAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            try
            {
                // Get the transcription first to find the audio file
                var transcription = await GetTranscriptionByIdAsync(id);
                if (transcription == null)
                    return false;

                // List of files to delete
                var filesToDelete = new List<string>();

                // Add original audio file if exists
                if (!string.IsNullOrEmpty(transcription.FilePath) && !transcription.FilePath.StartsWith("MISSING:"))
                {
                    filesToDelete.Add(transcription.FilePath);

                    // Find all related files with the same base name
                    string directory = Path.GetDirectoryName(transcription.FilePath) ?? string.Empty;
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(transcription.FilePath);
                    string searchPattern = filenameWithoutExt + "*";

                    try
                    {
                        // Find all related files
                        var relatedFiles = Directory.GetFiles(directory, searchPattern);
                        foreach (var file in relatedFiles)
                        {
                            // Don't add the original file again
                            if (file != transcription.FilePath)
                            {
                                filesToDelete.Add(file);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, $"Error finding related files for: {transcription.FilePath}");
                    }
                }

                // Try to delete all files
                bool allFilesDeleted = true;
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            Serilog.Log.Information($"Deleted file: {file}");
                        }
                        else
                        {
                            Serilog.Log.Warning($"File not found when trying to delete: {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, $"Error deleting file: {file}");
                        allFilesDeleted = false;
                    }
                }

                // Read all transcriptions
                var transcriptions = (await GetAllTranscriptionsAsync()).ToList();

                // Remove the one with matching ID
                var filteredTranscriptions = transcriptions.Where(t => t.Id != id).ToList();

                // Save back to file
                await File.WriteAllTextAsync(
                    _transcriptionsFile,
                    JsonConvert.SerializeObject(filteredTranscriptions, Formatting.Indented)
                );

                Serilog.Log.Information($"Deleted transcription: {id}. All files deleted: {allFilesDeleted}");

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error deleting transcription: {id}");
                return false;
            }
        }

        public async Task<bool> DeleteAudioFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                // Delete the file using async File operations
                await Task.Run(() => File.Delete(filePath));

                Serilog.Log.Information($"Deleted audio file: {filePath}");

                // Also delete any related files (like _mono versions)
                try
                {
                    string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    string searchPattern = filenameWithoutExt + "*";

                    var relatedFiles = await Task.Run(() => Directory.GetFiles(directory, searchPattern));
                    foreach (var file in relatedFiles)
                    {
                        // Don't try to delete the original file again
                        if (file != filePath && File.Exists(file))
                        {
                            await Task.Run(() => File.Delete(file));
                            Serilog.Log.Information($"Deleted related file: {file}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, $"Error deleting related files for: {filePath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error deleting audio file: {filePath}");
                return false;
            }
        }

        public async Task<IEnumerable<string>> GetAllAudioFilesAsync()
        {
            try
            {
                var settings = _settingsService.GetSettings();
                var storageDir = settings.StorageDirectory;

                // Get all wav files in the directory using Task.Run to make it properly async
                var files = await Task.Run(() => Directory.GetFiles(storageDir, "*.wav"));

                return files;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error getting audio files");
                return Enumerable.Empty<string>();
            }
        }

        // Securely delete all data associated with transcription
        public async Task<bool> SecurelyDeleteAllDataAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            try
            {
                // Get the transcription
                var transcription = await GetTranscriptionByIdAsync(id);
                if (transcription == null)
                    return false;

                // 1. Delete all associated files
                var filesToDelete = new List<string>();

                // Add original audio file
                if (!string.IsNullOrEmpty(transcription.FilePath) && !transcription.FilePath.StartsWith("MISSING:"))
                {
                    filesToDelete.Add(transcription.FilePath);

                    // Get directory
                    string directory = Path.GetDirectoryName(transcription.FilePath) ?? string.Empty;
                    string filenameWithoutExt = Path.GetFileNameWithoutExtension(transcription.FilePath);

                    // Add all related files
                    try
                    {
                        var relatedFiles = Directory.GetFiles(directory, filenameWithoutExt + "*");
                        filesToDelete.AddRange(relatedFiles.Where(f => f != transcription.FilePath));
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, $"Error finding related files for secure deletion: {transcription.FilePath}");
                    }
                }

                // Delete all files securely (overwrite with zeros first)
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            // Get file length
                            long length = new FileInfo(file).Length;

                            // Overwrite with zeros
                            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Write))
                            {
                                byte[] zeros = new byte[4096]; // 4KB buffer
                                long bytesRemaining = length;

                                while (bytesRemaining > 0)
                                {
                                    int bytesToWrite = (int)Math.Min(zeros.Length, bytesRemaining);
                                    stream.Write(zeros, 0, bytesToWrite);
                                    bytesRemaining -= bytesToWrite;
                                }

                                stream.Flush();
                            }

                            // Delete the file
                            File.Delete(file);
                            Serilog.Log.Information($"Securely deleted file: {file}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, $"Error securely deleting file: {file}");
                    }
                }

                // 2. Remove from transcriptions list
                var transcriptions = (await GetAllTranscriptionsAsync()).ToList();
                var filteredTranscriptions = transcriptions.Where(t => t.Id != id).ToList();

                await File.WriteAllTextAsync(
                    _transcriptionsFile,
                    JsonConvert.SerializeObject(filteredTranscriptions, Formatting.Indented)
                );

                Serilog.Log.Information($"Securely deleted all data for transcription: {id}");

                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error securely deleting all data for transcription: {id}");
                return false;
            }
        }
    }
}