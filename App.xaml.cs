using Mednote.Client.Services.Implementation;
using Mednote.Client.Services.Interfaces;
using Mednote.Client.Utils;
using Mednote.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Mednote.Client
{
    public partial class App : Application
    {
        // Add the required modifier to indicate this property must be initialized
        public static IServiceProvider? Services { get; private set; }

        public App()
        {
            // Configure culture to Swedish
            Thread.CurrentThread.CurrentCulture = new CultureInfo("sv-SE");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("sv-SE");

            // Configure Serilog
            ConfigureLogging();

            // Set up exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                // Configure services
                ConfigureServices();

                // Ensure Services is not null
                if (Services == null)
                {
                    throw new InvalidOperationException("Services provider failed to initialize");
                }

                // Create and show the main window
                var mainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };

                mainWindow.Show();

                Log.Information("Application started successfully");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error during application startup");
                MessageBox.Show(
                    $"Ett oväntat fel inträffade vid uppstart av programmet: {ex.Message}",
                    "Startfel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Current.Shutdown();
            }
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register singleton services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IEventAggregator, EventAggregator>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Register HTTP client
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10); // Set timeout for large audio files
            });

            // Register other services
            services.AddTransient<IAudioService, AudioService>();
            services.AddTransient<IStorageService, StorageService>();
            services.AddTransient<ITranscriptionService, TranscriptionService>();

            // Register view models
            services.AddTransient<MainViewModel>();
            services.AddTransient<RecordingViewModel>();
            services.AddTransient<HistoryViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<TranscriptionDetailsViewModel>();

            // Build the service provider
            Services = services.BuildServiceProvider();
        }

        // Rest of the implementation stays the same
        private void ConfigureLogging()
        {
            // Get the log file path
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Mednote",
                "Logs");

            // Ensure the directory exists
            Directory.CreateDirectory(logDirectory);

            var logFilePath = Path.Combine(logDirectory, "mednote-.log");

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    logFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("Logging configured");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            // Clean up
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Log.Fatal(ex, "Unhandled exception");

            if (e.IsTerminating)
            {
                MessageBox.Show(
                    $"Ett allvarligt fel har inträffat och programmet måste avslutas: {ex?.Message}",
                    "Allvarligt fel",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Unhandled exception in UI thread");

            MessageBox.Show(
                $"Ett oväntat fel inträffade: {e.Exception.Message}",
                "Fel",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}