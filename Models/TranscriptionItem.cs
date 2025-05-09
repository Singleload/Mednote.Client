using System;
using System.ComponentModel;

namespace Mednote.Client.Models
{
    public class TranscriptionItem : INotifyPropertyChanged
    {
        private string _id = Guid.NewGuid().ToString();
        private DateTime _createdAt = DateTime.Now;
        private string _title = string.Empty;
        private string _filePath = string.Empty;
        private string _transcriptionText = string.Empty;
        private string _processedText = string.Empty;
        private TimeSpan _duration;
        private bool _isProcessing;
        private bool _isCompleted;
        private string _patientId = string.Empty;
        private string _notes = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set
            {
                if (_createdAt != value)
                {
                    _createdAt = value;
                    OnPropertyChanged(nameof(CreatedAt));
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged(nameof(Title));
                }
            }
        }

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged(nameof(FilePath));
                }
            }
        }

        public string TranscriptionText
        {
            get => _transcriptionText;
            set
            {
                if (_transcriptionText != value)
                {
                    _transcriptionText = value;
                    OnPropertyChanged(nameof(TranscriptionText));
                }
            }
        }

        public string ProcessedText
        {
            get => _processedText;
            set
            {
                if (_processedText != value)
                {
                    _processedText = value;
                    OnPropertyChanged(nameof(ProcessedText));
                }
            }
        }

        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(DurationString));
                }
            }
        }

        public string DurationString => $"{(int)Duration.TotalHours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (_isProcessing != value)
                {
                    _isProcessing = value;
                    OnPropertyChanged(nameof(IsProcessing));
                }
            }
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged(nameof(IsCompleted));
                }
            }
        }

        public string PatientId
        {
            get => _patientId;
            set
            {
                if (_patientId != value)
                {
                    _patientId = value;
                    OnPropertyChanged(nameof(PatientId));
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged(nameof(Notes));
                }
            }
        }
    }
}