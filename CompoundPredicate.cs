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
		public CompoundPredicateType compoundPredicateType {
			get;
			protected set;
		}

		public IEnumerable<Predicate> subpredicates {
			get;
			protected set;
		}

		protected CompoundPredicate() { }

		public static CompoundPredicate And(IEnumerable<Predicate> subpredicates) {
			CompoundPredicate p = new CompoundPredicate();
			p.compoundPredicateType = CompoundPredicateType.And;
			p.subpredicates = subpredicates;
			return p;
		}

		public static CompoundPredicate Or(IEnumerable<Predicate> subpredicates) {
			CompoundPredicate p = new CompoundPredicate();
			p.compoundPredicateType = CompoundPredicateType.Or;
			p.subpredicates = subpredicates;
			return p;
		}

		public static CompoundPredicate Not(Predicate subpredicate) {
			CompoundPredicate p = new CompoundPredicate();
			p.compoundPredicateType = CompoundPredicateType.Not;
			var l = new List<Predicate> ();
			l.Add (subpredicate);
			p.subpredicates = l;
			return p;
		}

		public override string Format {
			get {
				switch (compoundPredicateType) {
                    case CompoundPredicateType.And:
                        return "(" + String.Join(" AND ", subpredicates) + ")";
                    case CompoundPredicateType.Or:
                        return "(" + String.Join(" OR ", subpredicates) + ")";
				    case CompoundPredicateType.Not:
					    return "NOT (${subpredicates.First().Format})";
				}
				return "";
			}
		}

        private Expression GenerateAnd(Dictionary<string, ParameterExpression> bindings, IEnumerable<Predicate> predicates) {
			if (predicates.Count() > 2) {
                Expression a = predicates.First().LinqExpression(bindings);
                Expression b = GenerateAnd(bindings, predicates.Skip(1));
				return Expression.AndAlso (a, b);
			} else if (predicates.Count() == 2) {
                Expression a = predicates.First().LinqExpression(bindings);
                Expression b = predicates.Last().LinqExpression(bindings);
				return Expression.AndAlso(a, b);
			} else if (predicates.Count() == 1) {
				return predicates.First().LinqExpression(bindings);
			} else {
                return new ConstantPredicate(false).LinqExpression(bindings);
			}
		}

        private Expression GenerateOr(Dictionary<string, ParameterExpression> bindings, IEnumerable<Predicate> predicates) {
			if (predicates.Count() > 2) {
				Expression a = predicates.First().LinqExpression(bindings);
				Expression b = GenerateOr(bindings, predicates.Skip(1));
				return Expression.OrElse(a, b);
			} else if (predicates.Count() == 2) {
				Expression a = predicates.First().LinqExpression(bindings);
				Expression b = predicates.Last().LinqExpression(bindings);
				return Expression.OrElse(a, b);
			} else if (predicates.Count() == 1) {
				return predicates.First().LinqExpression(bindings);
			} else {
				return new ConstantPredicate(false).LinqExpression(bindings);
			}
		}

		public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
		{
			switch (compoundPredicateType) {
				case CompoundPredicateType.And:
                    return GenerateAnd(bindings, subpredicates);
				case CompoundPredicateType.Or:
                    return GenerateOr(bindings, subpredicates);
				case CompoundPredicateType.Not:
                    return Expression.Not(subpredicates.First().LinqExpression(bindings));
			}
			return null;
		}
	}
}

