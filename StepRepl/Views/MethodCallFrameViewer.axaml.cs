using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

namespace StepRepl.Views
{
    public partial class MethodCallFrameViewer : Window
    {
        public MethodCallFrameViewer()
        {
            InitializeComponent();
        }

        private void KeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
