using System;
using System.Linq.Expressions;
using System.Collections.Generic;

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

        public override string Format
        {
            get
            {
                return Value ? "TRUE" : "FALSE";
            }
        }

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings)
        {
            return Expression.Constant(Value);
        }
    }
}

