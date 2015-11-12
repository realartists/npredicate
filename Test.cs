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

			Assert.IsFalse(CompoundPredicate.And(new Predicate[] { yes, no }).EvaluateObject<object>(null));
			Assert.IsTrue(CompoundPredicate.And(new Predicate[] { yes, yes }).EvaluateObject<object>(null));
			Assert.IsFalse(CompoundPredicate.And(new Predicate[] { no, no }).EvaluateObject<object>(null));
			Assert.IsTrue(CompoundPredicate.And(new Predicate[] { yes, yes, yes, yes, yes }).EvaluateObject<object>(null));
		}

		[Test ()]
		public void TestLinq()
		{
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

			var content = Expr.KeyPath("Content").ValueWithObject<Document, string>(doc);
			Assert.AreEqual(doc.Content, content);
			var authorName = Expr.KeyPath("Author.Name").ValueWithObject<Document, string>(doc);
			Assert.AreEqual(doc.Author.Name, authorName);
		}

		[Test()]
		public void TestSafeNavigation()
		{
			Document doc = new Document();
			doc.Content = "Hello World";

			var authorName = Expr.KeyPath("Author.Name").ValueWithObject<Document, string>(doc);
			Assert.IsNull(authorName);
		}

		[Test()]
		public void TestNumericComparison()
		{
			var a = Expr.EvaluatedObject();
			var b = Expr.Constant(1);

			var p = ComparisonPredicate.Comparison(a, PredicateOperatorType.LessThan, b);

			Assert.IsTrue(p.EvaluateObject(0));
			Assert.IsFalse(p.EvaluateObject(1));
		}

		[Test()]
		public void TestStringEquals()
		{
			var a = Expr.EvaluatedObject();
			var b = Expr.Constant("Hello World");

			var p = ComparisonPredicate.Comparison(a, PredicateOperatorType.EqualTo, b);
			Assert.IsTrue(p.EvaluateObject("Hello World"));
			Assert.IsFalse(p.EvaluateObject("Goodbye Cruel World"));
		}

		[Test()]
		public void TestStringEqualsCaseInsensitive()
		{
			var a = Expr.EvaluatedObject();
			var b = Expr.Constant("Hello World");

			var p = ComparisonPredicate.Comparison(a, PredicateOperatorType.EqualTo, b, ComparisonPredicateModifier.Direct, ComparisonPredicateOptions.CaseInsensitive);
			Assert.IsTrue(p.EvaluateObject("hello world"));
		}

		[Test()]
		public void TestRegex()
		{
			var a = Expr.EvaluatedObject();
			var b = Expr.Constant("^[0-9a-fA-F]+$");

			var p = ComparisonPredicate.Comparison(a, PredicateOperatorType.Matches, b);
			Assert.IsTrue(p.EvaluateObject("F00DFACE"));
			Assert.IsFalse(p.EvaluateObject("Hello World"));
		}

		[Test()]
		public void TestIn()
		{
			var a = Expr.EvaluatedObject();
			var b = Expr.Constant(new List<int>(new int[] { 1, 2, 3, 4 }));

			var p = ComparisonPredicate.Comparison(a, PredicateOperatorType.In, b);
			Assert.IsTrue(p.EvaluateObject(1));
			Assert.IsFalse(p.EvaluateObject(0));
		}

		[Test()]
		public void TestBetween()
		{
			var a = Expr.EvaluatedObject();
			var b = Expr.Constant(new List<int>(new int[] { 1, 4 }));

			var p = ComparisonPredicate.Comparison(a, PredicateOperatorType.Between, b);
			Assert.IsTrue(p.EvaluateObject(2));
			Assert.IsFalse(p.EvaluateObject(0));
		}

		[Test()]
		public void TestSubquery()
		{
			var k = Expr.Variable("$k");
			var prefix = Expr.Constant("hello");
			var subpred = ComparisonPredicate.Comparison(k, PredicateOperatorType.BeginsWith, prefix);

			var haystack = Expr.KeyPath("keywords");
			var subquery = Expr.Subquery(haystack, "$k", subpred);

			var count = Expr.KeyPath(subquery, "@count");

			var doc = new Document();
			doc.Keywords = new string[] { "hello world", "hello vietnam", "hello usa", "goodbye cruel world" };

			var helloCount = count.ValueWithObject<Document, int>(doc);
			Assert.AreEqual(helloCount, 3);
		}
	}
}

