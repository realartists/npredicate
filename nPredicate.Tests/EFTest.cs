// No SQL Ce Server to connect to for these tests on the Mac side.
#if !__MonoCS__

namespace RealArtists.NPredicate.Tests {
  using System;
  using System.Collections.Generic;
  using System.Data.Entity;
  using System.Data.Entity.Migrations;
  using System.Linq;
  using Xunit;

  public class TestEFUser {
    public int Id { get; set; }
    public string Name { get; set; }
  }

  public class TestEFDocument {
    public TestEFDocument() {
      Watchers = new HashSet<TestEFUser>();
      CreationDate = DateTime.UtcNow;
      ModificationDate = DateTime.UtcNow;
    }

    public int Id { get; set; }
    public string Content { get; set; }
    public virtual ICollection<TestEFUser> Watchers { get; set; }
    public TestEFUser Author { get; set; }
    public DateTime ModificationDate { get; set; }
    public DateTime CreationDate { get; set; }
  }

  public class TestEFContext : DbContext {
    public virtual DbSet<TestEFDocument> Documents { get; set; }
    public virtual DbSet<TestEFUser> Users { get; set; }

    public TestEFContext() : base("name=TestEFContext") { }

    public TestEFContext(string conn) : base(conn) { }
  }

  public partial class TestEFMigration : DbMigrationsConfiguration<TestEFContext> {
    public TestEFMigration() : base() {
      this.AutomaticMigrationsEnabled = true;
      this.AutomaticMigrationDataLossAllowed = true;
    }
  }

  public class SqlCeFixture : IDisposable {
    public SqlCeFixture() {
      Database.SetInitializer(new DropCreateDatabaseAlways<TestEFContext>());

      using (var ctx = new TestEFContext()) {
        var james = ctx.Users.Add(new TestEFUser() { Name = "James Howard" });
        var nick = ctx.Users.Add(new TestEFUser() { Name = "Nick Sivo" });

        var doc1 = ctx.Documents.Add(new TestEFDocument() { Content = "Hello World" });
        doc1.Watchers.Add(james);
        doc1.Watchers.Add(nick);
        doc1.CreationDate = DateTime.UtcNow.AddDays(-3);
        doc1.ModificationDate = DateTime.UtcNow.AddDays(-2);

        ctx.Documents.Add(new TestEFDocument() { Content = "Goodbye Cruel World" });

        ctx.SaveChanges();
      }
    }

    public void Dispose() {
      using (var ctx = new TestEFContext()) {
        ctx.Database.Delete();
      }
    }
  }

  public class EFTest : IClassFixture<SqlCeFixture> {
    [Fact]
    public void TestEFSanity() {
      using (var ctx = new TestEFContext()) {
        Assert.True(ctx.Documents.Where(x => x.Content == "Hello World").Any());
        Assert.False(ctx.Documents.Where(x => x.Content == "Nobody here but us chickens").Any());
      }
    }

    [Fact]
    public void TestStringEquals() {
      using (var ctx = new TestEFContext("TestEFContext")) {
        var needle = Expr.MakeConstant("Hello World");
        var content = Expr.MakeKeyPath("Content");

        var pred = ComparisonPredicate.EqualTo(content, needle);
        var matches = ctx.Documents.Where(pred);

        Assert.Equal(1, matches.Count());
      }
    }

    [Fact]
    public void TestCaseInsensitive() {
      // Content ==[c] "hello world"

      using (var ctx = new TestEFContext()) {
        var needle = Expr.MakeConstant("hello world");
        var content = Expr.MakeKeyPath("Content");

        var pred = ComparisonPredicate.EqualTo(content, needle, ComparisonPredicateModifier.Direct, ComparisonPredicateOptions.CaseInsensitive);
        var matches = ctx.Documents.Where(pred.LinqExpression<TestEFDocument>().Compile());

        Assert.Equal(1, matches.Count());
      }

    }

