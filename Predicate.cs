using System;
using System.Linq;
using System.Linq.Expressions;

namespace Predicate
{
	public abstract class Predicate
	{
		public static Predicate WithFormat(string format, params dynamic[] args) {
			return null;
		}

		public static Predicate Constant(bool value) {
			return new ConstantPredicate(value);
		}

		public Expression<Func<T, bool>> LinqExpression<T>() {
			ParameterExpression self = Expression.Parameter(typeof(T), "SELF");
			return Expression.Lambda<Func<T, bool>>(LinqExpression(self), self);
		}

		public bool EvaluateObject (dynamic obj) {
			var expr = this.LinqExpression<dynamic>();
			Func<dynamic, bool> func = expr.Compile();
			return func(obj);
		}

		// Subclassers must implement:
		public abstract string Format { get; }

		// subclassers implement this (potentially recursively) to provide an expression to the public LinqExpression()
		// method, given the provided self parameter.
		public abstract Expression LinqExpression(ParameterExpression self);
	}
}

