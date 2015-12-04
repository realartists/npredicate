# npredicate
An NSPredicate to LINQ Expressions translator [![Build status](https://ci.appveyor.com/api/projects/status/a9ovkxf4d1pxol4t?svg=true)](https://ci.appveyor.com/project/NicholasSivo/npredicate)

This library allows you to perform queries in .net using the NSPredicate query language from Cocoa. Every aspect of the documented NSPredicate syntax that I am aware of is supported, as well as some un/underdocumented aspects as well.

There are two reasons to do this:

1. You can use it to share queries between a Cocoa app and a .net app.
2. NSPredicate provides a concise string based query language, so saving, loading, and running user provided queries is possible.

## Examples

```
var predicate = Predicate.Parse("SELF BEGINSWITH 'N'");
string[] array = { "James", "Jack", "June", "John", "Jason", "Jill", "Nick" };
var filteredNames = array.Where(predicate); // returns { "Nick" }
```

```
using (var ctx = new TestEFContext())
{
  var pred = Predicate.Parse("SUBQUERY(Watchers, $user, $user.Name BEGINSWITH 'James').@count > 0");
  var matches = ctx.Documents.Where(pred); // Returns all documents who have more than 0 users watching them named "James"
}
```

## How it works

The library parses the NSPredicate syntax into a syntax tree that mirrors the types provided in Cocoa (NSPredicate => Predicate, NSExpression => Expr). From there, you can interact with the syntax tree in the same ways that you would in Cocoa, or you can create a syntax tree from scratch programmatically.

Once you have a Predicate or Expr object, you can create a System.Linq.Expression from it and use it anywhere that you would use a LINQ expression. Conveniences are offered for filtering IQueryable and IEnumerable types via overloaded Where methods.

Special care is made for the quirks of Entity Framework 6, and when generating a LINQ expression for use with Entity Framework, the library will take care to generate an expression tree that is compatible with Entity Framework.

Because the NSPredicate is compiled into LINQ expressions, there should be very little performance cost to using NSPredicate syntax in place of using LINQ directly.
