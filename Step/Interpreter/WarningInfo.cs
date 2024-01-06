using System;
using System.Collections.Generic;
using System.Text;

namespace Step.Interpreter
{
    public class WarningInfo
    {
        public WarningInfo(object? offender, string warning)
        {
            Offender = offender;
            Warning = warning;
        }

        public object? Offender { get; private set; }
        public string Warning{ get; private set; }

        public static implicit operator WarningInfo((object?, string) info) => new WarningInfo(info.Item1, info.Item2);
    }
}
