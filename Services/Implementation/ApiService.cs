using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Implementation
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsService _settingsService;

        public ApiService(HttpClient httpClient, ISettingsService settingsService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

            // Configure the HTTP client
            _httpClient.BaseAddress = new Uri(ApiSettings.BaseApiUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", ApiSettings.ApiKey);
            _httpClient.Timeout = TimeSpan.FromMinutes(10); // Long timeout for large audio files
        }

        public async Task<TranscriptionResult> TranscribeAudioAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Ljudfil kunde inte hittas", filePath);

            try
            {
                // Create multipart form content
                using var formContent = new MultipartFormDataContent();

                // Add the audio file
                var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath, cancellationToken));
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
                formContent.Add(fileContent, "audio", Path.GetFileName(filePath));

                // Make the request
                var response = await _httpClient.PostAsync(ApiSettings.TranscribeEndpoint, formContent, cancellationToken);

                // Ensure success
                response.EnsureSuccessStatusCode();

                // Parse the response
                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<TranscriptionResult>(jsonContent)
                    ?? throw new InvalidOperationException("Kunde inte tolka svaret från servern");

                // Log success
                Serilog.Log.Information($"Transcribed audio file: {filePath}, Response length: {result.RawText.Length} characters");

                return result;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Serilog.Log.Error(ex, "Timeout vid transkribering av ljudfil");
                throw new TimeoutException("Tidsfristen för transkriberingen överskreds. Ljudfilen kan vara för stor.", ex);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error transcribing audio file: {filePath}");
                throw;
            }
        }

        public async Task<TranscriptionResult> ProcessTranscriptionAsync(string transcriptionId, string text, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create request content
                var requestContent = new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        Id = transcriptionId,
                        Text = text
                    }),
                    Encoding.UTF8,
                    "application/json"
                );

                // Make the request
                var response = await _httpClient.PostAsync(ApiSettings.ProcessEndpoint, requestContent, cancellationToken);

                // Ensure success
                response.EnsureSuccessStatusCode();

                // Parse the response
                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonConvert.DeserializeObject<TranscriptionResult>(jsonContent)
                    ?? throw new InvalidOperationException("Kunde inte tolka svaret från servern");

                // Log success
                Serilog.Log.Information($"Processed transcription ID: {transcriptionId}, Response length: {result.ProcessedText.Length} characters");

                return result;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Serilog.Log.Error(ex, "Timeout vid bearbetning av transkribering");
                throw new TimeoutException("Tidsfristen för bearbetningen överskreds.", ex);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error processing transcription ID: {transcriptionId}");
                throw;
            }
        }

        public async Task<bool> CheckApiAvailabilityAsync()
        {
            try
            {
                // Add timeout to avoid hanging UI
                var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                // Add proper error handling and logging
                Serilog.Log.Debug($"Checking API availability at {ApiSettings.BaseApiUrl}");

                // Make the request with timeout
                var response = await _httpClient.GetAsync(ApiSettings.HealthCheckEndpoint, timeoutCts.Token);

                bool isAvailable = response.IsSuccessStatusCode;
                Serilog.Log.Information($"API health check result: {isAvailable}, Status code: {response.StatusCode}");

                return isAvailable;
            }
            catch (OperationCanceledException)
            {
                Serilog.Log.Warning("API health check timed out after 5 seconds");
                return false;
            }
            catch (HttpRequestException ex)
            {
                Serilog.Log.Error(ex, "Network error checking API availability");
                return false;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Unexpected error checking API availability");
                return false;
            }
        }
    }
}