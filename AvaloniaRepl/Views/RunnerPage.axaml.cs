using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using AvaloniaRepl.GraphVisualization;
using AvaloniaRepl.ViewModels;
using Step;
using Step.Interpreter;
using Task = System.Threading.Tasks.Task;

namespace AvaloniaRepl.Views;

public partial class RunnerPage : UserControl
{
    public List<StepButton> StepButtons = new();
    
    public RunnerPage()
    {
        InitializeComponent();
        ShowWarningsAndException();
        StepCommandField.AttachedToVisualTree += (s, e) => StepCommandField.Focus();
    }
    
    private RunnerViewModel ViewModel => (RunnerViewModel)DataContext;

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
    
    private void StartDebugButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (StepCode.CurrentStepThread == null) return;

        // get the active debugger tab (if we have one)
        var debugger = MainWindow.Instance.FindTabByContentType<DebuggerPage>();
        if (debugger == null)
        {
            debugger = new DebuggerPage();
            MainWindow.Instance.AddTab("Debugger", debugger);
        }

        debugger.SetThreadForDebugger(StepCode.CurrentStepThread);
    }
    
    private void Quit(object? sender, RoutedEventArgs e)
    {
        MainWindow.Instance.Close();
    }
    
    private async void StepCommandField_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Return || e.Key != Key.Enter) return;
        if (sender is not TextBox textBox) return;

        string command = textBox.Text;
        textBox.Text = "";
        if (string.IsNullOrEmpty(command)) return;

        ViewModel.AddCommandHistory(command);
        await EvalAndShowOutput(command);
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
    }

    private void OpenRecentProject(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem) return;
        
        string? path = (string?)menuItem.Header;
        if (Directory.Exists(path))
        {
            OpenProject(path);
        }
    }

    public void RegisterNewButton(StepButton btn)
    {
        StepButtons.Add(btn);
        ButtonPanelItems.Items.Add(btn);
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
    
    #endregion
    
    #region Editor support
    private bool CanEditProject => StepCode.ProjectDirectory != "";

    /// <summary>
    /// Invoke the editor to edit the line referenced in the exception, if any.
    /// </summary>
    private void ExceptionMessageClicked(object? obj, PointerReleasedEventArgs e)
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
        var t = (SelectableTextBlock)sender;
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

    Task EvalAndShowOutput(string command) => EvalAndShowOutput(StepCode.Eval(command));

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

    private void StackFrameGotFocus(object? sender, GotFocusEventArgs e)
    {
        var t = (SelectableTextBlock)sender;
        StackTrace.SelectedItem = t.DataContext;
    }
    
    private void ShowStackFrame(object? sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        var frame = (MethodCallFrame)item.DataContext;
        var window = new MethodCallFrameViewer() { DataContext = frame };
        window.Show();
    }
}