﻿using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Predicate
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

        public static ComparisonPredicate Comparison(Expr left, PredicateOperatorType op, Expr right) {
            return Comparison(left, op, right, ComparisonPredicateModifier.Direct, 0);
        }
        public static ComparisonPredicate Comparison(Expr left, PredicateOperatorType op, Expr right, ComparisonPredicateModifier modifier, ComparisonPredicateOptions options) {
            ComparisonPredicate p = new ComparisonPredicate();
            p.LeftExpression = left;
            p.PredicateOperatorType = op;
            p.RightExpression = right;
            p.ComparisonPredicateModifier = modifier;
            p.Options = options;
            return p;
        }

        public PredicateOperatorType PredicateOperatorType { get; private set; }
        public ComparisonPredicateModifier ComparisonPredicateModifier { get; private set; }
        public Expr LeftExpression { get; private set; }
        public Expr RightExpression { get; private set; }

        public ComparisonPredicateOptions Options { get; private set; }

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
                    op += $"[${opts}]";
                }

                return "(" + LeftExpression.Format + " " + op + " " + RightExpression.Format + ")";
            }
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings) {
            if (ComparisonPredicateModifier != ComparisonPredicateModifier.Direct)
            {
                // TODO: Rewrite the predicate as a subquery and return generate a LinqExpression on that instead.
                // ANY toMany.x = 'foo' => SUBQUERY(toMany, $x, $x = 'foo').@count > 0
                // ALL toMany.x = 'foo' => SUBQUERY(toMany, $x, $x = 'foo').@count = toMany.@count
                throw new NotImplementedException();
            }

            Expression left = LeftExpression.LinqExpression(bindings);
            Expression right = RightExpression.LinqExpression(bindings);

            if (0 != (Options & ComparisonPredicateOptions.CaseInsensitive))
            {
                left = Utils.CallSafe(left, "ToLower");
                if (PredicateOperatorType != PredicateOperatorType.Matches)
                {
                    right = Utils.CallSafe(right, "ToLower");
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
                case PredicateOperatorType.BeginsWith:
                    return Utils.CallSafe(left, "StartsWith", right);
                case PredicateOperatorType.EndsWith:
                    return Utils.CallSafe(left, "EndsWith", right);
                case PredicateOperatorType.In:
                    return Utils.CallSafe(right, "Contains", left);
                case PredicateOperatorType.Contains:
                    return Utils.CallSafe(left, "Contains", right);
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

        static class Utils {
            public static bool _Predicate_MatchesRegex(string s, string regex) {
                Regex r = new Regex(regex);
                return r.IsMatch(s);
            }
                            

            public static Expression CallSafe(Expression target, string methodName, params Expression[] arguments) {
                var defaultTarget = Expression.Default(target.Type);
                var isNull = Expression.ReferenceEqual(target, defaultTarget);
                var argTypes = arguments.Select(a => a.Type).ToArray();
                var called = Expression.Call(target, target.Type.GetMethod(methodName, argTypes), arguments);
                var defaultCalled = Expression.Default(called.Type);
                return Expression.Condition(isNull, defaultCalled, called);
            }
        }
    }


}

