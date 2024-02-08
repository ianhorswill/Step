using System.Text.RegularExpressions;
using System.Windows.Input;
using CommunityToolkit.Maui.Storage;
using Step;
using Step.Interpreter;
using Task = System.Threading.Tasks.Task;

namespace Repl
{
    /// <summary>
    /// MAUI page providing user with a Read/Eval/Print Loop (REPL)
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class ReplPage : ContentPage
    {
#pragma warning disable CA2211
        /// <summary>
        /// The (presumably single) instance of the page
        /// </summary>
        public static ReplPage Instance = null!;
#pragma warning restore CA2211

        public ReplPage()
        {
            InitializeComponent();
            Instance = this;
            ExceptionMessage.GestureRecognizers.Add(new TapGestureRecognizer() { Command = (CommandAdapter)ExceptionMessageClicked});
            StackTrace.BindingContext = this;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            ShowWarningsAndException();
        }

        #region Step code execution and page update
        /// <summary>
        /// This is just the event handler for the user pressing enter/return in the editor on the Repl page.
        /// Run the code typed in by the user on this page.
        /// </summary>
        private async void EvalAndShowOutput(object sender, EventArgs e)
        {
            await EvalAndShowOutput(Command.Text);
        }

        /// <summary>
        /// Parse and run step code.
        /// </summary>
        Task EvalAndShowOutput(string command) => EvalAndShowOutput(StepCode.Eval(command));

        /// <summary>
        /// Run some step code and then update the page with its output and/or exceptions
        /// </summary>
        /// <param name="evalTask">Task that runs the step code and returns its output text</param>
        async Task EvalAndShowOutput(Task<string> evalTask)
        {
            TemporaryControls.Clear();
            // Call code and update text
            OutputText.Text = await evalTask;
            // Update exception info
            UpdateExceptionInfo();
        }

        /// <summary>
        /// Update the error feedback areas of the page
        /// </summary>
        private void ShowWarningsAndException()
        {
            var warnings = StepCode.Module.WarningsWithOffenders().ToArray();
            var haveWarnings = warnings.Length > 0;
            WarningLabel.IsVisible = haveWarnings;
            WarningText.ItemsSource = haveWarnings ? warnings : null;

            UpdateExceptionInfo();
        }
    
        /// <summary>
        /// Stack frames of the running or most recently deceased step call.
        /// </summary>
        public IEnumerable<MethodCallFrame> StackFrames 
            => MethodCallFrame.CurrentFrame == null?Array.Empty<MethodCallFrame>():MethodCallFrame.CurrentFrame.CallerChain;

        /// <summary>
        /// Update the exception and stack on the page
        /// </summary>
        private void UpdateExceptionInfo()
        {
            if (StepCode.LastException != null)
            {
                ErrorLabel.IsVisible = true;
                Module.RichTextStackTraces = true;
                ExceptionMessage.Text = StepCode.LastException.Message;
                StackTrace.ItemsSource = StackFrames;
                
                CStackTrace.Text = "Internal debugging information for Ian:\n"+StepCode.LastException.StackTrace;
            }
            else
            {
                ExceptionMessage.Text = CStackTrace.Text = "";
                StackTrace.ItemsSource = null;
                ErrorLabel.IsVisible = false;
            }
        }
        #endregion

        #region UI commands
        private void ReloadStepCode(object sender, EventArgs e)
        {
            StepCode.ReloadStepCode();
            OutputText.Text = CStackTrace.Text = "";
            StackTrace.ItemsSource = null;
            ShowWarningsAndException();
        }
        private void EditProject(object? sender, EventArgs e)
        {
            if (CanEditProject)
                VSCode.EditFolder(StepCode.ProjectDirectory);
        }

        private async void CopyOutput(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(OutputText.Text);
        }

        private async void CopyError(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync($"{ExceptionMessage.Text}\n{Module.StackTrace()}");
        }

        private async void SelectProject(object sender, EventArgs e)
        {
            #pragma warning disable CA1416
            var chosen = await FolderPicker.PickAsync(StepCode.ProjectDirectory);
            #pragma warning restore CS1416
            if (!chosen.IsSuccessful || !Directory.Exists(chosen.Folder.Path)) return;
            StepCode.ProjectDirectory = chosen.Folder.Path;
            StepCode.ReloadStepCode();
            ShowWarningsAndException();
        }
        #endregion

        #region User menu commands
        /// <summary>
        /// The menu for the project's commands, if any.
        /// </summary>
        private MenuBarItem? projectCommandMenu;

        /// <summary>
        /// Remove project menu, if any, so as to get rid of any stale menu items.
        /// </summary>
        public void RemoveProjectMenu()
        {
            if (projectCommandMenu != null)
                Instance.MenuBarItems.Remove(projectCommandMenu);
        }

        /// <summary>
        /// Create the project menu if it doesn't already exist
        /// </summary>
        private void EnsureProjectMenu()
        {
            if (projectCommandMenu == null)
            {
                projectCommandMenu = new MenuBarItem() { Text = StepCode.ProjectName };
                MenuBarItems.Add(projectCommandMenu);
            }
        }

        /// <summary>
        /// Add an entry to the project menu
        /// </summary>
        /// <param name="stringLabel"></param>
        /// <param name="code"></param>
        public void AddProjectMenuEntry(string stringLabel, object[] code)
        {
            EnsureProjectMenu();
            var item = new MenuFlyoutItem() { Text = stringLabel };
#pragma warning disable CS4014
            item.Clicked += (_, _) => EvalAndShowOutput(StepCode.Eval(new StepThread(StepCode.Module, StepCode.State, "Call", new object[] { code })));
#pragma warning restore CS4014
            projectCommandMenu!.Add(item);
        }
        #endregion

        #region Adding temporary interface widgets under Step program control.
        public void AddButton(string buttonName, object[] action, State state)
        {
            var button = new Button() { Text = buttonName};
#pragma warning disable CS4014
            button.Clicked += (_, _) =>
                EvalAndShowOutput(
                    StepCode.Eval(new StepThread(StepCode.Module, state, "Call", new object[] { action })));
#pragma warning restore CS4014
            TemporaryControls.Add(button);
        }
        #endregion

        #region Editor support
        private bool CanEditProject => StepCode.ProjectDirectory != "";

        /// <summary>
        /// Invoke the editor to edit the line referenced in the exception, if any.
        /// </summary>
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

        /// <summary>
        /// Invoke the editor on the source code for the method being called in the selected stack frame.
        /// </summary>
        private void StackFrameSelected(object? sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is MethodCallFrame { Method.FilePath: not null } frame)
            {
                VSCode.Edit(frame.Method.FilePath, frame.Method.LineNumber);
            }
        }

