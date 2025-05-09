using Mednote.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Mednote.Client.Views
{
    public partial class TranscriptionDetailsView : UserControl
    {
        public TranscriptionDetailsView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TranscriptionDetailsViewModel viewModel)
            {
                // Check API connectivity when the view is loaded
                _ = viewModel.CheckApiConnectionAsync();
            }
        }
    }
}