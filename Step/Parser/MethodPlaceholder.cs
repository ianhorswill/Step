namespace Step.Parser
{
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