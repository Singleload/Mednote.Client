using Mednote.Client.Models;
using Mednote.Client.Services.Interfaces;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Implementation
{
    public class AudioService : IAudioService
    {
        private readonly ISettingsService _settingsService;
        private WaveInEvent? _waveIn;
        private WaveFileWriter? _waveWriter;
        private WaveOutEvent? _playbackDevice;
        private string _currentRecordingPath = string.Empty;
        private readonly Timer _recordingTimer = new(1000); // Update every second
        private DateTime _recordingStartTime;
        private TimeSpan _pausedDuration = TimeSpan.Zero;
        private DateTime? _pauseStartTime;
        private bool _disposedValue;

        public bool IsRecording { get; private set; }
        public bool IsPaused { get; private set; }
        public TimeSpan CurrentRecordingTime { get; private set; }

        public event EventHandler<TimeSpan>? RecordingTimeUpdated;
        public event EventHandler<bool>? RecordingStatusChanged;

        public AudioService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _recordingTimer.Elapsed += OnRecordingTimerElapsed;
        }

        private void OnRecordingTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (!IsRecording || IsPaused)
                return;

            UpdateRecordingTime();
        }

        private void UpdateRecordingTime()
        {
            if (_pauseStartTime.HasValue)
            {
                // We're paused, don't update the time
                return;
            }

            var elapsed = DateTime.Now - _recordingStartTime;
            CurrentRecordingTime = elapsed - _pausedDuration;
            RecordingTimeUpdated?.Invoke(this, CurrentRecordingTime);
        }

        public List<AudioDeviceInfo> GetInputDevices()
        {
            var devices = new List<AudioDeviceInfo>();

            for (int i = 0; i < WaveInEvent.DeviceCount; i++)
            {
                var capabilities = WaveInEvent.GetCapabilities(i);

                devices.Add(new AudioDeviceInfo
                {
                    DeviceId = i.ToString(),
                    Name = capabilities.ProductName,
                    IsDefault = i == 0,
                    IsInput = true
                });
            }

            return devices;
        }

        public List<AudioDeviceInfo> GetOutputDevices()
        {
            var devices = new List<AudioDeviceInfo>();

            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var capabilities = WaveOut.GetCapabilities(i);

                devices.Add(new AudioDeviceInfo
                {
                    DeviceId = i.ToString(),
                    Name = capabilities.ProductName,
                    IsDefault = i == 0,
                    IsInput = false
                });
            }

            return devices;
        }

        public async Task StartRecordingAsync(string deviceId)
        {
            if (IsRecording)
                return;

            // Ensure storage directory exists
            if (!_settingsService.EnsureStorageDirectoryExists())
            {
                throw new DirectoryNotFoundException("Kunde inte hitta eller skapa lagringskatalogen.");
            }

            var settings = _settingsService.GetSettings();

            // Create a temporary file path for recording
            _currentRecordingPath = Path.Combine(
                settings.StorageDirectory,
                $"recording_{DateTime.Now:yyyyMMdd_HHmmss}.wav"
            );

            try
            {
                // Setup wave in device
                _waveIn = new WaveInEvent
                {
                    DeviceNumber = int.Parse(deviceId),
                    WaveFormat = new WaveFormat(44100, 16, 2) // CD quality, stereo
                };

                // Setup wave file writer
                _waveWriter = new WaveFileWriter(_currentRecordingPath, _waveIn.WaveFormat);

                // Subscribe to data available event
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.RecordingStopped += OnRecordingStopped;

                // Start recording
                _waveIn.StartRecording();

                // Record start time and reset paused duration
                _recordingStartTime = DateTime.Now;
                _pausedDuration = TimeSpan.Zero;
                _pauseStartTime = null;

                // Start timer
                _recordingTimer.Start();

                // Update status
                IsRecording = true;
                IsPaused = false;

                // Explicitly raise the event to notify listeners
                Serilog.Log.Debug($"AudioService: Raising RecordingStatusChanged event with IsRecording={IsRecording}");
                RecordingStatusChanged?.Invoke(this, IsRecording);

                // Logging
                Serilog.Log.Information($"Started recording to {_currentRecordingPath}");

                await Task.CompletedTask; // Make method async compatible
            }
            catch (Exception ex)
            {
                // Clean up on error
                _waveIn?.Dispose();
                _waveIn = null;
                _waveWriter?.Dispose();
                _waveWriter = null;

                // Log and rethrow
                Serilog.Log.Error(ex, "Error starting recording");
                throw;
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (_waveWriter == null)
                return;

            try
            {
                // Only write data if not paused
                if (!IsPaused)
                {
                    _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
                    _waveWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error writing audio data");
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            // Check for any errors during recording
            if (e.Exception != null)
            {
                Serilog.Log.Error(e.Exception, "Recording stopped due to an error");
            }

            CloseWaveWriter();

            // Reset state if recording stopped due to error
            if (e.Exception != null)
            {
                IsRecording = false;
                IsPaused = false;

                // Make sure to notify listeners
                Serilog.Log.Debug("AudioService: Recording stopped due to error, raising RecordingStatusChanged event");
                RecordingStatusChanged?.Invoke(this, IsRecording);
            }
        }

        public void PauseRecording()
        {
            if (!IsRecording || IsPaused)
                return;

            IsPaused = true;
            _pauseStartTime = DateTime.Now;

            // Notify listeners
            Serilog.Log.Debug("AudioService: Recording paused, raising RecordingStatusChanged event");
            RecordingStatusChanged?.Invoke(this, IsRecording);

            Serilog.Log.Information("Recording paused");
        }

        public void ResumeRecording()
        {
            if (!IsRecording || !IsPaused)
                return;

            if (_pauseStartTime.HasValue)
            {
                _pausedDuration += DateTime.Now - _pauseStartTime.Value;
                _pauseStartTime = null;
            }

            IsPaused = false;

            // Notify listeners
            Serilog.Log.Debug("AudioService: Recording resumed, raising RecordingStatusChanged event");
            RecordingStatusChanged?.Invoke(this, IsRecording);

            Serilog.Log.Information("Recording resumed");
        }

        public async Task<string> StopAndSaveRecordingAsync()
        {
            if (!IsRecording)
                return string.Empty;

            // Stop recording
            StopRecording();

            // Return the path to the saved file
            return await Task.FromResult(_currentRecordingPath);
        }

        private void StopRecording()
        {
            try
            {
                // Stop the recording timer
                _recordingTimer.Stop();

                // Stop the recording device
                _waveIn?.StopRecording();

                // Update status
                IsRecording = false;
                IsPaused = false;

                // Notify listeners with the updated state
                Serilog.Log.Debug("AudioService: Recording stopped, raising RecordingStatusChanged event");
                RecordingStatusChanged?.Invoke(this, IsRecording);

                // Log
                Serilog.Log.Information($"Recording stopped. File saved to {_currentRecordingPath}");

                // Final time update
                UpdateRecordingTime();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error stopping recording");
                throw;
            }
            finally
            {
                // Close and cleanup
                CloseWaveWriter();

                _waveIn?.Dispose();
                _waveIn = null;
            }
        }

        private void CloseWaveWriter()
        {
            if (_waveWriter == null)
                return;

            try
            {
                _waveWriter.Flush();
                _waveWriter.Dispose();
                _waveWriter = null;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error closing wave writer");
            }
        }

        public async Task<string> ConvertToMonoAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Original ljudfil kunde inte hittas", filePath);

            var monoFilePath = Path.Combine(
                Path.GetDirectoryName(filePath) ?? string.Empty,
                Path.GetFileNameWithoutExtension(filePath) + "_mono.wav"
            );

            try
            {
                // Convert to mono using NAudio - fixed conversion issue
                using (var reader = new AudioFileReader(filePath))
                {
                    // Create a mono version by averaging left and right channels
                    var mono = new StereoToMonoSampleProvider(reader)
                    {
                        LeftVolume = 0.5f,
                        RightVolume = 0.5f
                    };

                    // Create a wave format for mono PCM
                    var monoFormat = new WaveFormat(reader.WaveFormat.SampleRate, 16, 1);

                    // Create a WaveProvider with the mono format
                    using (var writer = new WaveFileWriter(monoFilePath, monoFormat))
                    {
                        // Create a buffer for samples
                        var buffer = new float[reader.WaveFormat.SampleRate * 4]; // 1 second buffer
                        int samplesRead;

                        // Read and write samples in chunks
                        while ((samplesRead = mono.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // Convert float samples to bytes and write to file
                            for (int i = 0; i < samplesRead; i++)
                            {
                                short sample = (short)(buffer[i] * short.MaxValue);
                                writer.WriteSample(sample);
                            }
                        }
                    }
                }

                Serilog.Log.Information($"Converted audio to mono: {monoFilePath}");

                return monoFilePath;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error converting audio to mono");
                throw;
            }
        }

        public async Task PlayAudioAsync(string filePath, string deviceId)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Ljudfil kunde inte hittas", filePath);

            try
            {
                StopPlayback();

                // Initialize playback device
                _playbackDevice = new WaveOutEvent
                {
                    DeviceNumber = int.Parse(deviceId)
                };

                // Open audio file
                var audioFile = new AudioFileReader(filePath);

                // Setup playback
                _playbackDevice.Init(audioFile);
                _playbackDevice.Play();

                // Log
                Serilog.Log.Information($"Playing audio file: {filePath}");

                await Task.CompletedTask; // Make method async compatible
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error playing audio file");
                throw;
            }
        }

        public void StopPlayback()
        {
            if (_playbackDevice != null)
            {
                try
                {
                    if (_playbackDevice.PlaybackState != PlaybackState.Stopped)
                    {
                        _playbackDevice.Stop();
                    }

                    _playbackDevice.Dispose();
                    _playbackDevice = null;

                    Serilog.Log.Information("Audio playback stopped");
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error stopping audio playback");
                }
            }
        }

        public TimeSpan GetAudioDuration(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Ljudfil kunde inte hittas", filePath);

            try
            {
                using var reader = new AudioFileReader(filePath);
                return reader.TotalTime;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, $"Error getting audio duration for file: {filePath}");
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    StopRecording();
                    StopPlayback();

                    _recordingTimer.Elapsed -= OnRecordingTimerElapsed;
                    _recordingTimer.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}