using System;

namespace Mednote.Client.Models
{
    public class AudioDeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public bool IsInput { get; set; }

        public override string ToString()
        {
            return IsDefault ? $"{Name} (Standard)" : Name;
        }
    }
}