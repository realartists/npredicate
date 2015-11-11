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

			Assert.IsFalse(CompoundPredicate.And(new Predicate[] { yes, no }).EvaluateObject(null));
			Assert.IsTrue(CompoundPredicate.And(new Predicate[] { yes, yes }).EvaluateObject(null));
			Assert.IsFalse(CompoundPredicate.And(new Predicate[] { no, no }).EvaluateObject(null));
			Assert.IsTrue(CompoundPredicate.And(new Predicate[] { yes, yes, yes, yes, yes }).EvaluateObject(null));
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
	}
}

