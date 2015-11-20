using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace NPredicate
{
    public enum LinqDialect
    {
        Objects = 0,
        EntityFramework
    }

	public abstract class Predicate
	{
		public static Predicate Parse(string format, params dynamic[] args) {
            return new PredicateParser(format, args).ParsePredicate();
		}

		public static Predicate Constant(bool value) {
			return new ConstantPredicate(value);
		}

		public Expression<Func<T, bool>> LinqExpression<T>(LinqDialect dialect = LinqDialect.Objects) {
			ParameterExpression self = Expression.Parameter(typeof(T), "SELF");
            var bindings = new Dictionary<string, ParameterExpression>();
            bindings.Add(self.Name, self);
            return Expression.Lambda<Func<T, bool>>(LinqExpression(bindings, dialect), self);
		}

		public bool EvaluateObject<T>(T obj) {
			var expr = this.LinqExpression<T>();
			Func<T, bool> func = expr.Compile();
			return func(obj);
		}

		// Subclassers must implement:
		public abstract string Format { get; }

        public override string ToString()
        {
            return Format;
        }

		// subclassers implement this (potentially recursively) to provide an expression to the public LinqExpression()
		// method, given the provided bindings.
		public abstract Expression LinqExpression(Dictionary<string, ParameterExpression> bindings, LinqDialect dialect);
	}

    public static class PredicateExtensions {
        public static IQueryable<T> Where<T>(this IQueryable<T> e, Predicate p)
        {
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

