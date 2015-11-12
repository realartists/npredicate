using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Predicate
{
    public enum ExpressionType {
        ConstantValueExpressionType = 0, // Expression that always returns the same value
        EvaluatedObjectExpressionType, // Expression that always returns the parameter object itself
        VariableExpressionType, // Expression that always returns whatever is stored at 'variable' in the bindings dictionary
        KeyPathExpressionType, // Expression that returns something that can be used as a key path
        FunctionExpressionType, // Expression that returns the result of evaluating a symbol
        UnionSetExpressionType, // Expression that returns the result of doing a unionSet: on two expressions that evaluate to flat collections (arrays or sets)
        IntersectSetExpressionType, // Expression that returns the result of doing an intersectSet: on two expressions that evaluate to flat collections (arrays or sets)
        MinusSetExpressionType, // Expression that returns the result of doing a minusSet: on two expressions that evaluate to flat collections (arrays or sets)
        SubqueryExpressionType = 13,
        AggregateExpressionType = 14,
    }       

    public abstract class Expr
    {
        public ExpressionType expressionType
        {
            get; 
            protected set;
        }

        public dynamic constantValue
        {
            get;
            protected set;
        }

        public string keyPath
        {
            get;
            protected set;
        }

        public string function
        {
            get;
            protected set;
        }

        public string variable
        {
            get;
            protected set;
        }

        public Expr operand
        {
            get;
            protected set;
        }

        public IEnumerable<Expr> arguments
        {
            get;
            protected set;
        }

        public Expr collection
        {
            get;
            protected set;
        }

        public Predicate predicate
        {
            get;
            protected set;
        }

        public Expr leftExpression
        {
            get;
            protected set;
        }

        public Expr rightExpression
        {
            get;
            protected set;
        }
            
        public static Expr WithFormat(string format, params string[] arguments) {
            return null;
        }

        public static Expr Constant(dynamic obj) {
            return new ConstantExpr(obj);
        }

        public static Expr EvaluatedObject() {
            return new EvaluatedObjectExpr();
        }

        public static Expr Variable(string variable) {
            return new VariableExpr(variable);
        }

        public static Expr KeyPath(string keyPath) {
            return new KeyPathExpr(keyPath);
        }

        public static Expr KeyPath(Expr operand, string keyPath) {
            return new KeyPathExpr(operand, keyPath);
        }

        public static Expr Function(string name, IEnumerable<Expr> arguments) {
            return new FunctionExpr(name, arguments);
        }

        public static Expr Aggregate(IEnumerable<Expr> subexpressions) {
            return new AggregateExpr();
        }

        public static Expr UnionSet(Expr left, Expr right) {
            return new SetExpr();
        }

        public static Expr IntersectSet(Expr left, Expr right) {
            return new SetExpr();
        }

        public static Expr MinusSet(Expr left, Expr right) {
            return new SetExpr();
        }
            
        public static Expr Subquery(Expr expr, string variable, Predicate predicate) {
            return new SubqueryExpr(expr, variable, predicate);
        }

        public Expression<Func<T, V>> LinqExpression<T, V>() {
            var bindings = new Dictionary<string, ParameterExpression>();
            ParameterExpression self = Expression.Parameter(typeof(T), "SELF");
            bindings.Add(self.Name, self);
            return Expression.Lambda<Func<T, V>>(LinqExpression(bindings), self);
        }

        public V ValueWithObject<T,V>(T obj) {
            var expr = LinqExpression<T, V>();
            Func<T, V> func = expr.Compile();
            return func(obj);
        }

        public abstract Expression LinqExpression(Dictionary<string, ParameterExpression> bindings);

        public abstract string Format { get; }

        protected Expr() { }

        private static readonly Func<MethodInfo, IEnumerable<Type>> ParameterTypeProjection = 
            method => method.GetParameters()
                .Select(p => p.ParameterType.GetGenericTypeDefinition());

        protected static MethodInfo GetGenericMethod(Type type, string name, params Type[] parameterTypes)
        {
            return (from method in type.GetMethods()
                where method.Name == name
                where parameterTypes.SequenceEqual(ParameterTypeProjection(method))
                select method).SingleOrDefault();
        }
    }

    class ConstantExpr : Expr {
        public ConstantExpr(dynamic value) {
            expressionType = ExpressionType.ConstantValueExpressionType;
            constantValue = value;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            return Expression.Constant(constantValue);
        }

        public override string Format
        {
            get {
                return constantValue?.ToString() ?? "null";
            }
        }
    }

    class EvaluatedObjectExpr : Expr {
        public EvaluatedObjectExpr() {
            expressionType = ExpressionType.EvaluatedObjectExpressionType;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            return bindings["SELF"];
        }

        public override string Format
        {
            get {
                return "SELF";
            }
        }
    }

    class VariableExpr : Expr {
        public VariableExpr(string variable) {
            this.expressionType = ExpressionType.VariableExpressionType;
            this.variable = variable;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            return bindings[variable];
        }

        public override string Format
        {
            get {
                return variable;
            }
        }
    }

    class KeyPathExpr : Expr {

        public KeyPathExpr(string keyPath) {
            this.expressionType = ExpressionType.KeyPathExpressionType;
            this.keyPath = keyPath;
        }

        public KeyPathExpr(Expr operand, string keyPath) {
            this.expressionType = ExpressionType.KeyPathExpressionType;
            this.operand = operand;
            this.keyPath = keyPath;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            string[] keys = keyPath.Split('.');
            Expression result = operand != null ? operand.LinqExpression(bindings) : bindings["SELF"];
            foreach (string key in keys) {
                if (key.ToLower() == "self") {
                    continue;
                }

                Expression propertyExpr = null;

                // Keypath evaluations in Cocoa are always safe.
                // That is, if any point along the list is nil,
                // The whole thing collapses down to nil.

                // This is the equivalent in C# of writing
                // a?.b?.c?.d

                if (key == "@count") // Special case @count, which is actually a method, not a property (Length is a property, but it isn't available on IEnumerable, which is what we probably have)
                {
                    Type itemType;
                    if (result.Type.IsSubclassOf(typeof(Array))) 
                    {
                        itemType = Expression.ArrayIndex(result, Expression.Constant(0)).Type;
                    }
                    else
                    {
                        itemType = result.Type.GetGenericArguments()[0];
                    }

                    Type arg0OpenType = typeof(IEnumerable<>);

                    var countOpenMethod = GetGenericMethod(typeof(System.Linq.Enumerable), "Count", new Type[] { arg0OpenType });
                    var countMethod = countOpenMethod.MakeGenericMethod(new Type[] { itemType });

                    propertyExpr = Expression.Call(null, countMethod, result);
                }
                else
                {
                    propertyExpr = Expression.Property(result, key);
                }

                var defaultSource = Expression.Default(result.Type);
                var defaultResult = Expression.Default(propertyExpr.Type);

                var isNilExpr = Expression.Equal(result, defaultSource);
                var conditionExpr = Expression.Condition(isNilExpr, defaultResult, propertyExpr);

                result = conditionExpr;
            }

            return result;
        }

        public override string Format
        {
            get {
                return keyPath;
            }
        }
    }

    class FunctionExpr : Expr {
        public FunctionExpr(string function, IEnumerable<Expr> arguments) {
            this.expressionType = ExpressionType.FunctionExpressionType;
            this.function = function;
            this.arguments = arguments;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            throw new NotImplementedException();
        }

        public override string Format
        {
            get {
                throw new NotImplementedException();
            }
        }
    }

    class SetExpr : Expr {
        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            throw new NotImplementedException();
        }

        public override string Format
        {
            get {
                throw new NotImplementedException();
            }
        }
    }

    class SubqueryExpr : Expr {
        public SubqueryExpr(Expr collection, string variableName, Predicate predicate) {
            this.expressionType = ExpressionType.SubqueryExpressionType;
            this.collection = collection;
            this.variable = variableName;
            this.predicate = predicate;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            // Generate code for
            // collection.Where(variable => predicate)

            var collectionExpression = collection.LinqExpression(bindings);
            Type itemType = null;
            if (collectionExpression.Type.IsSubclassOf(typeof(Array))) 
            {
                itemType = Expression.ArrayIndex(collectionExpression, Expression.Constant(0)).Type;
            }
            else
            {
                itemType = collectionExpression.Type.GetGenericArguments()[0];
            }
                
            ParameterExpression varExpr = Expression.Parameter(itemType, variable);
            var subBindings = new Dictionary<string, ParameterExpression>(bindings);
            subBindings[variable] = varExpr;
            var p = predicate.LinqExpression(subBindings);

            Type arg0OpenType = typeof(IEnumerable<>);
            //Type arg0Type = arg0OpenType.MakeGenericType(new Type[] { itemType });

            Type arg1OpenType = typeof(Func<,>);
            Type arg1Type = arg1OpenType.MakeGenericType(new Type[] { itemType, typeof(Boolean) });

            var whereOpenMethod = GetGenericMethod(typeof(System.Linq.Enumerable), "Where", new Type[] { arg0OpenType, arg1OpenType });
            var whereMethod = whereOpenMethod.MakeGenericMethod(new Type[] { itemType });

            var lambda = Expression.Lambda(arg1Type, p, new ParameterExpression[] { varExpr });
            var whereExpression = Expression.Call(null, whereMethod, collectionExpression, lambda);

            return whereExpression;
        }

        public override string Format
        {
            get {
                return $"SUBQUERY(${collection.Format}, ${variable}, ${predicate}";
            }
        }
    }

    class AggregateExpr : Expr {
        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            throw new NotImplementedException();
        }

        public override string Format
        {
            get {
                throw new NotImplementedException();
            }
        }
    }

}

