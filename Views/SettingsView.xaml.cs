using Mednote.Client.ViewModels;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Mednote.Client.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SettingsViewModel viewModel)
            {
                viewModel.LoadSettingsCommand.ExecuteAsync(null);
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Open URL in default browser
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true
                });

                e.Handled = true;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Kunde inte öppna URL: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}