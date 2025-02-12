#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using StepRepl.GraphVisualization;
using StepRepl.ViewModels;
using Step;
using Step.Interpreter;
using Task = System.Threading.Tasks.Task;

namespace StepRepl.Views;

public partial class RunnerPage : UserControl
{
    public static RunnerPage Singleton=null!;
    
    public RunnerPage()
    {
        Singleton = this;
        InitializeComponent();
        
        StepCode.ReloadStepCode();
        ShowWarningsAndException();
        StepCommandField.AttachedToVisualTree += (s, e) => StepCommandField.Focus();
    }
    
    private RunnerViewModel ViewModel => (RunnerViewModel)DataContext!;

    #region Page Controls
    
    private void ReloadStepCode(object? sender, RoutedEventArgs e)
    {
        StepCode.ReloadStepCode();
        ShowWarningsAndException();
        OutputText.Text = $"Reloaded project {StepCode.ProjectName}!\n";
    }
    
    private void EditProject(object? sender, RoutedEventArgs e)
    {
        if (Directory.Exists(StepCode.ProjectDirectory))
        {
            VSCode.EditFolder(StepCode.ProjectDirectory);
        }
    }
    
    private void TestGraph_Click(object? sender, RoutedEventArgs e)
    {
        //var graphVisPage = new GraphVisualization();
        //MainWindow.Instance.AddTab($"Graph ({StepCode.ProjectName})", graphVisPage);
        StepGraph.ShowCallGraph();
    }
    
    private void Quit(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.Close();
    }
    
    private async void StepCommandField_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;

        if (e.Key == Key.Up && e.KeyModifiers == KeyModifiers.Control && ViewModel.CommandHistory.Count > 0)
        {
            // User typed Control-Up; select the previous command
            var index = ViewModel.CommandHistory.IndexOf(textBox.Text);
            textBox.Text = ViewModel.CommandHistory[(index+1) % ViewModel.CommandHistory.Count];
            textBox.SelectionStart = textBox.SelectionEnd = textBox.Text!.Length;
            return;
        }

        if (e.Key != Key.Return || e.Key != Key.Enter) return;

        ViewModel.EvalWithDebugging = (e.KeyModifiers == KeyModifiers.Control);

        string command = textBox.Text!;
        textBox.Text = "";
        if (string.IsNullOrEmpty(command))
        {
            if (ViewModel.CommandHistory.Count == 0)
                return;
            command = ViewModel.CommandHistory[0];
        } else 
            ViewModel.AddCommandHistory(command);

        if (StepThread.Current != null && !StepThread.Current.IsCompleted)
        {
            StepThread.Current.Abort();
            System.Threading.Thread.Sleep(100);
        }
        
