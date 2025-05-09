using System;

namespace Mednote.Client.Utils
{
    public static class Constants
    {
        // Application info
        public const string AppName = "Mednote";
        public const string AppVersion = "1.0.0";
        public const string Copyright = "Mednote all rights reserved";
        public const string Author = "Dennis Enström";
        public const string AuthorUrl = "https://github.com/Singleload";

        // UI constants
        public const double MinimumWindowWidth = 800;
        public const double MinimumWindowHeight = 600;
        public const double PreferredWindowWidth = 1024;
        public const double PreferredWindowHeight = 768;

        // File extensions
        public const string WavExtension = ".wav";
        public const string MonoSuffix = "_mono";

        // Timeouts (in milliseconds)
        public const int ApiTimeoutMs = 600000; // 10 minutes
        public const int RecordingStatusUpdateIntervalMs = 1000; // 1 second

        // UI animation durations
        public const int FadeAnimationDurationMs = 250;
        public const int SlideAnimationDurationMs = 350;

        // Colors
        public const string PrimaryColor = "#2979FF";
        public const string SecondaryColor = "#5C6BC0";
        public const string AccentColor = "#00B8D4";
        public const string SuccessColor = "#00C853";
        public const string WarningColor = "#FFAB00";
        public const string ErrorColor = "#FF5252";
        public const string NeutralColor = "#607D8B";
        public const string LightBackgroundColor = "#F5F5F5";
        public const string DarkBackgroundColor = "#263238";

        // Recording status colors
        public const string RecordingColor = "#E53935";
        public const string PausedColor = "#FFC107";
        public const string StoppedColor = "#4CAF50";

        // Default text values for empty states
        public const string NoTranscriptionsMessage = "Inga transkriptioner tillgängliga";
        public const string RecordingInstructionsMessage = "Tryck på inspelningsknappen för att börja";
        public const string ProcessingMessage = "Bearbetar...";
        public const string TranscribingMessage = "Transkriberar...";
        public const string NoSettingsMessage = "Standardinställningar används";
    }
}