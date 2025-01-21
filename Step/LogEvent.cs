using Step.Interpreter;
using Step.Output;

namespace Step
{
    public class LogEvent(object?[] payload, MethodCallFrame callFrame, BindingEnvironment env)
    {
        public readonly object?[] Payload = payload;
        public readonly MethodCallFrame CallFrame = callFrame;
        public readonly BindingEnvironment Environment = env;

        public string Text => Writer.TermToString(Payload, Environment.Unifications);

        public delegate void Listener(LogEvent e);

        public static event Listener? EventLogged;

        public static void Log(object?[] payload, MethodCallFrame callFrame, BindingEnvironment env) 
            => EventLogged?.Invoke(new LogEvent(payload, callFrame, env));
    }
}
