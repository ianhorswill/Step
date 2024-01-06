using System.Diagnostics;

namespace Repl
{
    public static class VSCode
    {
        public static void EditFolder(string path)
        {
            LaunchEditor("-r", path);
        }

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
