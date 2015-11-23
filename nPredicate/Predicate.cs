namespace RealArtists.NPredicate {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Linq.Expressions;

  public enum LinqDialect {
    Objects = 0,
    EntityFramework
  }

  public abstract class Predicate {
    public static Predicate Parse(string format, params dynamic[] args) {
      return new PredicateParser(format, args).ParsePredicate();
    }

    public static Predicate Constant(bool value) {
      return new ConstantPredicate(value);
    }

    public Expression<Func<T, bool>> LinqExpression<T>(LinqDialect dialect = LinqDialect.Objects) {
      ParameterExpression self = Expression.Parameter(typeof(T), "SELF");
      var bindings = new Dictionary<string, Expression>();
      bindings.Add(self.Name, self);
      if (VariableBindings != null) {
        foreach (var pair in VariableBindings) {
          bindings.Add("$" + pair.Key, Expression.Constant(pair.Value));
        }
      }
      return Expression.Lambda<Func<T, bool>>(LinqExpression(bindings, dialect), self);
    }

    public bool EvaluateObject<T>(T obj) {
      var expr = this.LinqExpression<T>();
      Func<T, bool> func = expr.Compile();
      return func(obj);
    }

    // Dictionary of variable names to values to replace variable references in the predicate.
    // For example, if the predicate is $foo == 42, and you want to set the value of $foo do:
    // predicate.VariableBindings = new Dictionary<string, dynamic>() { { "foo", 42 } };
    // Note that the preceding $ should be omitted in the VariableBindings dictionary.
    public Dictionary<string, dynamic> VariableBindings { get; set; }

    // Subclassers may override this
    public virtual void Visit(IVisitor visitor) { visitor.Visit(this); }

    // Subclassers must implement:
    public abstract string Format { get; }

    public override string ToString() {
      return Format;
    }

    // subclassers implement this (potentially recursively) to provide an expression to the public LinqExpression()
    // method, given the provided bindings.
    public abstract Expression LinqExpression(Dictionary<string, Expression> bindings, LinqDialect dialect);
  }

  public static class PredicateExtensions {
    public static IQueryable<T> Where<T>(this IQueryable<T> e, Predicate p) {
      var linq = p.LinqExpression<T>(LinqDialect.EntityFramework);
      var result = e.Where(linq);
      return result;
    }

    public static IEnumerable<T> Where<T>(this IEnumerable<T> e, Predicate p) {
      var linq = p.LinqExpression<T>();
      var result = e.Where(linq.Compile());
      return result;
    }
  }
}