        /// <summary>
        /// Invoke the editor on the line referred to in the selected warning.
        /// </summary>
        private void WarningSelected(object? sender, SelectedItemChangedEventArgs e)
        {
            var warning = (WarningInfo)e.SelectedItem;
            switch (warning.Offender)
            {
                case Method { FilePath: not null } m:
                    VSCode.Edit(m.FilePath, m.LineNumber);
                    break;

                case CompoundTask { Methods.Count: > 0 } t when t.Methods[0].FilePath != null:
                    var firstMethod = t.Methods[0];
                    VSCode.Edit(firstMethod.FilePath!, firstMethod.LineNumber);
                    break;

                case Step.Parser.MethodPlaceholder { SourcePath: not null } p:
                    VSCode.Edit(p.SourcePath, p.LineNumber);
                    break;
            }
        }
        #endregion

        /// <summary>
        /// This is just a dumb little wrapper for Actions (delegates) that makes them look like ICommands
        /// because that's what MAUI wants for mouse event handlers (TapGestureRecognizers).
        /// </summary>
        private class CommandAdapter : ICommand
        {
            private readonly Action action;

            private CommandAdapter(Action action) => this.action = action;

            public bool CanExecute(object? parameter) => true;
            
            public void Execute(object? parameter) => action();

#pragma warning disable CS1634
#pragma warning disable CS0067
            public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
#pragma warning restore CS1634
            
            public static implicit operator CommandAdapter(Action a) => new CommandAdapter(a);
        }
    }
}