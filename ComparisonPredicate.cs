using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;

namespace NPredicate
{
    [Flags]
    public enum ComparisonPredicateOptions {
        CaseInsensitive = 0x01,
        DiacriticInsensitive = 0x02,
        Normalized = 0x04,
    }

    public enum ComparisonPredicateModifier {
        Direct = 0, // Do a direct comparison
        All, // ALL toMany.x = y
        Any // ANY toMany.x = y
    }

    public enum PredicateOperatorType {
        LessThan,
        LessThanOrEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        EqualTo,
        NotEqualTo,
        Matches,
        Like,
        BeginsWith,
        EndsWith,
        In,
        Contains,
        Between
    }

    public class ComparisonPredicate : Predicate
    {
        protected ComparisonPredicate() { }

        public static ComparisonPredicate Comparison(Expr left, PredicateOperatorType op, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            ComparisonPredicate p = new ComparisonPredicate();
            p.LeftExpression = left;
            p.PredicateOperatorType = op;
            p.RightExpression = right;
            p.ComparisonPredicateModifier = modifier;
            p.Options = options;
            return p;
        }

        public static ComparisonPredicate LessThan(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.LessThan, right, modifier, options);
        }

        public static ComparisonPredicate LessThanOrEqualTo(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.LessThanOrEqualTo, right, modifier, options);
        }

        public static ComparisonPredicate GreaterThan(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.GreaterThan, right, modifier, options);
        }

        public static ComparisonPredicate GreaterThanOrEqualTo(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.GreaterThanOrEqualTo, right, modifier, options);
        }

        public static ComparisonPredicate EqualTo(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.EqualTo, right, modifier, options);
        }

        public static ComparisonPredicate NotEqualTo(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.NotEqualTo, right, modifier, options);
        }

        public static ComparisonPredicate Matches(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.Matches, right, modifier, options);
        }

        public static ComparisonPredicate Like(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.Like, right, modifier, options);
        }

        public static ComparisonPredicate BeginsWith(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.BeginsWith, right, modifier, options);
        }

        public static ComparisonPredicate EndsWith(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.EndsWith, right, modifier, options);
        }

        public static ComparisonPredicate In(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.In, right, modifier, options);
        }

        public static ComparisonPredicate Contains(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.Contains, right, modifier, options);
        }

        public static ComparisonPredicate Between(Expr left, Expr right, ComparisonPredicateModifier modifier = ComparisonPredicateModifier.Direct, ComparisonPredicateOptions options = 0) {
            return Comparison(left, PredicateOperatorType.Between, right, modifier, options);
        }

        public PredicateOperatorType PredicateOperatorType { get; set; }
        public ComparisonPredicateModifier ComparisonPredicateModifier { get; set; }
        public Expr LeftExpression { get; set; }
        public Expr RightExpression { get; set; }

        public ComparisonPredicateOptions Options { get; set; }

        public override string Format
        {
            get
            {
                string op = "";
                string opts = "";

                switch (PredicateOperatorType)
                {
                    case PredicateOperatorType.LessThan:
                        op = "<";
                        break;
                    case PredicateOperatorType.LessThanOrEqualTo:
                        op = "<=";
                        break;
                    case PredicateOperatorType.GreaterThan:
                        op = ">";
                        break;
                    case PredicateOperatorType.GreaterThanOrEqualTo:
                        op = ">=";
                        break;
                    case PredicateOperatorType.EqualTo:
                        op = "==";
                        break;
                    case PredicateOperatorType.NotEqualTo:
                        op = "!=";
                        break;
                    case PredicateOperatorType.Matches:
                        op = "MATCHES";
                        break;
                    case PredicateOperatorType.Like:
                        op = "LIKE";
                        break;
                    case PredicateOperatorType.BeginsWith:
                        op = "BEGINSWITH";
                        break;
                    case PredicateOperatorType.EndsWith:
                        op = "ENDSWITH";
                        break;
                    case PredicateOperatorType.In:
                        op = "IN";
                        break;
                    case PredicateOperatorType.Contains:
                        op = "CONTAINS";
                        break;
                    case PredicateOperatorType.Between:
                        op = "BETWEEN";
                        break;
                }

                if (0 != (Options & ComparisonPredicateOptions.CaseInsensitive))
                {
                    opts += "c";
                }
                if (0 != (Options & ComparisonPredicateOptions.DiacriticInsensitive))
                {
                    opts += "d";
                }

                if (opts.Length > 0)
                {
                    op += $"[{opts}]";
                }

                return "(" + LeftExpression.Format + " " + op + " " + RightExpression.Format + ")";
            }
        }

        private Expression _LinqExpression(Expression left, Expression right, LinqDialect dialect)
        {
            if (0 != (Options & ComparisonPredicateOptions.CaseInsensitive))
            {
                left = Utils.CallSafe(dialect, left, "ToLower");
                if (PredicateOperatorType != PredicateOperatorType.Matches)
                {
                    right = Utils.CallSafe(dialect, right, "ToLower");
                }
            }

            switch (PredicateOperatorType)
            {
                case PredicateOperatorType.LessThan:
                    return Expression.LessThan(left, right);
                case PredicateOperatorType.LessThanOrEqualTo:
                    return Expression.LessThanOrEqual(left, right);
                case PredicateOperatorType.GreaterThan:
                    return Expression.GreaterThan(left, right);
                case PredicateOperatorType.GreaterThanOrEqualTo:
                    return Expression.GreaterThanOrEqual(left, right);
                case PredicateOperatorType.EqualTo:
                    return Expression.Equal(left, right);
                case PredicateOperatorType.NotEqualTo:
                    return Expression.NotEqual(left, right);
                case PredicateOperatorType.Matches:
                    var method = typeof(Utils).GetMethod("_Predicate_MatchesRegex", BindingFlags.Public | BindingFlags.Static);
                    return Expression.Call(method, left, right);
                case PredicateOperatorType.Like:
                    throw new NotImplementedException();
                case PredicateOperatorType.BeginsWith:
                    return Utils.CallSafe(dialect, left, "StartsWith", right);
                case PredicateOperatorType.EndsWith:
                    return Utils.CallSafe(dialect, left, "EndsWith", right);
                case PredicateOperatorType.In:
                    return Utils.CallSafe(dialect, right, "Contains", left);
                case PredicateOperatorType.Contains:
                    return Utils.CallSafe(dialect, left, "Contains", right);
                case PredicateOperatorType.Between:
                    Expression lower, upper;
                    if (right.Type.IsSubclassOf(typeof(Array)))
                    {
                        lower = Expression.ArrayIndex(right, Expression.Constant(0));
                        upper = Expression.ArrayIndex(right, Expression.Constant(1));
                    }
                    else
                    {
                        lower = Expression.Property(right, "Item", Expression.Constant(0));
                        upper = Expression.Property(right, "Item", Expression.Constant(1));
                    }
                    return Expression.AndAlso(Expression.GreaterThanOrEqual(left, lower), Expression.LessThanOrEqual(left, upper));
            }
            return null;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings, LinqDialect dialect) {
            Expression left = LeftExpression.LinqExpression(bindings, dialect);
            Expression right = RightExpression.LinqExpression(bindings, dialect);

            if (ComparisonPredicateModifier != ComparisonPredicateModifier.Direct)
            {
                ParameterExpression t = Expression.Parameter(Utils.ElementType(left.Type));
                Expression filter = Expression.Lambda(_LinqExpression(t, right, dialect), new ParameterExpression[] { t });
                if (ComparisonPredicateModifier == ComparisonPredicateModifier.All)
                {
                    Expression all = Utils.CallAggregate("All", left, filter);
                    return all;
                }
                else if (ComparisonPredicateModifier == ComparisonPredicateModifier.Any)
                {
                    Expression any = Utils.CallAggregate("Any", left, filter);
                    return any;
                }
                else
                {
                    throw new NotImplementedException($"Unhandled ComparisonPredicateModifier {ComparisonPredicateModifier}");
                }
            }
            else
            {
                return _LinqExpression(left, right, dialect);
            }
        }

        public override void Visit(IVisitor visitor)
        {
            visitor.Visit(this);
            LeftExpression.Visit(visitor);
            RightExpression.Visit(visitor);
        }
    }


}

