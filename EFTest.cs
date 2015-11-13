using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Data.Common;
using Effort;

namespace Predicate
{
    public class TestEFUser {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestEFDocument {
        public int Id { get; set; }
        public string Content { get; set; }
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

        public void Initialize() {
            if (Context != null)
                return;
            
//            Context = Effort.ObjectContextFactory.CreateTransient<TestEFContext>();
            var connection = Effort.DbConnectionFactory.CreateTransient();
            Context = new TestEFContext(connection);

            Context.Documents.Add(new TestEFDocument() { Content = "Hello World" });
            Context.Documents.Add(new TestEFDocument() { Content = "Goodbye Cruel World" });

            Context.Users.Add(new TestEFUser() { Name = "James Howard" });
            Context.Users.Add(new TestEFUser() { Name = "Nick Sivo" });

            Context.SaveChanges();
        }

        [Test]
        public void TestEFSanity()
        {
            Initialize();

            Console.WriteLine("Hello");
            Console.WriteLine(Context.Documents.Count());
//            Assert.IsTrue(Context.Documents.Where(x => x.Content == "Hello World").Any());
//            Assert.IsFalse(Context.Documents.Where(x => x.Content == "Nobody here but us chickens").Any());
        }
    }
}

