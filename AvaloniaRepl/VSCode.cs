using System;
using System.Diagnostics;
using System.IO;

namespace AvaloniaRepl
{
    /// <summary>
    /// Interface for invoking Visual Studio Code editor from within the Repl.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class VSCode
    {
        private static readonly string? editorPath;

        static VSCode()
        {
            // We need to search for the VS Code installation because, at least on Windows 11, the file called "code" in the search path
            // isn't the actual executable, it's a shell script.  Somehow if you run "code" from the windows shell, it finds it, but
            // it doesn't work from Process.Start unless you set UseShell, in which case it's both slower and also pops up a console window.
            editorPath = "code";
            foreach (var dir in Environment.GetEnvironmentVariable("PATH")!.Split(';'))
            {
                var p = Path.Combine(dir, "Code.exe");
                if (File.Exists(p))
                {
                    editorPath = p;
                    return;
                }

                p = Path.Combine(Path.GetDirectoryName(dir)!, "Code.exe");
                if (File.Exists(p))
                {
                    editorPath = p;
                    return;
                }
            }
        }

        /// <summary>
        /// Invoke the editor on the specified folder.
        /// </summary>
        public static void EditFolder(string path)
        {
            LaunchEditor("-r", path);
        }

        /// <summary>
        /// Invoke the editor and bring it to the specified line of the specified file.
        /// </summary>
        public static void Edit(string path, int lineNumber)
        {
            LaunchEditor("-r", "-g", $"{path}:{lineNumber}");
        }

        private static void LaunchEditor(params string[] args)
        {
            if (editorPath == null)
                // VSC not installed
                return;
            Process.Start(new ProcessStartInfo(editorPath, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            });
        }
    }
}