    [Fact]
    public void TestSubquery() {
      // COUNT(SUBQUERY(Watchers, $user, $user.Name BEGINSWITH "James")) > 0
      // (x => x.Watchers.Where(user => user.Name.StartsWith("James")).Count() > 0);

      using (var ctx = new TestEFContext()) {
        var collection = Expr.MakeKeyPath("Watchers");
        var needle = Expr.MakeConstant("James");
        var user = Expr.MakeVariable("$user");
        var name = Expr.MakeKeyPath(user, "Name");
        var subquery = Expr.MakeSubquery(collection, "$user", ComparisonPredicate.BeginsWith(name, needle));

        var count = Expr.MakeFunction("count:", subquery);

        var pred = ComparisonPredicate.GreaterThan(count, Expr.MakeConstant(0));

        var matches = ctx.Documents.Where(pred);

        Assert.Equal(1, matches.Count());
      }
    }

    [Fact]
    public void TestSubquery2() {
      // SUBQUERY(Watchers, $user, $user.Name BEGINSWITH "James").@count > 0
      // (x => x.Watchers.Where(user => user.Name.StartsWith("James")).Count() > 0);

      using (var ctx = new TestEFContext()) {
        var collection = Expr.MakeKeyPath("Watchers");
        var needle = Expr.MakeConstant("James");
        var user = Expr.MakeVariable("$user");
        var name = Expr.MakeKeyPath(user, "Name");
        var subquery = Expr.MakeSubquery(collection, "$user", ComparisonPredicate.BeginsWith(name, needle));

        var count = Expr.MakeKeyPath(subquery, "@count");

        var pred = ComparisonPredicate.GreaterThan(count, Expr.MakeConstant(0));

        var matches = ctx.Documents.Where(pred);

        Assert.Equal(1, matches.Count());
      }
    }

    [Fact]
    public void TestSubquery3() {
      using (var ctx = new TestEFContext()) {
        var pred = Predicate.Parse("SUBQUERY(Watchers, $user, $user.Name BEGINSWITH 'James').@count > 0");
        var matches = ctx.Documents.Where(pred);

        Assert.Equal(1, matches.Count());
      }
    }

    // TODO: This can work, it's just a matter of placing _Predicate_MatchesRegex into
    // the database and then properly mapping and registering Utils._Predicate_MatchesRegex
    // with Entity Framework.
    // See ship://Problems/371 <Register _Predicate_MatchesRegex in the database and map it into EF>
#if false
        [Fact]
        public void TestMatches()
        {
            //var d1 = Context.Documents.Where(x => Utils._Predicate_MatchesRegex(x.Content, ".*World$"));
            using (var ctx = new TestEFContext()) {
                var predicate = Predicate.Parse("Content MATCHES '.*World$'");
                var d2 = ctx.Documents.Where(predicate);
                Assert.Equal(1, d2.Count());
            }
            // Assert.Throws<Exception>(delegate { Context.Documents.Where(predicate); });
        }
#endif

    [Fact]
    public void TestName() {
      using (var ctx = new TestEFContext()) {
        var predicate = Predicate.Parse("Author.Name == 'James Howard'");
        Assert.False(ctx.Documents.Where(predicate).Any());
      }
    }

    [Fact]
    public void TestDateComparison() {
      using (var ctx = new TestEFContext()) {
        var nope = Predicate.Parse("CreationDate > ModificationDate");
        var yep = Predicate.Parse("CreationDate < ModificationDate");

        Assert.False(ctx.Documents.Where(nope).Any());
        Assert.True(ctx.Documents.Where(yep).Any());
      }
    }

    [Fact]
    public void TestDateArithmetic() {
      using (var ctx = new TestEFContext()) {
        var nope = Predicate.Parse("CreationDate < FUNCTION(now(), 'dateByAddingDays:', -4)");
        var yep = Predicate.Parse("CreationDate < FUNCTION(now(), 'dateByAddingDays:', -2)");

        Assert.False(ctx.Documents.Where(nope).Any());
        Assert.True(ctx.Documents.Where(yep).Any());
      }
    }
  }
}

#endif
