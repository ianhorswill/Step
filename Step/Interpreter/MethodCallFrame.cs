using System;
using System.IO;

namespace Step.Interpreter
{
    internal class MethodCallFrame
    {
        public static MethodCallFrame CurrentFrame;

        public Method Method;
        public BindingList<LogicVariable> BindingsAtCallTime;
        public LogicVariable[] Locals;
        public MethodCallFrame Parent;

        public MethodCallFrame(Method method, BindingList<LogicVariable> bindings, LogicVariable[] locals, MethodCallFrame parent)
        {
            Method = method;
            BindingsAtCallTime = bindings;
            Locals = locals;
            Parent = parent;
        }

        public object[] Arglist
        {
            get
            {
                var arglist = new Object[Method.ArgumentPattern.Length];
                for (var i = 0; i < arglist.Length; i++)
                {
                    var formal = Method.ArgumentPattern[i];
                    switch (formal)
                    {
                        case LocalVariableName v:
                            var logicVariable = Locals[v.Index];
                            arglist[i] = BindingList<LogicVariable>.Lookup(BindingsAtCallTime, logicVariable, logicVariable);
                            break;

                        default:
                            arglist[i] = formal;
                            break;
                    }
                }

                return arglist;
            }
        }

        public string CallSourceText
        {
            get
            {
                var source = Call.CallSourceText(Method.Task, Arglist);
                if (Method.FilePath == null)
                    return source;
                return $"{source} (at {Path.GetFileName(Method.FilePath)}:{Method.LineNumber})";
            }
        }
    }
}