        LogViewModel.Singleton.Clear();
        await EvalAndShowOutput(command, ViewModel.EvalWithDebugging);
    }
    
    private void SetCommandFieldText(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        if (menuItem.Header is not string cmd) return;
        
        StepCommandField.Text = cmd;
    }
    
    private async void SelectProjectFolder(object? sender, RoutedEventArgs e)
    {
        var chosen = await MainWindow.Instance.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = "Select a project directory",
            SuggestedStartLocation = await MainWindow.Instance.StorageProvider.TryGetFolderFromPathAsync(StepCode.ProjectDirectory)
        });

        if (chosen.Count == 0 || !Directory.Exists(chosen[0].Path.LocalPath)) return;
        string path = chosen[0].Path.LocalPath;
        OpenProject(path);
        ReloadStepCode(sender, e);
    }

    private void OpenRecentProject(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        
        string? path = (string?)menuItem.Header;
        if (Directory.Exists(path))
        {
            OpenProject(path);
            ReloadStepCode(sender ,e);
        }
    }

    public void RegisterNewButton(StepButton btn)
    {
        ViewModel.StepButtons.Add(btn);
        ButtonPanelItems.Items.Add(btn);
    }
    
    private void ToggleEvalWithDebugger(object? sender, RoutedEventArgs e)
    {
        ViewModel.EvalWithDebugging = !ViewModel.EvalWithDebugging;
    }

    private void ToggleAutoReload(object? sender, RoutedEventArgs e)
    {
        ViewModel.AutoReload = !ViewModel.AutoReload;
    }
    
    private async void StepButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not StepButton stepButton) return;
        
        await EvalAndShowOutput(StepCode.Eval(
            new StepThread(StepCode.Module, stepButton.State, "Call", new object[] { stepButton.Action })
        ));
    }

    /// <summary>
    /// Remove all buttons
    /// </summary>
    private void ClearButtons() => ButtonPanelItems.Items.Clear();

    public void Clear()
    {
        ClearButtons();
        UpdateExceptionInfo();
    }
    
    #endregion
    
    #region Editor support
    private bool CanEditProject => StepCode.ProjectDirectory != "";

    /// <summary>
    /// Invoke the editor to edit the line referenced in the exception, if any.
    /// </summary>
    private void ExceptionMessageClicked(object? obj, PointerReleasedEventArgs e)
    {
        if (ExceptionMessage.Text == null)
            return;
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
    private void StackFrameSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (StackTrace.SelectedItem is MethodCallFrame { Method.FilePath: not null } frame)
        {
            VSCode.Edit(frame.Method.FilePath, frame.Method.LineNumber);
        }
    }

    /// <summary>
    /// Invoke the editor on the line referred to in the selected warning.
    /// </summary>
    private void WarningSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (WarningText.SelectedItem is not WarningInfo warning) return;
        
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
    
    
    private void WarningSelectedContext(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        if (menuItem.DataContext is not WarningInfo warning) return;
        
        // This assignment will trigger the WarningSelected event
        WarningText.SelectedItem = warning;
    }

    private void WarningGotFocus(object? sender, GotFocusEventArgs e)
    {
        var t = (SelectableTextBlock)sender!;
        WarningText.SelectedItem = t.DataContext;
    }
    #endregion


    private void OpenProject(string path)
    {
        StepCode.ProjectDirectory = path;
        StepCode.ReloadStepCode();
        ViewModel.AddRecentProjects(path);
        MainWindow.Instance.SetTabDisplayName(this, $"{StepCode.ProjectName}"); 
        ShowWarningsAndException();
    }
    
    #region Step Code Execution

    private void ShowWarningsAndException()
    {
        var warnings = StepCode.Module.WarningsWithOffenders().ToArray();
        var haveWarnings = warnings.Length > 0;
        WarningLabel.IsVisible = haveWarnings;
        //WarningLabel.IsExpanded = haveWarnings;
        WarningText.ItemsSource = haveWarnings ? warnings : null;

        UpdateExceptionInfo();
        StepCommandField.Focus();
    }
    
    public IEnumerable<MethodCallFrame> StackFrames 
        => MethodCallFrame.CurrentFrame == null?Array.Empty<MethodCallFrame>():MethodCallFrame.CurrentFrame.CallerChain;
    
    private void UpdateExceptionInfo()
    {
        if (StepCode.LastException != null)
        {
            ErrorLabel.IsVisible = true;
            Module.RichTextStackTraces = true;
            HtmlTextFormatter.SetFormattedText(ExceptionMessage, StepCode.LastException is TaskCanceledException?"Your stopped the step program":StepCode.LastException.Message);
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

    Task EvalAndShowOutput(string command, bool singleStep = false)
    {
        Task<string> evalTask = (ViewModel.EvalWithDebugging || singleStep) ?
            StepCode.EvalWithDebugger(command, OnDebugPause, singleStep)
            : StepCode.Eval(command);
        return EvalAndShowOutput(evalTask);
    }

    /// <summary>
    /// Run some step code and then update the page with its output and/or exceptions
    /// </summary>
    /// <param name="evalTask">Task that runs the step code and returns its output text</param>
    async Task EvalAndShowOutput(Task<string> evalTask)
    {
        ClearButtons();
        HtmlTextFormatter.SetFormattedText(OutputText, "<i>Running...</i>");
        var output = await evalTask;
        if (string.IsNullOrEmpty(output))
            output = StepCode.LastException==null?"<i>Execution succeeded</i>":"";
        HtmlTextFormatter.SetFormattedText(OutputText, output);

        // Update exception info
        UpdateExceptionInfo();
    }
    #endregion

    private void AbortMenuItemClicked(object? sender, RoutedEventArgs e)
    {
        StepCode.AbortCurrentStepThread();
    }
    
    private void OnDebugPause(ReplDebugger debugger)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var runnerPage = MainWindow.Instance.FindTabByContentType<RunnerPage>();
            runnerPage?.DebuggerPanelControl.SetDebugger(debugger);
        });
    }

    private void StackFrameGotFocus(object? sender, GotFocusEventArgs e)
    {
        var t = (SelectableTextBlock)sender!;
        StackTrace.SelectedItem = t.DataContext;
    }
    
    private void ShowStackFrame(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender!;
        var frame = (MethodCallFrame)item.DataContext!;
        var window = new MethodCallFrameViewer() { DataContext = frame };
        window.Show();
    }

    private void StepCommandField_OnGotFocus(object sender, GotFocusEventArgs e)
    {
        StepCode.ReloadIfNecessary();
    }

    #region Menu manipulation
    private Dictionary<string, MenuItem> UserMenus = new();

    private MenuItem UserMenu(string name)
    {
        if (!UserMenus.TryGetValue(name, out var menu))
        {
            menu = new MenuItem() { Header = name };
            MainMenu.Items.Add(menu);
            UserMenus[name] = menu;
        }

        return menu;
    }

    public void RemoveUserManus()
    {
        foreach (var pair in UserMenus)
            MainMenu.Items.Remove(pair.Value);
        UserMenus.Clear();
    }

    public void AddMenuItem(string menuName, string itemName, object?[] call)
    {
        var item = new MenuItem()
        {
            Header = itemName
        };

        item.Click += async (sender, args) => await EvalAndShowOutput(StepCode.Eval(call));

        UserMenu(menuName).Items.Add(item);
    }
    #endregion
}