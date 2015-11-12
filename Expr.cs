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
        public ExpressionType ExpressionType
        {
            get; 
            protected set;
        }

        public dynamic ConstantValue
        {
            get;
            protected set;
        }

        public string KeyPath
        {
            get;
            protected set;
        }

        public string Function
        {
            get;
            protected set;
        }

        public string Variable
        {
            get;
            protected set;
        }

        public Expr Operand
        {
            get;
            protected set;
        }

        public IEnumerable<Expr> Arguments
        {
            get;
            protected set;
        }

        public Expr Collection
        {
            get;
            protected set;
        }

        public Predicate Predicate
        {
            get;
            protected set;
        }

        public Expr LeftExpression
        {
            get;
            protected set;
        }

        public Expr RightExpression
        {
            get;
            protected set;
        }
            
        public static Expr WithFormat(string format, params string[] arguments) {
            return null;
        }

        public static Expr MakeConstant(dynamic obj) {
            return new ConstantExpr(obj);
        }

        public static Expr MakeEvaluatedObject() {
            return new EvaluatedObjectExpr();
        }

        public static Expr MakeVariable(string variable) {
            return new VariableExpr(variable);
        }

        public static Expr MakeKeyPath(string keyPath) {
            return new KeyPathExpr(keyPath);
        }

        public static Expr MakeKeyPath(Expr operand, string keyPath) {
            return new KeyPathExpr(operand, keyPath);
        }

        public static Expr MakeFunction(string name, IEnumerable<Expr> arguments) {
            return new FunctionExpr(name, arguments);
        }

        public static Expr MakeUnionSet(Expr left, Expr right) {
            return new SetExpr();
        }

        public static Expr MakeIntersectSet(Expr left, Expr right) {
            return new SetExpr();
        }

        public static Expr MakeMinusSet(Expr left, Expr right) {
            return new SetExpr();
        }
            
        public static Expr MakeSubquery(Expr expr, string variable, Predicate predicate) {
            return new SubqueryExpr(expr, variable, predicate);
        }

        public static Expr MakeAggregate(IEnumerable<Expr> components) {
            return new AggregateExpr(components);
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
            ExpressionType = ExpressionType.ConstantValueExpressionType;
            ConstantValue = value;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            return Expression.Constant(ConstantValue);
        }

        public override string Format
        {
            get {
                return ConstantValue?.ToString() ?? "null";
            }
        }
    }

    class EvaluatedObjectExpr : Expr {
        public EvaluatedObjectExpr() {
            ExpressionType = ExpressionType.EvaluatedObjectExpressionType;
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
            this.ExpressionType = ExpressionType.VariableExpressionType;
            this.Variable = variable;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            return bindings[Variable];
        }

        public override string Format
        {
            get {
                return Variable;
            }
        }
    }

    class KeyPathExpr : Expr {

        public KeyPathExpr(string keyPath) {
            this.ExpressionType = ExpressionType.KeyPathExpressionType;
            this.KeyPath = keyPath;
        }

        public KeyPathExpr(Expr operand, string keyPath) {
            this.ExpressionType = ExpressionType.KeyPathExpressionType;
            this.Operand = operand;
            this.KeyPath = keyPath;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            string[] keys = KeyPath.Split('.');
            Expression result = Operand != null ? Operand.LinqExpression(bindings) : bindings["SELF"];
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
                return KeyPath;
            }
        }
    }

    class FunctionExpr : Expr {
        public FunctionExpr(string function, IEnumerable<Expr> arguments) {
            this.ExpressionType = ExpressionType.FunctionExpressionType;
            this.Function = function;
            this.Arguments = arguments;
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
            this.ExpressionType = ExpressionType.SubqueryExpressionType;
            this.Collection = collection;
            this.Variable = variableName;
            this.Predicate = predicate;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            // Generate code for
            // collection.Where(variable => predicate)

            var collectionExpression = Collection.LinqExpression(bindings);
            Type itemType = null;
            if (collectionExpression.Type.IsSubclassOf(typeof(Array))) 
            {
                itemType = Expression.ArrayIndex(collectionExpression, Expression.Constant(0)).Type;
            }
            else
            {
                itemType = collectionExpression.Type.GetGenericArguments()[0];
            }
                
            ParameterExpression varExpr = Expression.Parameter(itemType, Variable);
            var subBindings = new Dictionary<string, ParameterExpression>(bindings);
            subBindings[Variable] = varExpr;
            var p = Predicate.LinqExpression(subBindings);

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
                return $"SUBQUERY(${Collection.Format}, ${Variable}, ${Predicate}";
            }
        }
    }

    class AggregateExpr : Expr {
        public AggregateExpr(IEnumerable<Expr> components) {
            this.ExpressionType = ExpressionType.AggregateExpressionType;
            this.Arguments = components;
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            var exprs = new List<Expression>();
            Type type = typeof(object);
            foreach (var e in Arguments)
            {
                var l = e.LinqExpression(bindings);
                type = l.Type;
                exprs.Add(l);
            }
            Type listOpenType = typeof(List<>);
            Type listType = listOpenType.MakeGenericType(new Type[] { type });
            return Expression.ListInit(Expression.New(listType), exprs);
        }

        public override string Format
        {
            get {
                var delimited = String.Join(", ", Arguments.Select(a => a.Format));
                return "{ " + delimited + " }";
            }
        }
    }

}

