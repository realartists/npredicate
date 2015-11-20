using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Data.Entity;

namespace Predicate
{
    class Utils {

        [DbFunction("CodeFirstDatabaseSchema", "_Predicate_MatchesRegex")]
        public static bool _Predicate_MatchesRegex(string s, string regex) {
            Regex r = new Regex(regex);
            return r.IsMatch(s);
        }

        public static Expression CallSafe(LinqDialect dialect, Expression target, string methodName, params Expression[] arguments) {
            if (dialect == LinqDialect.Objects) {
                var defaultTarget = Expression.Default(target.Type);
                var isNull = Expression.ReferenceEqual(target, defaultTarget);
                var argTypes = arguments.Select(a => a.Type).ToArray();
                var called = Expression.Call(target, target.Type.GetMethod(methodName, argTypes), arguments);
                var defaultCalled = Expression.Default(called.Type);
                return Expression.Condition(isNull, defaultCalled, called);
            } else if (dialect == LinqDialect.EntityFramework)
            {
                var argTypes = arguments.Select(a => a.Type).ToArray();
                return Expression.Call(target, target.Type.GetMethod(methodName, argTypes), arguments);

            } else
            {
                throw new NotImplementedException($"Unknown dialect {dialect}");
            }
        }

        public static Expression CallAggregate(string aggregate, params Expression[] args) {
            Debug.Assert(args.Length > 0);
            List<Type> types = new List<Type>();
            types.AddRange(args.Select(e => e.Type));
            var aggregateMethod = typeof(System.Linq.Enumerable).GetMethod(aggregate, types.ToArray());
            if (aggregateMethod == null)
            {
                List<Type> typeArguments = new List<Type>();
                typeArguments.Add(ElementType(types[0]));
                types[0] = typeof(IEnumerable<>);
                for (var i = 1; i < types.Count; i++)
                {
                    if (types[i].IsGenericType)
                    {
                        foreach (var specific in types[i].GetGenericArguments()) {
                            if (!typeArguments.Contains(specific))
                            {
                                typeArguments.Add(specific);
                            }
                        }
                        types[i] = types[i].GetGenericTypeDefinition();
                    }
                }
                var aggregateOpenMethod = GetGenericMethod(typeof(System.Linq.Enumerable), aggregate, types.ToArray());
                aggregateMethod = aggregateOpenMethod.MakeGenericMethod(typeArguments.Take(aggregateOpenMethod.GetGenericArguments().Length).ToArray());
            }
            return Expression.Call(aggregateMethod, args);
        }

        public static Type ElementType(Type enumerableType)
        {
            if (enumerableType.GetGenericArguments().Length > 0)
            {
                return enumerableType.GetGenericArguments()[0];
            }
            else
            {
                return enumerableType.GetElementType();
            }
        }

        public static bool TypeIsEnumerable(Type type)
        {
            // http://stackoverflow.com/questions/1121834/finding-out-if-a-type-implements-a-generic-interface

            // this conditional is necessary if myType can be an interface,
            // because an interface doesn't implement itself: for example,
            // typeof (IList<int>).GetInterfaces () does not contain IList<int>!
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return true;
            }

            foreach (var i in type.GetInterfaces ())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return true;
                }
            }

            return false;
        }

        public static Expression CallMath(string fn, params Expression[] args) {
            var mathMethod = typeof(System.Math).GetMethod(fn, args.Select(x => x.Type).ToArray());
            return Expression.Call(mathMethod, args);
        }

        private static readonly Func<MethodInfo, IEnumerable<Type>> ParameterTypeProjection = 
            method => method.GetParameters()
                .Select(p => p.ParameterType.ContainsGenericParameters ? p.ParameterType.GetGenericTypeDefinition() : p.ParameterType);

        public static MethodInfo GetGenericMethod(Type type, string name, params Type[] parameterTypes)
        {
            foreach (var method in type.GetMethods())
            {
                if (method.Name == name)
                {
                    var projection = ParameterTypeProjection(method);
                    if (parameterTypes.SequenceEqual(projection))
                    {
                        return method;
                    }
                }
            }
            return null;

#if false
            return (from method in type.GetMethods()
            where method.Name == name
            where parameterTypes.SequenceEqual(ParameterTypeProjection(method))
            select method).SingleOrDefault();
#endif
        }

        public static bool IsTypeNumeric(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        public static DateTime GetReferenceDate()
        {
            DateTime reference = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return reference;
        }

        public static double TimeIntervalSinceReferenceDate(DateTime dateTime)
        {
            var reference = GetReferenceDate();
            var span = dateTime - reference;
            return span.TotalSeconds;
        }

        public static DateTime DateTimeFromTimeIntervalSinceReferenceDate(double timeInterval)
        {
            var reference = GetReferenceDate();
            var span = TimeSpan.FromSeconds(timeInterval);
            var dateTime = reference + span;
            return dateTime;
        }

        public static Expression AsDouble(Expression a)
        {
            if (a.Type == typeof(double))
            {
                return a;
            } else
            {
                return Expression.Convert(a, typeof(double));
            }
        }

        public static Expression AsInt(Expression a)
        {
            if (a.Type == typeof(int))
            {
                return a;
            } else
            {
                return Expression.Convert(a, typeof(int));
            }
        }

        public static Expression AsNullable(Expression a)
        {
            if (a.Type.IsByRef)
            {
                return a;
            } else if (a.Type.IsGenericType && a.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return a;
            } else
            {
                var type = typeof(Nullable<>).MakeGenericType(a.Type);
                return Expression.Convert(a, type);
            }
        }

        public static Expression AsNotNullable(Expression a)
        {
            if (a.Type.IsGenericType && a.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var type = a.Type.GetGenericArguments()[0];
                return Expression.Convert(a, type); 
            } else
            {
                return a;
            }
        }
    }
}

