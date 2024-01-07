using System.Diagnostics;

namespace Repl
{
    /// <summary>
    /// Interface for invoking Visual Studio Code editor from within the Repl.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class VSCode
    {
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
            Process.Start(new ProcessStartInfo("code", args) { UseShellExecute = true, CreateNoWindow = true });
        }
    }
}
