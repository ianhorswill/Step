using System.Diagnostics.Tracing;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using StepRepl.ViewModels;

namespace StepRepl.Views
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();
        }

        private LogViewModel LogViewModel => ((LogViewModel)DataContext);

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            LogViewModel.Clear();
        }
    }
}
