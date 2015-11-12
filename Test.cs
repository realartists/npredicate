using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Predicate
{

	[TestFixture ()]
	public class Test
	{
		[Test ()]
		public void TestAnd()
		{
			Predicate yes = Predicate.Constant(true);
			Predicate no = Predicate.Constant(false);

			// (YES AND NO)
			Assert.IsFalse(CompoundPredicate.And(new Predicate[] { yes, no }).EvaluateObject<object>(null));
			// (YES AND YES)
			Assert.IsTrue(CompoundPredicate.And(new Predicate[] { yes, yes }).EvaluateObject<object>(null));
			// (NO AND NO) 
			Assert.IsFalse(CompoundPredicate.And(new Predicate[] { no, no }).EvaluateObject<object>(null));
			// (YES AND YES AND YES AND YES AND YES)
			Assert.IsTrue(CompoundPredicate.And(new Predicate[] { yes, yes, yes, yes, yes }).EvaluateObject<object>(null));
		}

		[Test ()]
		public void TestLinq()
		{
			// (YES OR NO)

			Predicate yes = Predicate.Constant(true);
			Predicate no = Predicate.Constant(false);

			Predicate or = CompoundPredicate.Or(new Predicate[] { yes, no });

			var l = new List<Boolean>();
			l.Add(true);

			var v = l.Where(or.LinqExpression<Boolean>().Compile());

			Assert.AreEqual(1, v.Count());
		}

		public class Document {
			public User Author { get; set; }
			public string Content { get; set; }
			public string[] Keywords { get; set; }

			public class User {
				public string Name { get; set; }
			}
		}

		[Test()]
		public void TestKeyPath()
		{
			Document.User user = new Document.User();
			user.Name = "James";

			Document doc = new Document();
			doc.Content = "Hello World";
			doc.Author = user;

			// Content
			var content = Expr.MakeKeyPath("Content").ValueWithObject<Document, string>(doc);
			Assert.AreEqual(doc.Content, content);

			// Author.Name
			var authorName = Expr.MakeKeyPath("Author.Name").ValueWithObject<Document, string>(doc);
			Assert.AreEqual(doc.Author.Name, authorName);
		}

		[Test()]
		public void TestSafeNavigation()
		{
			// Author.Name
			Document doc = new Document();
			doc.Content = "Hello World";

			var authorName = Expr.MakeKeyPath("Author.Name").ValueWithObject<Document, string>(doc);
			Assert.IsNull(authorName);
		}

		[Test()]
		public void TestNumericComparison()
		{
			// SELF = 1
			var a = Expr.MakeEvaluatedObject();
			var b = Expr.MakeConstant(1);

			var p = ComparisonPredicate.LessThan(a, b);

			Assert.IsTrue(p.EvaluateObject(0));
			Assert.IsFalse(p.EvaluateObject(1));
		}

		[Test()]
		public void TestStringEquals()
		{
			// SELF = 'Hello World'
			var a = Expr.MakeEvaluatedObject();
			var b = Expr.MakeConstant("Hello World");

			var p = ComparisonPredicate.EqualTo(a, b);
			Assert.IsTrue(p.EvaluateObject("Hello World"));
			Assert.IsFalse(p.EvaluateObject("Goodbye Cruel World"));
		}

		[Test()]
		public void TestStringEqualsCaseInsensitive()
		{
			// SELF =[c] 'Hello World'
			var a = Expr.MakeEvaluatedObject();
			var b = Expr.MakeConstant("Hello World");

            var p = ComparisonPredicate.EqualTo(a, b, ComparisonPredicateModifier.Direct, ComparisonPredicateOptions.CaseInsensitive);
			Assert.IsTrue(p.EvaluateObject("hello world"));
		}

		[Test()]
		public void TestRegex()
		{
			// SELF MATCHES '^[0-9a-fA-F]+$'
			var a = Expr.MakeEvaluatedObject();
			var b = Expr.MakeConstant("^[0-9a-fA-F]+$");

			var p = ComparisonPredicate.Matches(a, b);
			Assert.IsTrue(p.EvaluateObject("F00DFACE"));
			Assert.IsFalse(p.EvaluateObject("Hello World"));
		}

		[Test()]
		public void TestIn()
		{
			// SELF IN { 1, 2, 3, 4 }
			var a = Expr.MakeEvaluatedObject();
			var b = Expr.MakeConstant(new List<int>(new int[] { 1, 2, 3, 4 }));

			var p = ComparisonPredicate.In(a, b);
			Assert.IsTrue(p.EvaluateObject(1));
			Assert.IsFalse(p.EvaluateObject(0));
		}

		[Test()]
		public void TestBetween()
		{
			// SELF BETWEEN { 1, 4 }
			var a = Expr.MakeEvaluatedObject();
			var b = Expr.MakeConstant(new List<int>(new int[] { 1, 4 }));

			var p = ComparisonPredicate.Between(a, b);
			Assert.IsTrue(p.EvaluateObject(2));
			Assert.IsFalse(p.EvaluateObject(0));
		}

		[Test()]
		public void TestSubquery()
		{
			// SUBQUERY(keywords, $k, $k BEGINSWITH 'hello').@count
			var k = Expr.MakeVariable("$k");
			var prefix = Expr.MakeConstant("hello");
            var subpred = ComparisonPredicate.BeginsWith(k, prefix);

			var haystack = Expr.MakeKeyPath("keywords");
			var subquery = Expr.MakeSubquery(haystack, "$k", subpred);

			var count = Expr.MakeKeyPath(subquery, "@count");

			var doc = new Document();
			doc.Keywords = new string[] { "hello world", "hello vietnam", "hello usa", "goodbye cruel world" };

			var helloCount = count.ValueWithObject<Document, int>(doc);
			Assert.AreEqual(helloCount, 3);
		}

		[Test()]
		public void TestAggregate()
		{
			// SELF IN { 0, 1 }
			var zero = Expr.MakeConstant(0);
			var one = Expr.MakeConstant(1);

			var agg = Expr.MakeAggregate(new Expr[] { zero, one });

			var p = ComparisonPredicate.In(Expr.MakeEvaluatedObject(), agg);

			Assert.IsTrue(p.EvaluateObject(0));
			Assert.IsTrue(p.EvaluateObject(1));
			Assert.IsFalse(p.EvaluateObject(2));
		}

		[Test()]
		public void TestSum()
		{
			var l = Expr.MakeAggregate(new Expr[] { Expr.MakeConstant(1), Expr.MakeConstant(2), Expr.MakeConstant(3), });
			var sum = Expr.MakeFunction("sum:", new Expr[] { l });

			Assert.AreEqual(6, sum.LinqExpression<int, int>().Compile()(0));
		}

		[Test()]
		public void TestArithmetic()
		{
			var self = Expr.MakeEvaluatedObject();
			var two = Expr.MakeConstant(2);

			var add = Expr.MakeFunction("add:to:", self, two);
			var sub = Expr.MakeFunction("from:subtract:", self, two);
			var abs = Expr.MakeFunction("abs:", self);
			var rand = Expr.MakeFunction("random");

			Assert.AreEqual(4, add.ValueWithObject<int, int>(2));
			Assert.AreEqual(0, sub.ValueWithObject<int, int>(2));
			Assert.AreEqual(2, abs.ValueWithObject<int, int>(-2));
           
			int i = rand.ValueWithObject<int, int>(0);
            int j = rand.ValueWithObject<int, int>(0);

            Assert.True(i != j);
		}

		[Test()]
		public void TestStringFns() {
			var self = Expr.MakeEvaluatedObject();

			var lower = Expr.MakeFunction("lowercase:", self);
			var upper = Expr.MakeFunction("uppercase:", self);

			Assert.AreEqual("hello", lower.ValueWithObject<string, string>("Hello"));
			Assert.AreEqual("HELLO", upper.ValueWithObject<string, string>("Hello"));
		}
	}
}

