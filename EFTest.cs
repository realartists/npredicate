using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Data.Common;
using Effort;

// The Effort stuff doesn't seem to work on Mono :(
#if !__MonoCS__

namespace Predicate
{
    public class TestEFUser {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestEFDocument {
        public TestEFDocument()
        {
            Watchers = new HashSet<TestEFUser>();
        }

        public int Id { get; set; }
        public string Content { get; set; }
        public virtual ICollection<TestEFUser> Watchers { get; set; }
    }

    public class TestEFContext : DbContext {
        public virtual DbSet<TestEFDocument> Documents { get; set; }
        public virtual DbSet<TestEFUser> Users { get; set; }

        public TestEFContext(DbConnection connection) : base(connection, true)
        {

        }
    }

    [TestFixture()]
    public class EFTest
    {
        private TestEFContext Context;

        [TestFixtureSetUp]
        public void Initialize() {
            if (Context != null)
                return;
            
//            Context = Effort.ObjectContextFactory.CreateTransient<TestEFContext>();
            var connection = Effort.DbConnectionFactory.CreateTransient();
            Context = new TestEFContext(connection);

            var james = Context.Users.Add(new TestEFUser() { Name = "James Howard" });
            var nick = Context.Users.Add(new TestEFUser() { Name = "Nick Sivo" });

            var doc1 = Context.Documents.Add(new TestEFDocument() { Content = "Hello World" });
            doc1.Watchers.Add(james);
            doc1.Watchers.Add(nick);

            Context.Documents.Add(new TestEFDocument() { Content = "Goodbye Cruel World" });
            
            Context.SaveChanges();

            
        }

        [Test]
        public void TestEFSanity()
        {
            Assert.IsTrue(Context.Documents.Where(x => x.Content == "Hello World").Any());
            Assert.IsFalse(Context.Documents.Where(x => x.Content == "Nobody here but us chickens").Any());
        }

        [Test]
        public void TestStringEquals()
        {
            var needle = Expr.MakeConstant("Hello World");
            var content = Expr.MakeKeyPath("Content");

            var pred = ComparisonPredicate.EqualTo(content, needle);
            var matches = Context.Documents.Where(pred.LinqExpression<TestEFDocument>().Compile());

            Assert.AreEqual(1, matches.Count());
        }

        [Test]
        public void TestCaseInsensitive()
        {
            // Content ==[c] "hello world"

            var needle = Expr.MakeConstant("hello world");
            var content = Expr.MakeKeyPath("Content");

            var pred = ComparisonPredicate.EqualTo(content, needle, ComparisonPredicateModifier.Direct, ComparisonPredicateOptions.CaseInsensitive);
            var matches = Context.Documents.Where(pred.LinqExpression<TestEFDocument>().Compile());

            Assert.AreEqual(1, matches.Count());
        }
      
        [Test]
        public void TestSubquery()
        {
            // COUNT(SUBQUERY(Watchers, $user, $user.Name BEGINSWITH "James")) > 0
            // (x => x.Watchers.Where(user => user.Name.StartsWith("James")).Count() > 0);

            var collection = Expr.MakeKeyPath("Watchers");
            var needle = Expr.MakeConstant("James");
            var user = Expr.MakeVariable("$user");
            var name = Expr.MakeKeyPath(user, "Name");
            var subquery = Expr.MakeSubquery(collection, "$user", ComparisonPredicate.BeginsWith(name, needle));

            var count = Expr.MakeFunction("count:", subquery);

            var pred = ComparisonPredicate.GreaterThan(count, Expr.MakeConstant(0));

            var matches = Context.Documents.Where(pred.LinqExpression<TestEFDocument>().Compile());

            Assert.AreEqual(1, matches.Count());
        }

        [Test]
        public void TestSubquery2()
        {
            // SUBQUERY(Watchers, $user, $user.Name BEGINSWITH "James").@count > 0
            // (x => x.Watchers.Where(user => user.Name.StartsWith("James")).Count() > 0);

            var collection = Expr.MakeKeyPath("Watchers");
            var needle = Expr.MakeConstant("James");
            var user = Expr.MakeVariable("$user");
            var name = Expr.MakeKeyPath(user, "Name");
            var subquery = Expr.MakeSubquery(collection, "$user", ComparisonPredicate.BeginsWith(name, needle));

            var count = Expr.MakeKeyPath(subquery, "@count");

            var pred = ComparisonPredicate.GreaterThan(count, Expr.MakeConstant(0));

            var matches = Context.Documents.Where(pred.LinqExpression<TestEFDocument>().Compile());

            Assert.AreEqual(1, matches.Count());
        }

        [Test]
        public void TestMatches()
        {
            var d1 = Context.Documents.Where(x => Utils._Predicate_MatchesRegex(x.Content, ".*World$"));

            var predicate = Predicate.WithFormat("Content MATCHES '.*World$'");
            var d2 = Context.Documents.Where(predicate);
            // Assert.Throws<Exception>(delegate { Context.Documents.Where(predicate); });
        }
    }
}

#endif
