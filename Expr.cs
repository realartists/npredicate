using System;
using System.Collections.Generic;

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

    class ConstantExpr : Expr {
        public ConstantExpr(dynamic value) {
            expressionType = ExpressionType.ConstantValueExpressionType;
            constantValue = value;
        }
    }

    class EvaluatedObjectExpr : Expr {
        public EvaluatedObjectExpr() {
            expressionType = ExpressionType.EvaluatedObjectExpressionType;
        }
    }

    class VariableExpr : Expr {
        public VariableExpr(string variable) {
            this.expressionType = ExpressionType.VariableExpressionType;
            this.variable = variable;
        }
    }

    class KeyPathExpr : Expr {

    }

    class FunctionExpr : Expr {

    }

    class SetExpr : Expr {

    }

    class SubqueryExpr : Expr {

    }

    class AggregateExpr : Expr {

    }

    public class Expr
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
            Expr e = new Expr();
            e.expressionType = ExpressionType.ConstantValueExpressionType;
            e.constantValue = obj;
            return e;
        }

        public static Expr EvaluatedObject() {
            Expr e = new Expr();
            e.expressionType = ExpressionType.EvaluatedObjectExpressionType;
            return e;
        }

        // Pulls from the variable bindings dictionary
        public static Expr Variable(string variable) {
            Expr e = new Expr();
            e.expressionType = ExpressionType.VariableExpressionType;
            e.variable = variable;
            return e;
        }

        public static Expr KeyPath(string keyPath) {
            Expr e = new Expr();
            e.expressionType = ExpressionType.KeyPathExpressionType;
            e.keyPath = keyPath;
            return e;
        }

        public static Expr Function(string name, IEnumerable<Expr> arguments) {
            Expr e = new Expr();
            e.expressionType = ExpressionType.FunctionExpressionType;
            e.arguments = arguments;
            return e;
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
            
        protected Expr()
        {
        }
    }


}

