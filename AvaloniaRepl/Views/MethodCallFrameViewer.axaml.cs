using Avalonia.Controls;
using Avalonia.Input;
using ReactiveUI;

namespace AvaloniaRepl.Views
{
    public partial class MethodCallFrameViewer : Window
    {
        public MethodCallFrameViewer()
        {
            InitializeComponent();
        }

        private void KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
