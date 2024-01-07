namespace Step.Interpreter
{
    /// <summary>
    /// Represents a warning occurring in the source of a Step program.
    /// </summary>
    public class WarningInfo
    {
        internal WarningInfo(object? offender, string warning)
        {
            Offender = offender;
            Warning = warning;
        }

        /// <summary>
        /// The task or method that generated this warning.  This may be a CompoundTask, Method, or MethodPlaceholder
        /// </summary>
        public object? Offender { get; private set; }

        /// <summary>
        /// Text describing the problem
        /// </summary>
        public string Warning{ get; private set; }

        public static implicit operator WarningInfo((object?, string) info) => new WarningInfo(info.Item1, info.Item2);
    }
}
