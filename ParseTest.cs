using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace NPredicate
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
            var predicate = Predicate.Parse("%d = 1", 10);
            Assert.AreEqual("(10 == 1)", predicate.Format);
        }

        class Prop {
            public string A { get; set; }
            public Prop P { get; set; }
        }

        [Test]
        public void TestKeyPathFormatArguments()
        {
            var predicate = Predicate.Parse("%K =[c] 'Hi'", "A");
            Console.WriteLine(predicate.Format);
            var prop = new Prop() { A = "hi" };
            Assert.IsTrue(predicate.EvaluateObject(prop));
        }

        [Test]
        public void TestSelfBeginsWith() {
            var predicate = Predicate.Parse("SELF BEGINSWITH 'N'");
            string[] array = { "James", "Jack", "June", "John", "Jason", "Jill", "Nick" };
            Assert.AreEqual(1, array.Where(predicate).Count());
        }

        [Test]
        public void TestArithmetic() {
            var expr = Expr.Parse("1 + 2 + 3 * 9");
            var result = expr.Value<int>();
            Assert.AreEqual(30, result);
        }

        [Test]
        public void TestKeyPath()
        {
            var predicate = Predicate.Parse("A MATCHES '.*World$'");
            var prop = new Prop() { A = "Hello World" };
            Assert.IsTrue(predicate.EvaluateObject(prop));
        }

        [Test]
        public void TestArrayIndex()
        {
            var array = new int[] { 0, 1, 1, 2, 3, 5, 8, 13, 21 };
            var first = Expr.Parse("%@[FIRST]", array).Value<int>();
            Assert.AreEqual(array[0], first);
            var last = Expr.Parse("%@[LAST]", array).Value<int>();
            Assert.AreEqual(array[array.Length - 1], last);
            var size = Expr.Parse("%@[SIZE]", array).Value<int>();
            Assert.AreEqual(array.Length, size);
        }

        [Test]
        public void TestEnumerableIndex()
        {
            var array = new int[] { 0, 1, 1, 2, 3, 5, 8, 13, 21 };
            var list = new System.Collections.Generic.List<int>(array);
            var first = Expr.Parse("%@[FIRST]", list).Value<int>();
            Assert.AreEqual(array[0], first);
            var last = Expr.Parse("%@[LAST]", list).Value<int>();
            Assert.AreEqual(array[array.Length - 1], last);
            var size = Expr.Parse("%@[SIZE]", list).Value<int>();
            Assert.AreEqual(array.Length, size);
        }

        [Test]
        public void TestNestedKeyPath()
        {
            var predicate = Predicate.Parse("P.P.A MATCHES '.*World$'");
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
            var count = Expr.Parse("SUBQUERY(keywords, $k, $k BEGINSWITH 'hello').@count");

            var doc = new Document();
            doc.Keywords = new string[] { "hello world", "hello vietnam", "hello usa", "goodbye cruel world" };

            var helloCount = count.ValueWithObject<Document, int>(doc);
            Assert.AreEqual(helloCount, 3);
        }

        [Test]
        public void TestSubquery2()
        {
            // SUBQUERY(keywords, $k, $k BEGINSWITH 'hello').@count
            var count = Expr.Parse("subquery(keywords, $k, $k BEGINSWITH 'hello')[SIZE]");

            var doc = new Document();
            doc.Keywords = new string[] { "hello world", "hello vietnam", "hello usa", "goodbye cruel world" };

            var helloCount = count.ValueWithObject<Document, int>(doc);
            Assert.AreEqual(helloCount, 3);
        }

        [Test]
        public void TestNoArgsFunction()
        {
            var now = Expr.Parse("NOW()").Value<DateTime>();
            Assert.IsTrue(Math.Abs(DateTime.UtcNow.Subtract(now).TotalSeconds) <= 1);

            var r1 = Expr.Parse("RANDOM()").Value<int>();
            var r2 = Expr.Parse("RANDOM()").Value<int>();
            Assert.AreNotEqual(r1, r2);
        }

        [Test]
        public void Test1ArgFunction()
        {
            var sum = Expr.Parse("SUM(%@)", new int[] { 5, 8 }).Value<int>();
            Assert.AreEqual(13, sum);
        }

        [Test]
        public void Test2ArgFunction()
        {
            var mod = Expr.Parse("FUNCTION('modulus:by:', 10, 7)").Value<int>();
            Assert.AreEqual(10 % 7, mod);
        }

        [Test]
        public void TestEmptyAggregate()
        {
            Assert.AreEqual(0, Expr.Parse("{}.@count").Value<int>());
        }

        [Test]
        public void TestAggregate()
        {
            Assert.AreEqual(3, Expr.Parse("{0,1,2}.@count").Value<int>());
        }

        [Test]
        public void TestValueFalse()
        {
            Assert.IsFalse(Expr.Parse("NO").Value<bool>());
        }

        [Test]
        public void TestValueTrue()
        {
            Assert.IsTrue(Expr.Parse("YES").Value<bool>());
        }

        [Test]
        public void TestNull()
        {
            Assert.IsTrue(Predicate.Parse("SELF.A == nil").EvaluateObject(new Prop()));
        }

        [Test]
        public void TestTruePredicate()
        {
            Assert.IsTrue(Predicate.Parse("TRUEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestFalsePredicate()
        {
            Assert.IsFalse(Predicate.Parse("FALSEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestOrPredicate()
        {
            Assert.IsTrue(Predicate.Parse("TRUEPREDICATE OR TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsTrue(Predicate.Parse("FALSEPREDICATE OR TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsTrue(Predicate.Parse("TRUEPREDICATE OR FALSEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.Parse("FALSEPREDICATE OR FALSEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestAndPredicate()
        {
            Assert.IsTrue(Predicate.Parse("TRUEPREDICATE AND TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.Parse("FALSEPREDICATE AND TRUEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.Parse("TRUEPREDICATE AND FALSEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.Parse("FALSEPREDICATE AND FALSEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestNotPredicate()
        {
            Assert.IsTrue(Predicate.Parse("NOT FALSEPREDICATE").EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.Parse("NOT TRUEPREDICATE").EvaluateObject<object>(null));
        }

        [Test]
        public void TestBetween()
        {
            Assert.IsTrue(Predicate.Parse("%d BETWEEN { 0, 2 }", 1).EvaluateObject<object>(null));
            Assert.IsFalse(Predicate.Parse("%d BETWEEN { 0, 2 }", 3).EvaluateObject<object>(null));
        }

        [Test]
        public void TestPower()
        {
            Assert.AreEqual(8.0, Expr.Parse("2.0**3.0").Value<double>());
        }

        [Test]
        public void TestSub()
        {
            Assert.AreEqual(1, Expr.Parse("3-2").Value<int>());
        }

        [Test]
        public void TestDiv()
        {
            Assert.AreEqual(10, Expr.Parse("100/10").Value<int>());
        }

        [Test]
        public void TestUnaryMinus()
        {
            Assert.AreEqual(-3, Expr.Parse("-(1+2)").Value<int>());
        }

        [Test]
        public void TestNegativeNumber()
        {
            Assert.AreEqual(-1.0, Expr.Parse("-1.0").Value<double>());
        }

        [Test]
        public void TestVariableAssignment()
        {
            Assert.AreEqual(3, Expr.Parse("$a := 1+2").Value<int>());
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


            var any = Predicate.Parse("ANY B.Collection.S BEGINSWITH 'Hello'");
            var all = Predicate.Parse("ALL B.Collection.S ENDSWITH 'World'");
            var allFail = Predicate.Parse("ALL B.Collection.S BEGINSWITH 'Hello'");
            var none = Predicate.Parse("NONE B.Collection.S BEGINSWITH 'Gday'");
            var noneFail = Predicate.Parse("NONE B.Collection.S BEGINSWITH 'Hello'");

            Assert.IsTrue(any.EvaluateObject(a));
            Assert.IsTrue(all.EvaluateObject(a));
            Assert.IsFalse(allFail.EvaluateObject(a));
            Assert.IsTrue(none.EvaluateObject(a));
            Assert.IsFalse(noneFail.EvaluateObject(a));
        }

        [Test]
        public void TestCastStringToNumber()
        {
            var e = Expr.Parse("CAST('123.0', 'NSNumber')");
            Assert.AreEqual(123.0, e.Value<double>());
        }

        [Test]
        public void TestCastDate()
        {
            var e = Expr.Parse("CAST(CAST(now(), 'NSNumber'), 'NSDate')");
            DateTime dt = e.Value<DateTime>();
            Assert.IsTrue((DateTime.UtcNow - dt).TotalSeconds < 1.0);
        }

        [Test]
        public void TestDateArithmetic()
        {
            var e = Expr.Parse("FUNCTION(now(), 'dateByAddingDays:', -2)");
            DateTime dt = e.Value<DateTime>();
            Assert.IsTrue(Math.Abs((DateTime.UtcNow.AddDays(-2) - dt).TotalSeconds) < 1.0);
        }
            

        [Test]
        public void TestPascalRewriter()
        {
            var keypath = Expr.Parse("a.b.c");
            keypath.Visit(new PascalCaseRewriter());
            Assert.AreEqual("A.B.C", keypath.Format);

            var subquery = Predicate.Parse("(SUBQUERY(SELF.a.collection, $c, (($c.name == 'foo') AND ($c.num < 10))).@count > 0)");
            subquery.Visit(new PascalCaseRewriter());
            Assert.AreEqual("(SUBQUERY(SELF.A.Collection, $c, (($c.Name == 'foo') AND ($c.Num < 10))).@count > 0)", subquery.Format);
        }
    }
}

