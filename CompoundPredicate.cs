using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace Predicate
{
	public enum CompoundPredicateType {
		Not,
		And,
		Or
	}

	public class CompoundPredicate : Predicate
	{
		public CompoundPredicateType CompoundPredicateType {
			get;
			protected set;
		}

		public IEnumerable<Predicate> Subpredicates {
			get;
			protected set;
		}

		protected CompoundPredicate() { }

		public static CompoundPredicate And(IEnumerable<Predicate> subpredicates) {
			CompoundPredicate p = new CompoundPredicate();
			p.CompoundPredicateType = CompoundPredicateType.And;
			p.Subpredicates = subpredicates;
			return p;
		}

		public static CompoundPredicate Or(IEnumerable<Predicate> subpredicates) {
			CompoundPredicate p = new CompoundPredicate();
			p.CompoundPredicateType = CompoundPredicateType.Or;
			p.Subpredicates = subpredicates;
			return p;
		}

		public static CompoundPredicate Not(Predicate subpredicate) {
			CompoundPredicate p = new CompoundPredicate();
			p.CompoundPredicateType = CompoundPredicateType.Not;
			var l = new List<Predicate> ();
			l.Add (subpredicate);
			p.Subpredicates = l;
			return p;
		}

		public override string Format {
			get {
				switch (CompoundPredicateType) {
                    case CompoundPredicateType.And:
                        return "(" + String.Join(" AND ", Subpredicates) + ")";
                    case CompoundPredicateType.Or:
                        return "(" + String.Join(" OR ", Subpredicates) + ")";
				    case CompoundPredicateType.Not:
					    return $"NOT ({Subpredicates.First().Format})";
				}
				return "";
			}
		}

        private Expression GenerateAnd(Dictionary<string, ParameterExpression> bindings, IEnumerable<Predicate> predicates, LinqDialect dialect) {
			if (predicates.Count() > 2) {
                Expression a = predicates.First().LinqExpression(bindings, dialect);
                Expression b = GenerateAnd(bindings, predicates.Skip(1), dialect);
				return Expression.AndAlso (a, b);
			} else if (predicates.Count() == 2) {
                Expression a = predicates.First().LinqExpression(bindings, dialect);
                Expression b = predicates.Last().LinqExpression(bindings, dialect);
				return Expression.AndAlso(a, b);
			} else if (predicates.Count() == 1) {
				return predicates.First().LinqExpression(bindings, dialect);
			} else {
                return new ConstantPredicate(false).LinqExpression(bindings, dialect);
			}
		}

        private Expression GenerateOr(Dictionary<string, ParameterExpression> bindings, IEnumerable<Predicate> predicates, LinqDialect dialect) {
			if (predicates.Count() > 2) {
				Expression a = predicates.First().LinqExpression(bindings, dialect);
				Expression b = GenerateOr(bindings, predicates.Skip(1), dialect);
				return Expression.OrElse(a, b);
			} else if (predicates.Count() == 2) {
				Expression a = predicates.First().LinqExpression(bindings, dialect);
				Expression b = predicates.Last().LinqExpression(bindings, dialect);
				return Expression.OrElse(a, b);
			} else if (predicates.Count() == 1) {
				return predicates.First().LinqExpression(bindings, dialect);
			} else {
				return new ConstantPredicate(false).LinqExpression(bindings, dialect);
			}
		}

		public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings, LinqDialect dialect)
		{
			switch (CompoundPredicateType) {
				case CompoundPredicateType.And:
                    return GenerateAnd(bindings, Subpredicates, dialect);
				case CompoundPredicateType.Or:
                    return GenerateOr(bindings, Subpredicates, dialect);
				case CompoundPredicateType.Not:
                    return Expression.Not(Subpredicates.First().LinqExpression(bindings, dialect));
			}
			return null;
		}
	}
}

