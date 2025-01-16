using Microsoft.VisualStudio.TestTools.UnitTesting;
using Step;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Step.Interpreter;

namespace Step.Tests
{
    [TestClass()]
    public class PairTests
    {
        [TestMethod()]
        public void FromIListTest()
        {
            var p1 = (Pair)Pair.List(1, 2, 3);
            Assert.AreEqual(1, p1.First);
            var p2 = (Pair)p1.Rest;
            Assert.AreEqual(2, p2.First);
            var p3 = (Pair)p2.Rest;
            Assert.AreEqual(3, p3.First);
            Assert.IsTrue(((IList)p3.Rest).Count == 0);
        }

        [TestMethod()]
        public void LengthTest()
        {
            // Easy case: proper list with no variables
            var list = (Pair)Pair.List(1, 2, 3);
            Assert.AreEqual(3, list.LengthProperOrImproper(null));
            Assert.AreEqual(-1, new Pair(1, new LogicVariable("foo", 0)).LengthProperOrImproper(null));
            var v = new LogicVariable("v", 0);
            var extended = new Pair(0, v);
            Assert.AreEqual(4, extended.LengthProperOrImproper(new BindingList(v, list, null)));
        }

        [TestMethod]
        public void UnifyPairPair()
        {
            var e = new BindingEnvironment();
            var target = new Pair(1,Pair.Empty);
            target.AssertCanonicalEmptyList();

            Assert.IsTrue(e.Unify(new Pair(1,Pair.Empty), target, e.Unifications, out var nu));
            Assert.IsNull(nu);

            var h = new LogicVariable("?h", 0);
            var t = new LogicVariable("?t", 1);
            Assert.AreEqual(Pair.Empty, target.Rest);
            Assert.IsTrue(e.Unify(new Pair(h,t), target, e.Unifications, out nu));
            target.AssertCanonicalEmptyList();
            Assert.AreEqual(t, nu.Variable);
            Assert.IsTrue(ReferenceEquals(target.Rest, nu.Value));
            Assert.IsTrue(ReferenceEquals(Pair.Empty, nu.Value));
            nu = nu.Next;
            Assert.AreEqual(h, nu.Variable);
            Assert.AreEqual(1, nu.Value);
        }

        [TestMethod]
        public void MemberTest()
        {
            var m = Module.FromDefinitions(
                "[predicate] Mem ?e [?e | ?].",
                "Mem ?e [? | ?tail]: [Mem ?e ?tail]");
            Assert.IsTrue(m.CallPredicate("Mem", 1, new object[] { 1, 2, 3 }));
            Assert.IsTrue(m.CallPredicate("Mem", 2, new object[] { 1, 2, 3 }));
            Assert.IsTrue(m.CallPredicate("Mem", 3, new object[] { 1, 2, 3 }));
            Assert.IsFalse(m.CallPredicate("Mem", 4, new object[] { 1, 2, 3}));
        }

        [TestMethod]
        public void AppendTest()
        {
            var m = Module.FromDefinitions(
                "Append [] ?x ?x.",
                "Append [?h | ?t] ?l [?h | ?lt]: [Append ?t ?l ?lt]",
                "Test: [Append [1 2 3] [4 5 6] ?x] [Write ?x]",
                "Test2: [Append ?x [4 5 6] [1 2 3 4 5 6]] [Write ?x]",
                "Test3: [Append [1 2 3] ?x [1 2 3 4 5 6]] [Write ?x]"
            );
            Assert.AreEqual("[1 2 3 4 5 6]", m.Call("Test"));
            Assert.AreEqual("[1 2 3]", m.Call("Test2"));
            Assert.AreEqual("[4 5 6]", m.Call("Test3"));
        }
    }
}