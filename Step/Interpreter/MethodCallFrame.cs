using System;
using System.IO;
using System.Linq;

namespace Step.Interpreter
{
    /// <summary>
    /// Reifies a call to a method
    /// Used only so that there's a data structure that can be walked to generate a stack backtrace
    /// NOT THREAD SAFE
    /// </summary>
    public class MethodCallFrame
    {
        /// <summary>
        /// The MethodCallFrame for the most recently called frame
        /// NOT THREAD SAFE
        /// </summary>
        public static MethodCallFrame CurrentFrame { get; internal set; }

        /// <summary>
        /// The method being called
        /// </summary>
        public readonly Method Method;
        
        /// <summary>
        /// The logic variable binding list at the time of the call
        /// </summary>
        public BindingList<LogicVariable> BindingsAtCallTime { get; internal set; }
        
        /// <summary>
        /// The local variables of the environment of the call
        /// </summary>
        public readonly LogicVariable[] Locals;
        
        /// <summary>
        /// Parent frame - this is the frame of the calling method, not the most recently executed task
        /// The two are the same for deterministic languages, but can be different for non-deterministic ones
        /// For example, if A calls B then C and B calls D, then on entry to D, then the method call frame chain
        /// entry to C is just C -> A.
        /// 
        /// However, the real C# execution stack looks like:
        ///    C -> D -> B -> A
        /// Because if C fails, we have to backtrack to D, not to A.  
        /// </summary>
        public readonly MethodCallFrame Parent;

        internal MethodCallFrame(Method method, BindingList<LogicVariable> bindings, LogicVariable[] locals, MethodCallFrame parent)
        {
            Method = method;
            BindingsAtCallTime = bindings;
            Locals = locals;
            Parent = parent;
        }

        /// <summary>
        /// The effective argument list of the call
        /// This has to get reconstructed from the ArgumentPattern of the method,
        /// which is fixed across all calls and contains LocalVariableName objects
        /// in place of the actual LogicVariables they name (since the latter vary
        /// from call to call), and the Locals array, which contains the specific
        /// logicVariables used in this particular call.
        /// </summary>
        public object[] Arglist
        {
            get
            {
                object Resolve(object o)
                {
                    switch (o)
                    {
                        case LocalVariableName n:
                            return BindingEnvironment.Deref(Locals[n.Index], BindingsAtCallTime);

                        case LogicVariable l:
                            return BindingEnvironment.Deref(l, BindingsAtCallTime);

                        case object[] tuple:
                            return tuple.Select(Resolve).ToArray();

                        default:
                            return o;
                    }
                }

                return Method.ArgumentPattern.Select(Resolve).ToArray();
            }
        }
        
        /// <summary>
        /// Regenerates the textual version of the call in this frame
        /// </summary>
        /// <param name="unifications">Binding list currently in effect.  This will generally be whatever the most recent binding list of the interpreter is.</param>
        public string GetCallSourceText(BindingList<LogicVariable> unifications)
        {
            var source = Call.CallSourceText(Method.Task, Arglist, unifications);
            if (Method.FilePath == null)
                return source;
            var start = Module.RichTextStackTraces ? "\n     <i>" : "(";
            var end = Module.RichTextStackTraces ? "</i>" : ")";
            return $"{source} {start}at {Path.GetFileName(Method.FilePath)}:{Method.LineNumber}{end}";
        }
    }
}
