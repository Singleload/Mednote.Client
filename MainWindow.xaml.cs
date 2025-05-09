using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace Mednote.Client
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
            catch (Exception ex)
            {
                MessageBox.Show($"Kunde inte öppna URL: {ex.Message}", "Fel", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}