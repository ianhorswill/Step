using System.Text;
using Step.Interpreter;
using Step.Output;

namespace Step
{
    public class LogEvent(object?[] payload, MethodCallFrame callFrame, BindingEnvironment env)
    {
        public readonly object?[] Payload = payload;
        public readonly MethodCallFrame CallFrame = callFrame;
        public readonly BindingEnvironment Environment = env;

        public BindingList? Bindings => env.Unifications;

        public string Text => Writer.TermToString(Payload, Bindings);

        public delegate void Listener(LogEvent e);

        public static event Listener? EventLogged;

        public static void Log(object?[] payload, MethodCallFrame callFrame, BindingEnvironment env) 
            => EventLogged?.Invoke(new LogEvent(payload, callFrame, env));

        public string StackTrace
        {
            get
            {
                var b = new StringBuilder();
                for (var frame = CallFrame; frame != null; frame = frame.Predecessor)
                {
                    b.Append(frame.GetCallSourceText(Module.RichTextStackTraces, Bindings));
                    b.Append('\n');
                }
                return b.ToString();
            }
        }
    }
}
