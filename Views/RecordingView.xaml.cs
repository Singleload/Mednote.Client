using Mednote.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Mednote.Client.Views
{
    public partial class RecordingView : UserControl
    {
        public RecordingView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RecordingViewModel viewModel)
            {
                // Check API connectivity on load
                _ = viewModel.CheckApiConnectionAsync();
            }
        }
    }
}