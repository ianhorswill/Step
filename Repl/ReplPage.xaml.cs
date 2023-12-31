using CommunityToolkit.Maui.Storage;
using Step;

namespace Repl
{
    public partial class ReplPage : ContentPage
    {
        public static ReplPage Instance = null!;

        private MenuBarItem? projectCommandMenu = null;

        private static Color TextOutputColor = Colors.White;
        private static Color WarningColor = Colors.Orange;

        public ReplPage()
        {
            InitializeComponent();
            Instance = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ShowWarningsAndException();
        }

        private void ShowWarningsAndException()
        {
            var warnings = StepCode.Module.Warnings().ToArray();
            if (warnings.Length > 0)
            {
                OutputText.Text = string.Join('\n', warnings);
                WarningColor = Colors.Yellow;
                OutputText.TextColor = WarningColor;
            }
            else
            {
                OutputText.Text = "";
                OutputText.TextColor = TextOutputColor;
            }

            UpdateExceptionInfo();
        }

        private void UpdateExceptionInfo()
        {
            if (StepCode.LastException != null)
            {
                ErrorLabel.IsVisible = true;
                Module.RichTextStackTraces = true;
                ExceptionMessage.Text = StepCode.LastException.Message;
                StackTrace.Text = Module.StackTrace();
                CStackTrace.Text = "Internal debugging information for Ian:\n"+StepCode.LastException.StackTrace;
            }
            else
            {
                ExceptionMessage.Text = StackTrace.Text = CStackTrace.Text = "";
                ErrorLabel.IsVisible = false;
            }
        }

        private async void EvalAndShowOutput(object sender, EventArgs e)
        {
            await EvalAndShowOutput(Command.Text);
        }

        Task EvalAndShowOutput(string command) => EvalAndShowOutput(StepCode.Eval(command));

        async Task EvalAndShowOutput(Task<string> evalTask)
        {
            OutputText.Text = await evalTask;
            OutputText.TextColor = TextOutputColor;
            UpdateExceptionInfo();
        }

        private void ReloadStepCode(object sender, EventArgs e)
        {
            StepCode.ReloadStepCode();
            StackTrace.Text = CStackTrace.Text = "";
            ShowWarningsAndException();
        }

        private async void CopyOutput(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(OutputText.Text);
        }

        private async void CopyError(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync($"{ExceptionMessage.Text}\n{StackTrace.Text}");
        }

        private async void SelectProject(object sender, EventArgs e)
        {
            var chosen = await FolderPicker.PickAsync(StepCode.ProjectDirectory);
            if (chosen.IsSuccessful && Directory.Exists(chosen.Folder.Path))
            {
                StepCode.ProjectDirectory = chosen.Folder.Path;
                StepCode.ReloadStepCode();
                ShowWarningsAndException();
            }
        }

        public void RemoveProjectMenu()
        {
            if (projectCommandMenu != null)
                Instance.MenuBarItems.Remove(projectCommandMenu);
        }

        private void EnsureProjectMenu()
        {
            if (projectCommandMenu == null)
            {
                projectCommandMenu = new MenuBarItem() { Text = StepCode.ProjectName };
                Instance.MenuBarItems.Add(projectCommandMenu);
            }
        }

        public void AddButton(string stringLabel, object[] code)
        {
            EnsureProjectMenu();
            var item = new MenuFlyoutItem() { Text = stringLabel };
#pragma warning disable CS4014
            item.Clicked += (sender, args) => EvalAndShowOutput(StepCode.Eval(new StepThread(StepCode.Module, StepCode.State, "Call", new object[] { code })));
#pragma warning restore CS4014
            projectCommandMenu!.Add(item);
        }
    }
}