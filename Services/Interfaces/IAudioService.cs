using Mednote.Client.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mednote.Client.Services.Interfaces
{
    public interface IAudioService : IDisposable
    {
        bool IsRecording { get; }
        bool IsPaused { get; }
        TimeSpan CurrentRecordingTime { get; }

        /// <summary>
        /// Gets available input devices (microphones)
        /// </summary>
        /// <returns>List of available input devices</returns>
        List<AudioDeviceInfo> GetInputDevices();

        /// <summary>
        /// Gets available output devices (speakers)
        /// </summary>
        /// <returns>List of available output devices</returns>
        List<AudioDeviceInfo> GetOutputDevices();

        /// <summary>
        /// Starts a new recording
        /// </summary>
        /// <param name="deviceId">Device ID to use for recording</param>
        /// <returns>Task representing the async operation</returns>
        Task StartRecordingAsync(string deviceId);

        /// <summary>
        /// Pauses current recording
        /// </summary>
        void PauseRecording();

        /// <summary>
        /// Resumes a paused recording
        /// </summary>
        void ResumeRecording();

        /// <summary>
        /// Stops and saves the current recording
        /// </summary>
        /// <returns>Path to the saved recording file</returns>
        Task<string> StopAndSaveRecordingAsync();

        /// <summary>
        /// Converts a recorded file to mono format as required by Google Speech-to-Text
        /// </summary>
        /// <param name="filePath">Original recording file path</param>
        /// <returns>Path to the converted mono file</returns>
        Task<string> ConvertToMonoAsync(string filePath);

        /// <summary>
        /// Plays an audio file
        /// </summary>
        /// <param name="filePath">Path to the audio file to play</param>
        /// <param name="deviceId">Device ID to use for playback</param>
        Task PlayAudioAsync(string filePath, string deviceId);

        /// <summary>
        /// Stops playback of audio
        /// </summary>
        void StopPlayback();

        /// <summary>
        /// Gets the duration of an audio file
        /// </summary>
        /// <param name="filePath">Path to the audio file</param>
        /// <returns>Duration of the audio file</returns>
        TimeSpan GetAudioDuration(string filePath);

        /// <summary>
        /// Event raised when recording time updates
        /// </summary>
        event EventHandler<TimeSpan> RecordingTimeUpdated;

        /// <summary>
        /// Event raised when recording status changes
        /// </summary>
        event EventHandler<bool> RecordingStatusChanged;
    }
}