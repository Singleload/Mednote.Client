using Mednote.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Mednote.Client.Views
{
    public partial class HistoryView : UserControl
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is HistoryViewModel viewModel)
            {
                viewModel.LoadTranscriptionsCommand.ExecuteAsync(null);
            }
        }
    }
}