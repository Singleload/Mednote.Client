namespace Mednote.Client.Models
{
    public class ApiSettings
    {
        // The API key for our internal API (hardcoded for simplicity as requested)
        public const string ApiKey = "MedNote-WPF-Client-Access-Key-2024";

        // Base API URL
        public const string BaseApiUrl = "https://api.mednote.se/v1";

        // API endpoints
        public const string TranscribeEndpoint = "/transcribe";
        public const string ProcessEndpoint = "/process";
        public const string HealthCheckEndpoint = "/health";
    }
}