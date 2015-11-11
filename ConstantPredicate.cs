using System;
using System.Linq.Expressions;

namespace Predicate
{
    public class ConstantPredicate : Predicate
    {
        public bool Value {
            get;
            private set;
        }

        public ConstantPredicate(bool val) {
            Value = val;
        }

        #if false
        public override bool EvaluateObject(dynamic obj)
        {
            return Value;
        } 
        #endif

        public override string Format
        {
            get
            {
                return Value ? "TRUE" : "FALSE";
            }
        }

        public override Expression LinqExpression(ParameterExpression self)
        {
            return Expression.Constant(Value);
        }
    }
}

