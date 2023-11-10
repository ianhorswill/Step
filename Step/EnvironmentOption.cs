using System;

namespace Step
{
    /// <summary>
    /// Hook used to allow the surrounding environment (i.e. repl, game, server code) respond to
    /// environment-specific requests.  If a request is irrelevant to a given kind of environment,
    /// the environment should ignore it rather than throw an exception.
    /// </summary>
    public static class EnvironmentOption
    {
        /// <summary>
        /// Handler(s) for options for the current environment.
        /// A given handler should ignore any option calls it doesn't know how to handle
        /// and only throw an exception on a truly invalid call.
        /// </summary>
        public static event Action<string, object?[]>? Handler;

        /// <summary>
        /// Call the surrounding environment code to handle the specified option
        /// </summary>
        public static void Handle(string optionName, params object?[] options)
        {
            if (options.Length == 1 && options[0] is int limit)
                switch (optionName)
                {
                    case "searchLimit":
                        Module.SearchLimit = limit;
                        break;
                    case "defaultSearchLimit":
                        Module.SearchLimit = Module.DefaultSearchLimit = limit;
                        break;
                }

            if (Handler != null)
                Handler(optionName, options);
        }
    }
}
