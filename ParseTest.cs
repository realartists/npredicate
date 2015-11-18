using NUnit.Framework;
using System;
using System.Linq;

namespace Predicate
{
    [TestFixture ()]
    public class ParseTest
    {
        [Test]
        public void Test() {
            string predicateFormat = "1 = 2";
            PredicateParser parser = new PredicateParser(predicateFormat);
            var predicate = parser.ParsePredicate();
            Assert.IsNotNull(predicate);
        }

        [Test]
        public void TestComparisonOptions() {
            string predicateFormat = "'Hello World' =[cd] 'hello world'";
            PredicateParser parser = new PredicateParser(predicateFormat);
            var predicate = parser.ParsePredicate();
            Assert.IsNotNull(predicate);
        }

        [Test]
        public void TestFormatArguments() {
            var predicate = Predicate.WithFormat("%d = 1", 10);
            Assert.AreEqual("(10 == 1)", predicate.Format);
        }

        class Prop {
            public string A { get; set; }
        }

        [Test]
        public void TestKeyPathFormatArguments()
        {
            var predicate = Predicate.WithFormat("%K =[c] 'Hi'", "A");
            Console.WriteLine(predicate.Format);
            var prop = new Prop() { A = "hi" };
            Assert.IsTrue(predicate.EvaluateObject(prop));
        }

        [Test]
        public void TestSelfBeginsWith() {
            var predicate = Predicate.WithFormat("SELF BEGINSWITH 'N'");
            string[] array = { "James", "Jack", "June", "John", "Jason", "Jill", "Nick" };
            Assert.AreEqual(1, array.Where(predicate).Count());
        }

        [Test]
        public void TestArithmetic() {
            var expr = Expr.WithFormat("1 + 2 + 3 * 9");
            var result = expr.Value<int>();
            Assert.AreEqual(30, result);
        }

        [Test]
        public void TestKeyPath()
        {
            var predicate = Predicate.WithFormat("A MATCHES '.*World$'");
            var prop = new Prop() { A = "Hello World" };
            Assert.IsTrue(predicate.EvaluateObject(prop));
        }

        [Test]
        public void TestArrayIndex()
        {
            var array = new int[] { 0, 1, 1, 2, 3, 5, 8, 13, 21 };
            var first = Expr.WithFormat("%@[FIRST]", array).Value<int>();
            Assert.AreEqual(array[0], first);
            var last = Expr.WithFormat("%@[LAST]", array).Value<int>();
            Assert.AreEqual(array[array.Length - 1], last);
            var size = Expr.WithFormat("%@[SIZE]", array).Value<int>();
            Assert.AreEqual(array.Length, size);
        }
    }
}

