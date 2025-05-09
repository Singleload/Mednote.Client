using System;

namespace Mednote.Client.Models
{
    public class TranscriptionResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RawText { get; set; } = string.Empty;
        public string ProcessedText { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.Now;
        public double Confidence { get; set; }
        public TimeSpan ProcessingTime { get; set; }
    }
}