using Step.Interpreter;
using Task = System.Threading.Tasks.Task;

namespace Step.Tests
{
    [TestClass()]
    public class StepThreadTests
    {
        [TestMethod()]
        public void Constructor()
        {
            var m = Module.FromDefinitions("Test: [Write success]");
            using var t = new StepThread(m, State.Empty, "Test");
            Assert.IsFalse(t.IsCompleted);
            StepThread.Current = null; // Allow other tests to run
        }

        [TestMethod()]
        public async Task StartAndAwait()
        {
            var m = Module.FromDefinitions("Test: [Write success]");
            for (var i = 0; i < 10; i++)
            {
                using var t = new StepThread(m, State.Empty, "Test");
                var result = await t.Start();
                Assert.IsTrue(t.IsCompleted);
                Assert.AreEqual("Success", result.text);
            }
        }

        [TestMethod, ExpectedException(typeof(UndefinedVariableException))]
        public async Task ExceptionTest()
        {
            await new StepThread(Module.Global, State.Empty, "TestBlork").Start();
        }

        [TestMethod,ExpectedException(typeof(StepTaskTimeoutException))]
        public async Task AbortTest()
        {
            var m = Module.FromDefinitions("Test: [Sub] [KillMeNow] [Sub] [Write success] [Sub]", "Sub.");

            using var t = new StepThread(m, State.Empty, "Test");
            m["KillMeNow"] = new SimplePredicate("KillMeNow", () =>
            {
                t.Abort();
                return true;
            }); ;
            var result = await t.Start();
            Assert.IsTrue(t.IsCompleted);
        }

        [TestMethod()]
        public async Task DebuggerStartAndAwait()
        {
            var m = Module.FromDefinitions("Test: [Write success]");
            using var t = new StepThread(m, State.Empty, "Test");
            t.Debugger.Start();
            var result = await t.Debugger;
            Assert.IsTrue(t.IsCompleted);
            Assert.AreEqual("Success", result.Text);
        }

        [TestMethod()]
        public async Task SingleStep()
        {
            var m = Module.FromDefinitions("Test: [Sub] [Sub] [Write success]", "Sub.");
            using var t = new StepThread(m, State.Empty, "Test");
            var trace = await ExecutionTrace(t);
            CollectionAssert.AreEqual(new (Module.MethodTraceEvent, string)[]
            {
                (Module.MethodTraceEvent.Enter, "Test"),
                (Module.MethodTraceEvent.Enter, "Sub"),
                (Module.MethodTraceEvent.Succeed, "Sub"),
                (Module.MethodTraceEvent.Enter, "Sub"),
                (Module.MethodTraceEvent.Succeed, "Sub"),
                (Module.MethodTraceEvent.Succeed, "Test"),
            },
                trace);
        }

        [TestMethod()]
        public async Task NonDebuggerBurnInTest()
        {
            var m = Module.FromDefinitions("Test: [Sub] [Sub] [Write success]",
                "Sub.",
                "Die: [KillMeNow][Sub]");
            for (int i = 0; i < 100; i++)
            {
                using var t1 = new StepThread(m, State.Empty, "Test");
                await t1.Start();
                Console.WriteLine("t2");

                var gotException = false;
                using var t2 = new StepThread(m, State.Empty, "TestBlork");
                try
                {
                    await t2.Start();
                }
                catch (UndefinedVariableException)
                {
                    gotException = true;
                }
                Assert.IsTrue(gotException);
                Console.WriteLine("t3");


                using var t3 = new StepThread(m, State.Empty, "Die");
                m["KillMeNow"] = new SimplePredicate("KillMeNow", () =>
                {
                    t3.Abort();
                    return true;
                });
                gotException = false;
                try
                {
                    await t3.Start();
                }
                catch (StepTaskTimeoutException)
                {
                    gotException = true;
                }
                Assert.IsTrue(gotException);
                Console.WriteLine("t4");
                
            }
        }

        [Ignore("This is very slow")]
        [TestMethod()]
        public void DebuggerBurnIn()
        {
            var m = Module.FromDefinitions("Test: [Sub] [Sub] [Write success]",
                "Sub.",
                "Die: [KillMeNow][Sub]");
            for (var i = 0; i < 100; i++)
            {
                var t = DebuggerBurnInCycle(m);
                if (!t.Wait(5000))
                {
                    Console.WriteLine($"Task: {t}, status={t.Status}");
                    Console.WriteLine($"StepThread: {StepThread.Current?.ToString()??"null"}");
                    Console.WriteLine(StepThread.Current?.DebugState);
                    if (lastExecutionTraceTask != null)
                        Console.WriteLine($"Last execution trace task: {lastExecutionTraceTask}, status = {lastExecutionTraceTask.Status}");
                    throw new TimeoutException($"Task hung at cycle {i}");
                }
            }
        }

        private Task<List<(Module.MethodTraceEvent, string)>> lastExecutionTraceTask;

        public async Task DebuggerBurnInCycle(Module m)
        {

            {
                var t = new StepThread(m, State.Empty, "Test");
                lastExecutionTraceTask = ExecutionTrace(t);
                var trace = await lastExecutionTraceTask;
                lastExecutionTraceTask = null;
                CollectionAssert.AreEqual(new[]
                    {
                        (Module.MethodTraceEvent.Enter, "Test"),
                        (Module.MethodTraceEvent.Enter, "Sub"),
                        (Module.MethodTraceEvent.Succeed, "Sub"),
                        (Module.MethodTraceEvent.Enter, "Sub"),
                        (Module.MethodTraceEvent.Succeed, "Sub"),
                        (Module.MethodTraceEvent.Succeed, "Test"),
                    },
                    trace);
                t.Dispose();
            }
            //Console.WriteLine("t1");

            {
                using var t2 = new StepThread(m, State.Empty, "Test");
                await t2.Start();
            }
            //Console.WriteLine("t2");

            var gotException = false;
            {
                using var t3 = new StepThread(m, State.Empty, "TestBlork");
                try
                {
                    await t3.Start();
                }
                catch (UndefinedVariableException)
                {
                    gotException = true;
                }

                Assert.IsTrue(gotException);
            }
            //Console.WriteLine("t3");


            {
                using var t4 = new StepThread(m, State.Empty, "Die");
                m["KillMeNow"] = new SimplePredicate("KillMeNow", () =>
                {
                    t4.Abort();
                    return true;
                });
                gotException = false;
                try
                {
                    await t4.Start();
                }
                catch (StepTaskTimeoutException)
                {
                    gotException = true;
                }

                Assert.IsTrue(gotException);
            }
            //Console.WriteLine("t4");

        }

        async Task<List<(Module.MethodTraceEvent, string)>> ExecutionTrace(StepThread t)
        {
            t.Debugger.SingleStep = true;
            t.Debugger.Start();
            var trace = new List<(Module.MethodTraceEvent, string)>();
            var result = await t.Debugger;
            trace.Add((result.TraceEvent,result.CalledMethod.Task.Name));
            while (true)
            {
                result = await t.Debugger.Continue();
                if (t.IsCompleted)
                    return trace;
                trace.Add((result.TraceEvent,result.CalledMethod.Task.Name));
            }
        }
    }
}