using Step.Binding;
using Step.Output;
using Step.Tasks;
using Step.Tasks.Primitives;

namespace Step.Interpreter
{
    public class LambdaExpression(object?[] head, object?[] body)
    {
        public readonly object?[] Head = head;
        public readonly object?[] Body = body;

        private class LambdaTask(object?[] head, object?[] body) : Task("anonymous", head.Length)
        {
            public readonly object?[] Head = head;
            public readonly object?[] Body = body;

            public override bool Call(object?[] arglist, TextBuffer output, BindingEnvironment env,
                MethodCallFrame? predecessor, Continuation k)
            {
                var copy = (object[])env.CopyTermCloningVariables(new object?[] { Head, Body })!;
                return env.UnifyArrays((object[])copy[0], arglist, out BindingList? u) &&
                       HigherOrderBuiltins.And.Call((object[])copy[1], output,
                           new BindingEnvironment(env, u, env.State), predecessor, k);
            }
        }

        public Task Instantiate(BindingEnvironment env)
        {
            return new LambdaTask(env.ResolveList(Head), env.ResolveList(Body));
        }
    }
}
