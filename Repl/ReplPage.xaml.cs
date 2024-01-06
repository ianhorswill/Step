using System.Text.RegularExpressions;
using System.Windows.Input;
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

        private readonly Dictionary<string,MenuBarItem> TemporaryMenus = new Dictionary<string,MenuBarItem>();

        public ReplPage()
        {
            InitializeComponent();
            Instance = this;
            ExceptionMessage.GestureRecognizers.Add(new TapGestureRecognizer() { Command = (CommandAdapter)ExceptionMessageClicked});
            StackTrace.GestureRecognizers.Add(new TapGestureRecognizer() { Command = (CommandAdapter)StackTraceClicked});
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ShowWarningsAndException();
        }

        private void ShowWarningsAndException()
        {
            var warnings = StepCode.Module.Warnings().ToArray();
            var haveWarnings = warnings.Length > 0;
            WarningLabel.IsVisible = haveWarnings;
            if (haveWarnings)
                WarningText.Text = string.Join("<br>", warnings);
            else
                WarningText.Text = OutputText.Text = "";

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
            // Clear out previous menu items
            foreach (var pair in TemporaryMenus)
                MenuBarItems.Remove(pair.Value);
            TemporaryMenus.Clear();
            TemporaryControls.Clear();
            // Call code and update text
            OutputText.Text = await evalTask;
            OutputText.TextColor = TextOutputColor;
            // Update exception info
            UpdateExceptionInfo();
        }

        private void ReloadStepCode(object sender, EventArgs e)
        {
            StepCode.ReloadStepCode();
            OutputText.Text = StackTrace.Text = CStackTrace.Text = "";
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
                MenuBarItems.Add(projectCommandMenu);
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

        public void AddTemporaryMenuItem(string menuName, string itemName, object[] action, State state)
        {
            if (!TemporaryMenus.TryGetValue(menuName, out var menu))
            {
                menu = new MenuBarItem() { Text = menuName };
                MenuBarItems.Add(menu);
                TemporaryMenus[menuName] = menu;
            }
            var item = new MenuFlyoutItem() { Text = itemName };
#pragma warning disable CS4014
            item.Clicked += (sender, args) => EvalAndShowOutput(StepCode.Eval(new StepThread(StepCode.Module, state, "Call", new object[] { action })));
#pragma warning restore CS4014
            menu.Add(item);
        }

        public void AddButton(string buttonName, object[] action, State state)
        {
            var button = new Button() { Text = buttonName};
#pragma warning disable CS4014
            button.Clicked += (sender, args) => EvalAndShowOutput(StepCode.Eval(new StepThread(StepCode.Module, state, "Call", new object[] { action })));
#pragma warning restore CS4014
            TemporaryControls.Add(button);
        }

        private void EditProject(object? sender, EventArgs e)
        {
            VSCode.EditFolder(StepCode.ProjectDirectory);
        }

        private bool CanEditProject => StepCode.ProjectDirectory != null;

        private void ExceptionMessageClicked()
        {
            var m = Regex.Match(ExceptionMessage.Text, "^([^.]+.step):([0-9]+) ");
            if (m.Success)
            {
                var file = m.Groups[1].Value;
                var lineNumber = int.Parse(m.Groups[2].Value);
                VSCode.Edit(Path.Combine(StepCode.ProjectDirectory, file), lineNumber);
            }
        }

        private void StackTraceClicked()
        {
            var m = Regex.Match(StackTrace.Text, "<i>at ([^.]+.step):([0-9]+)");
            if (m.Success)
            {
                var file = m.Groups[1].Value;
                var lineNumber = int.Parse(m.Groups[2].Value);
                VSCode.Edit(Path.Combine(StepCode.ProjectDirectory, file), lineNumber);
            }
        }

        private class CommandAdapter : ICommand
        {
            private readonly Action action;

            public CommandAdapter(Action action)
            {
                this.action = action;
            }

            public bool CanExecute(object? parameter) => true;
            
            public void Execute(object? parameter) => action();

            public event EventHandler? CanExecuteChanged;

            public static implicit operator CommandAdapter(Action a) => new CommandAdapter(a);
        }
    }
}