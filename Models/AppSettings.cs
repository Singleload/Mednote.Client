using System.ComponentModel;
using System.IO;

namespace Mednote.Client.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private string _selectedInputDeviceId = string.Empty;
        private string _selectedOutputDeviceId = string.Empty;
        private string _storageDirectory = Path.Combine(
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Mednote"),
            "Recordings");
        private bool _autoStartTranscription = true;
        private bool _saveRawAudio = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SelectedInputDeviceId
        {
            get => _selectedInputDeviceId;
            set
            {
                if (_selectedInputDeviceId != value)
                {
                    _selectedInputDeviceId = value;
                    OnPropertyChanged(nameof(SelectedInputDeviceId));
                }
            }
        }

        public string SelectedOutputDeviceId
        {
            get => _selectedOutputDeviceId;
            set
            {
                if (_selectedOutputDeviceId != value)
                {
                    _selectedOutputDeviceId = value;
                    OnPropertyChanged(nameof(SelectedOutputDeviceId));
                }
            }
        }

        public string StorageDirectory
        {
            get => _storageDirectory;
            set
            {
                if (_storageDirectory != value)
                {
                    _storageDirectory = value;
                    OnPropertyChanged(nameof(StorageDirectory));
                }
            }
        }

        public bool AutoStartTranscription
        {
            get => _autoStartTranscription;
            set
            {
                if (_autoStartTranscription != value)
                {
                    _autoStartTranscription = value;
                    OnPropertyChanged(nameof(AutoStartTranscription));
                }
            }
        }

        public bool SaveRawAudio
        {
            get => _saveRawAudio;
            set
            {
                if (_saveRawAudio != value)
                {
                    _saveRawAudio = value;
                    OnPropertyChanged(nameof(SaveRawAudio));
                }
            }
        }
    }
}