using System;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace RealArtists.NPredicate
{
    public class ConstantPredicate : Predicate
    {
        public bool Value { get; set; }

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

        public override Expression LinqExpression(Dictionary<string, ParameterExpression> bindings, LinqDialect dialect)
        {
            return Expression.Constant(Value);
        }
    }
}

