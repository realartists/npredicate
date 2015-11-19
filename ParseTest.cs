using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

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
            public Prop P { get; set; }
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

        [Test]
        public void TestEnumerableIndex()
        {
            var array = new int[] { 0, 1, 1, 2, 3, 5, 8, 13, 21 };
            var list = new System.Collections.Generic.List<int>(array);
            var first = Expr.WithFormat("%@[FIRST]", list).Value<int>();
            Assert.AreEqual(array[0], first);
            var last = Expr.WithFormat("%@[LAST]", list).Value<int>();
            Assert.AreEqual(array[array.Length - 1], last);
            var size = Expr.WithFormat("%@[SIZE]", list).Value<int>();
            Assert.AreEqual(array.Length, size);
        }

        [Test]
        public void TestNestedKeyPath()
        {
            var predicate = Predicate.WithFormat("P.P.A MATCHES '.*World$'");
            var p = new Prop() { P = new Prop() { P = new Prop() { A = "Hello World" } } };
            Assert.IsTrue(predicate.EvaluateObject(p));
        }

        public class Document {
            public User Author { get; set; }
            public string Content { get; set; }
            public string[] Keywords { get; set; }

            public class User {
                public string Name { get; set; }
            }
        }

        [Test]
        public void TestSubquery()
        {
            // SUBQUERY(keywords, $k, $k BEGINSWITH 'hello').@count
            var count = Expr.WithFormat("SUBQUERY(keywords, $k, $k BEGINSWITH 'hello').@count");

            var doc = new Document();
            doc.Keywords = new string[] { "hello world", "hello vietnam", "hello usa", "goodbye cruel world" };

            var helloCount = count.ValueWithObject<Document, int>(doc);
            Assert.AreEqual(helloCount, 3);
        }

        [Test]
        public void TestSubquery2()
        {
            // SUBQUERY(keywords, $k, $k BEGINSWITH 'hello').@count
            var count = Expr.WithFormat("SUBQUERY(keywords, $k, $k BEGINSWITH 'hello')[SIZE]");

            var doc = new Document();
            doc.Keywords = new string[] { "hello world", "hello vietnam", "hello usa", "goodbye cruel world" };

            var helloCount = count.ValueWithObject<Document, int>(doc);
            Assert.AreEqual(helloCount, 3);
        }

        [Test]
        public void TestNoArgsFunction()
        {
            var now = Expr.WithFormat("NOW()").Value<DateTime>();
            Assert.IsTrue(Math.Abs(DateTime.UtcNow.Subtract(now).TotalSeconds) <= 1);

            var r1 = Expr.WithFormat("RANDOM()").Value<int>();
            var r2 = Expr.WithFormat("RANDOM()").Value<int>();
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Test1ArgFunction()
        {
            var sum = Expr.WithFormat("SUM(%@)", new int[] { 5, 8 }).Value<int>();
            Assert.AreEqual(13, sum);
        }

        [Test]
        public void Test2ArgFunction()
        {
            var mod = Expr.WithFormat("FUNCTION('modulus:by:', 10, 7)").Value<int>();
            Assert.AreEqual(10 % 7, mod);
        }

        [Test]
        public void TestEmptyAggregate()
        {
            Assert.AreEqual(0, Expr.WithFormat("{}.@count").Value<int>());
        }

        [Test]
        public void TestAggregate()
        {
            Assert.AreEqual(3, Expr.WithFormat("{0,1,2}.@count").Value<int>());
        }

        [Test]
        public void TestValueFalse()
        {
            Assert.IsFalse(Expr.WithFormat("NO").Value<bool>());
        }

        [Test]
        public void TestValueTrue()
        {
            Assert.IsTrue(Expr.WithFormat("YES").Value<bool>());
        }

        [Test]
        public void TestNull()
        {
            Assert.IsTrue(Predicate.WithFormat("SELF.A == nil").EvaluateObject(new Prop()));
        }

        [Test]
        public void TestTruePredicate()
        {
            Assert.IsTrue(Predicate.WithFormat("TRUEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestFalsePredicate()
        {
            Assert.IsFalse(Predicate.WithFormat("FALSEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestOrPredicate()
        {
            Assert.IsTrue(Predicate.WithFormat("TRUEPREDICATE OR TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsTrue(Predicate.WithFormat("FALSEPREDICATE OR TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsTrue(Predicate.WithFormat("TRUEPREDICATE OR FALSEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.WithFormat("FALSEPREDICATE OR FALSEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestAndPredicate()
        {
            Assert.IsTrue(Predicate.WithFormat("TRUEPREDICATE AND TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.WithFormat("FALSEPREDICATE AND TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.WithFormat("TRUEPREDICATE AND FALSEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.WithFormat("FALSEPREDICATE AND FALSEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestNotPredicate()
        {
            Assert.IsTrue(Predicate.WithFormat("NOT FALSEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.WithFormat("NOT TRUEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestBetween()
        {
            Assert.IsTrue(Predicate.WithFormat("%d BETWEEN { 0, 2 }", 1).EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.WithFormat("%d BETWEEN { 0, 2 }", 3).EvaluateObject<object>(null));
        }

        [Test]
        public void TestPower()
        {
            Assert.AreEqual(8.0, Expr.WithFormat("2.0**3.0").Value<double>());
        }

        [Test]
        public void TestSub()
        {
            Assert.AreEqual(1, Expr.WithFormat("3-2").Value<int>());
        }

        [Test]
        public void TestDiv()
        {
            Assert.AreEqual(10, Expr.WithFormat("100/10").Value<int>());
        }

        [Test]
        public void TestUnaryMinus()
        {
            Assert.AreEqual(-3, Expr.WithFormat("-(1+2)").Value<int>());
        }

        [Test]
        public void TestNegativeNumber()
        {
            Assert.AreEqual(-1.0, Expr.WithFormat("-1.0").Value<double>());
        }

        [Test]
        public void TestVariableAssignment()
        {
            Assert.AreEqual(3, Expr.WithFormat("$a := 1+2").Value<int>());
        }

        class C {
            public string S { get; set; }
        }

        class B {
            public IEnumerable<C> Collection { get; set; }
        }

        class A {
            public B B { get; set; }
        }

        [Test]
        public void TestKeyPathCollection()
        {
            C c0 = new C() { S = "Hello World" };
            C c1 = new C() { S = "Goodbye Cruel World" };

            B b = new B() { Collection = new C[] { c0, c1 } };

            A a = new A() { B = b };


            var any = Predicate.WithFormat("ANY B.Collection.S BEGINSWITH 'Hello'");
            var all = Predicate.WithFormat("ALL B.Collection.S ENDSWITH 'World'");
            var allFail = Predicate.WithFormat("ALL B.Collection.S BEGINSWITH 'Hello'");
            var none = Predicate.WithFormat("NONE B.Collection.S BEGINSWITH 'Gday'");
            var noneFail = Predicate.WithFormat("NONE B.Collection.S BEGINSWITH 'Hello'");

            Assert.IsTrue(any.EvaluateObject(a));
            Assert.IsTrue(all.EvaluateObject(a));
            Assert.IsFalse(allFail.EvaluateObject(a));
            Assert.IsTrue(none.EvaluateObject(a));
            Assert.IsFalse(noneFail.EvaluateObject(a));
        }
            
    }
}

