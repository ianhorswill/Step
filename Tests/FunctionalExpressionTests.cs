using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;
using Step.Interpreter;
using static Step.Interpreter.FunctionalExpressionParser;

namespace Tests
{
    [TestClass]
    public class FunctionalExpressionTests
    {
        private static readonly BindingEnvironment EmptyEnvironment = new BindingEnvironment(Module.Global, null);

        private static FunctionalExpression Constant(object c) => new Constant(c);
        [TestMethod]
        public void ConstantTest()
        {
            Assert.AreEqual(1, Constant(1).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void VariableTest()
        {
            Assert.AreEqual(Module.Global["Write"], new VariableReference(StateVariableName.Named("Write")).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void NegateTest()
        {
            Assert.AreEqual(-1, Parse("-", 1).Eval(EmptyEnvironment));
            Assert.AreEqual(-1f, Parse("-", 1f).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void AddTest()
        {
            Assert.AreEqual(3, Parse(1, "+", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(3f, Parse(1f, "+", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(3f, Parse(1, "+", 2f).Eval(EmptyEnvironment));
            Assert.AreEqual(3f, Parse(1f, "+", 2f).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void SubtractTest()
        {
            Assert.AreEqual(-1, Parse(1, "-", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(-1f, Parse(1f, "-", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(-1f, Parse(1, "-", 2f).Eval(EmptyEnvironment));
            Assert.AreEqual(-1f, Parse(1f, "-", 2f).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void MultiplyTest()
        {
            Assert.AreEqual(2, Parse(1, "*", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(2f, Parse(1f, "*", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(2f, Parse(1, "*", 2f).Eval(EmptyEnvironment));
            Assert.AreEqual(2f, Parse(1f, "*", 2f).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void DivideTest()
        {
            Assert.AreEqual(2, Parse(4, "/", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(0.5f, Parse(1, "/", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(0.5f, Parse(1f, "/", 2).Eval(EmptyEnvironment));
            Assert.AreEqual(0.5f, Parse(1, "/", 2f).Eval(EmptyEnvironment));
            Assert.AreEqual(0.5f, Parse(1f, "/", 2f).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void PrecedenceTest()
        {
            Assert.AreEqual(14, Parse(1, "*", 2, "+", 3, "*", 4).Eval(EmptyEnvironment));
        }

        [TestMethod]
        public void ParenTest()
        {
            Assert.AreEqual(20, Parse("(", 1, "*", 2, "+", 3, ")", "*", 4).Eval(EmptyEnvironment));
            Assert.AreEqual(24, Parse(1, "*","(", 3, "+", 3, ")", "*", 4).Eval(EmptyEnvironment));
            Assert.AreEqual(15, Parse(1, "*", "(", 3, "+", 3, "*", 4, ")").Eval(EmptyEnvironment));
        }
    }
}
