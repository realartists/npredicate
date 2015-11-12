using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace Predicate
{
    class Utils {
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

