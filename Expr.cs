using System;
using System.Collections.Generic;
using System.Linq.Expressions;

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

        public IEnumerable<Expr> arguments
        {
            get;
            protected set;
        }

        public dynamic collection
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

        // Pulls from the variable bindings dictionary
        public static Expr Variable(string variable) {
            return new VariableExpr(variable);
        }

        public static Expr KeyPath(string keyPath) {
            return new KeyPathExpr(keyPath);
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
            
        public static Expr Subquery(Expr expr, string iterator, Predicate predicate) {
            return new SubqueryExpr();
        }

        public Expression<Func<T, dynamic>> LinqExpression<T>() {
            ParameterExpression self = Expression.Parameter(typeof(T), "SELF");
            return Expression.Lambda<Func<T, dynamic>>(LinqExpression(self), self);
        }

        public V ValueWithObject<T,V>(T obj) {
            var expr = LinqExpression<T>();
            Func<T, V> func = expr.Compile();
            return func(obj);
        }

        protected abstract Expression LinqExpression(ParameterExpression self);

        protected Expr() { }
    }

    class ConstantExpr : Expr {
        public ConstantExpr(dynamic value) {
            expressionType = ExpressionType.ConstantValueExpressionType;
            constantValue = value;
        }

        protected override Expression LinqExpression(ParameterExpression self)
        {
            return Expression.Constant(constantValue);
        }
    }

    class EvaluatedObjectExpr : Expr {
        public EvaluatedObjectExpr() {
            expressionType = ExpressionType.EvaluatedObjectExpressionType;
        }

        protected override Expression LinqExpression(ParameterExpression self)
        {
            return self;
        }
    }

    class VariableExpr : Expr {
        public VariableExpr(string variable) {
            this.expressionType = ExpressionType.VariableExpressionType;
            this.variable = variable;
        }

        protected override Expression LinqExpression(ParameterExpression self)
        {
            throw new NotImplementedException();
        }
    }

    class KeyPathExpr : Expr {
        public KeyPathExpr(string keyPath) {
            this.expressionType = ExpressionType.KeyPathExpressionType;
            this.keyPath = keyPath;
        }

        protected override Expression LinqExpression(ParameterExpression self)
        {
            string[] keys = keyPath.Split('.');
            Expression result = self;
            foreach (string key in keys) {
                if (key.ToLower() == "self") {
                    continue;
                }

                var propertyExpr = Expression.Property(result, key);

                var defaultSource = Expression.Default(result.Type);
                var defaultResult = Expression.Default(propertyExpr.Type);

                var isNilExpr = Expression.Equal(result, defaultSource);
                var conditionExpr = Expression.Condition(isNilExpr, defaultResult, propertyExpr);

                result = conditionExpr;
            }

            return result;
        }
    }

    class FunctionExpr : Expr {
        public FunctionExpr(string function, IEnumerable<Expr> arguments) {
            this.expressionType = ExpressionType.FunctionExpressionType;
            this.function = function;
            this.arguments = arguments;
        }

        protected override Expression LinqExpression(ParameterExpression self)
        {
            throw new NotImplementedException();
        }
    }

    class SetExpr : Expr {
        protected override Expression LinqExpression(ParameterExpression self)
        {
            throw new NotImplementedException();
        }
    }

    class SubqueryExpr : Expr {
        protected override Expression LinqExpression(ParameterExpression self)
        {
            throw new NotImplementedException();
        }
    }

    class AggregateExpr : Expr {
        protected override Expression LinqExpression(ParameterExpression self)
        {
            throw new NotImplementedException();
        }
    }

}

