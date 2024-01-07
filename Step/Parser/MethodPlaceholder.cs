namespace Step.Parser
{
    /// <summary>
    /// This is used to record information about what method a warning from the parser is about because the method
    /// doesn't actually exist until the parser exits.
    /// </summary>
    public class MethodPlaceholder
    {
        public string TaskName { get; private set; }
        public string? SourcePath { get; private set; }
        public int LineNumber { get; private set; }

        public MethodPlaceholder(string taskName, string? sourcePath, int lineNumber)
        {
            TaskName = taskName;
            SourcePath = sourcePath;
            LineNumber = lineNumber;
        }
    }
}