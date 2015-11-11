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

		#if false
		public override bool EvaluateObject(dynamic obj) {
			switch (compoundPredicateType) {
			case CompoundPredicateType.And:
				{
					foreach (Predicate p in subpredicates) {
						if (!(p.EvaluateObject (obj))) {
							return false;
						}
					}
					return true;
				}
			case CompoundPredicateType.Or:
				{
					foreach (Predicate p in subpredicates) {
						if (p.EvaluateObject (obj)) {
							return true;
						}
					}
					return false;
				}
			case CompoundPredicateType.Not:
				{
					Predicate p = subpredicates.First();
					return !p.EvaluateObject (obj);
				}
			}

			return false;
		}
		#endif

		public override string Format {
			get {
				switch (compoundPredicateType) {
				case CompoundPredicateType.And:
					return String.Join(" AND ", subpredicates);
				case CompoundPredicateType.Or:
					return String.Join(" OR ", subpredicates);
				case CompoundPredicateType.Not:
					return "NOT (${subpredicates.First().Format})";
				}
				return "";
			}
		}

		private Expression GenerateAnd(ParameterExpression self, IEnumerable<Predicate> predicates) {
			if (predicates.Count() > 2) {
				Expression a = predicates.First().LinqExpression(self);
				Expression b = GenerateAnd(self, predicates.Skip(1));
				return Expression.AndAlso (a, b);
			} else if (predicates.Count() == 2) {
				Expression a = predicates.First().LinqExpression(self);
				Expression b = predicates.Last().LinqExpression(self);
				return Expression.AndAlso(a, b);
			} else if (predicates.Count() == 1) {
				return predicates.First().LinqExpression(self);
			} else {
				return new ConstantPredicate(false).LinqExpression(self);
			}
		}

		private Expression GenerateOr(ParameterExpression self, IEnumerable<Predicate> predicates) {
			if (predicates.Count() > 2) {
				Expression a = predicates.First().LinqExpression(self);
				Expression b = GenerateOr(self, predicates.Skip(1));
				return Expression.OrElse(a, b);
			} else if (predicates.Count() == 2) {
				Expression a = predicates.First().LinqExpression(self);
				Expression b = predicates.Last().LinqExpression(self);
				return Expression.OrElse(a, b);
			} else if (predicates.Count() == 1) {
				return predicates.First().LinqExpression(self);
			} else {
				return new ConstantPredicate(false).LinqExpression(self);
			}
		}

		public override Expression LinqExpression(ParameterExpression self)
		{
			switch (compoundPredicateType) {
				case CompoundPredicateType.And:
					return GenerateAnd(self, subpredicates);
				case CompoundPredicateType.Or:
					return GenerateOr(self, subpredicates);
				case CompoundPredicateType.Not:
					return Expression.Not(subpredicates.First().LinqExpression(self));
			}
			return null;
		}
	}
}

