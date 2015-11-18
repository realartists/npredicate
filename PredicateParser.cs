using System;
using Antlr4.Runtime;
using System.Collections.Generic;
using System.Diagnostics;

namespace Predicate
{
    public class PredicateParser : NSPredicateBaseListener
    {
        public string PredicateFormat { get; private set; }
        public dynamic[] FormatArguments { get; private set; }

        public PredicateParser(string predicateFormat, params dynamic[] formatArgs)
        {
            this.PredicateFormat = predicateFormat;
            this.FormatArguments = formatArgs;
        }

        private dynamic _Parse(Action<NSPredicateParser> startRule) {
            // Figure out where all the positional arguments live in the input
            PositionalArgumentLocations.Clear();
            int positional = 0;
            bool state = false;
            for (int i = 0; i < PredicateFormat.Length; i++)
            {
                var c = PredicateFormat[i];
                if (c == '%')
                {
                    state = !state;
                }
                else if (state && (c == '@' || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))
                {
                    state = false;
                    PositionalArgumentLocations[positional] = i - 1;
                    positional++;
                }
            }

            // Run the input through antlr to generate the predicate tree
            AntlrInputStream input = new AntlrInputStream(PredicateFormat);
            NSPredicateLexer lexer = Lexer = new NSPredicateLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            NSPredicateParser parser = Parser = new NSPredicateParser(tokens);
            parser.AddParseListener(this);
            startRule(parser);
            return Stack.Pop();
        }

        public Predicate ParsePredicate() {
            return _Parse(parser => parser.predicate());
        }

        public Expr ParseExpr() {
            return _Parse(parser => parser.expression());
        }

        private NSPredicateLexer Lexer;
        private NSPredicateParser Parser;
        private Dictionary<int, int> PositionalArgumentLocations = new Dictionary<int, int>();
        private Stack<dynamic> Stack = new Stack<dynamic>();
        private Stack<PredicateOperatorType> Operators = new Stack<PredicateOperatorType>();
        private Stack<ComparisonPredicateOptions> ComparisonOptions = new Stack<ComparisonPredicateOptions>();
        private int AggregateExpressionCount = 0;

        // --- ComparisonPredicate ---

        public override void ExitUnqualifiedComparisonPredicate(NSPredicateParser.UnqualifiedComparisonPredicateContext context)
        {
            var rhs = Stack.Pop();
            var lhs = Stack.Pop();

            var pred = ComparisonPredicate.Comparison(lhs, Operators.Pop(), rhs, ComparisonPredicateModifier.Direct, ComparisonOptions.Pop());
            Stack.Push(pred);
        }

        // --- PredicateOperatorType ---

        // =
        public override void ExitOpEqualTo(NSPredicateParser.OpEqualToContext context)
        {
            Operators.Push(PredicateOperatorType.EqualTo);
        }

        // !=
        public override void ExitOpNotEqualTo(NSPredicateParser.OpNotEqualToContext context)
        {
            Operators.Push(PredicateOperatorType.NotEqualTo);
        }

        // <
        public override void ExitOpLessThan(NSPredicateParser.OpLessThanContext context)
        {
            Operators.Push(PredicateOperatorType.LessThan);
        }

        // >
        public override void ExitOpGreaterThan(NSPredicateParser.OpGreaterThanContext context)
        {
            Operators.Push(PredicateOperatorType.GreaterThan);
        }

        // <=
        public override void ExitOpLessThanOrEqualTo(NSPredicateParser.OpLessThanOrEqualToContext context) 
        {
            Operators.Push(PredicateOperatorType.LessThanOrEqualTo);
        }

        // >=
        public override void ExitOpGreaterThanOrEqualTo(NSPredicateParser.OpGreaterThanOrEqualToContext context) 
        {
            Operators.Push(PredicateOperatorType.GreaterThanOrEqualTo);
        }

//        | CONTAINS                  # OpContains
        public override void ExitOpContains(NSPredicateParser.OpContainsContext context) 
        {
            Operators.Push(PredicateOperatorType.Contains);
        }
//        | IN                        # OpIn
        public override void ExitOpIn(NSPredicateParser.OpInContext context) 
        {
            Operators.Push(PredicateOperatorType.In);
        }

//        | BEGINS_WITH               # OpBeginsWith
        public override void ExitOpBeginsWith(NSPredicateParser.OpBeginsWithContext context) 
        {
            Operators.Push(PredicateOperatorType.BeginsWith);
        }

//        | ENDS_WITH                 # OpEndsWith
        public override void ExitOpEndsWith(NSPredicateParser.OpEndsWithContext context) 
        {
            Operators.Push(PredicateOperatorType.EndsWith);
        }
//        | LIKE                      # OpLike
        public override void ExitOpLike(NSPredicateParser.OpLikeContext context) 
        {
            Operators.Push(PredicateOperatorType.Like);
        }
//        | MATCHES                   # OpMatches
        public override void ExitOpMatches(NSPredicateParser.OpMatchesContext context) 
        {
            Operators.Push(PredicateOperatorType.Matches);
        }

        public override void ExitOperatorBetween(NSPredicateParser.OperatorBetweenContext context)
        {
            ComparisonOptions.Push(0);
            Operators.Push(PredicateOperatorType.Between);
        }

        public override void ExitOperatorOptionsBare(NSPredicateParser.OperatorOptionsBareContext context) {
            ComparisonOptions.Push(0);
        }

        public override void ExitOperatorOptionsSpecified(NSPredicateParser.OperatorOptionsSpecifiedContext context) {
            string optStr = context.IDENTIFIER().GetText().ToLower();

            ComparisonPredicateOptions opts = 0;
            if (optStr.Contains("c")) {
                opts |= ComparisonPredicateOptions.CaseInsensitive;
            }
            if (optStr.Contains("d")) {
                opts |= ComparisonPredicateOptions.DiacriticInsensitive;
            }

            ComparisonOptions.Push(opts);
        }

        // --- expression ---

//        : expression '**' expression    # ExprPower
        public override void ExitExprPower(NSPredicateParser.ExprPowerContext context)
        {
            var lhs = Stack.Pop();
            var rhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("raise:toPower:", lhs, rhs));
        }

//        | expression '*' expression     # ExprMult
        public override void ExitExprMult(NSPredicateParser.ExprMultContext context)
        {
            var lhs = Stack.Pop();
            var rhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("multiply:by:", lhs, rhs));
        }

//        | expression '/' expression     # ExprDiv
        public override void ExitExprDiv(NSPredicateParser.ExprDivContext context)
        {
            var lhs = Stack.Pop();
            var rhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("divide:by:", lhs, rhs));
        }

//        | expression '+' expression     # ExprAdd
        public override void ExitExprAdd(NSPredicateParser.ExprAddContext context)
        {
            var lhs = Stack.Pop();
            var rhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("add:to:", rhs, lhs));
        }

//        | expression '-' expression     # ExprSub
        public override void ExitExprSub(NSPredicateParser.ExprSubContext context)
        {
            var lhs = Stack.Pop();
            var rhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("from:subtract:", lhs, rhs));
        }

//        | '-' expression                # ExprUnaryMinus
        public override void ExitExprUnaryMinus(NSPredicateParser.ExprUnaryMinusContext context)
        {
            var e = Stack.Pop();

            Stack.Push(Expr.MakeFunction("from:subtract:", Expr.MakeConstant(0), e));
        }

        public override void ExitExprIndex(NSPredicateParser.ExprIndexContext context)
        {
            var index = Stack.Pop();
            var indexable = Stack.Pop();

            Stack.Push(Expr.MakeFunction("objectFrom:withIndex:", indexable, index));
        }

//        | IDENTIFIER                            # ExprKeypathIdentifier
        public override void ExitExprKeypathIdentifier(NSPredicateParser.ExprKeypathIdentifierContext context)
        {
            var identifier = context.IDENTIFIER().GetText();
            Stack.Push(Expr.MakeKeyPath(identifier));
        }

//        | '@' IDENTIFIER                        # ExprKeypathAtIdentifier
        public override void ExitExprKeypathAtIdentifier(NSPredicateParser.ExprKeypathAtIdentifierContext context)
        {
            var identifier = context.IDENTIFIER().GetText();
            Debug.Assert(identifier is string);

            Stack.Push(Expr.MakeKeyPath("@" + identifier));
        }

//        | expression '.' expression             # ExprKeypathBinaryExpressions
        public override void ExitExprKeypathBinaryExpressions(NSPredicateParser.ExprKeypathBinaryExpressionsContext context)
        {
            
            Expr lhs = Stack.Pop();
            Expr rhs = Stack.Pop();

            if (!(rhs is KeyPathExpr)) {
                throw new Antlr4.Runtime.RecognitionException(
                    $"Expression to the right of '.' must be a key path expression (instead saw ${lhs.Format} '.' ${rhs.Format})",
                    Parser, Lexer.InputStream, context
                );
            }

            if (lhs is KeyPathExpr && rhs is KeyPathExpr)
            {
                Stack.Push(Expr.MakeKeyPath(lhs.Operand, lhs.KeyPath + "." + rhs.KeyPath));
            } 
            else
            {
                Stack.Push(Expr.MakeKeyPath(lhs, rhs.KeyPath));
            }
        }


        // --- ConstantExprs ---
        public override void ExitValueEmptyAggregate(NSPredicateParser.ValueEmptyAggregateContext context)
        {
            Stack.Push(Expr.MakeAggregate(new Expr[] { }));
        }

        public override void ExitValueAggregate(NSPredicateParser.ValueAggregateContext context)
        {
            var exprs = new List<Expr>();
            while (AggregateExpressionCount > 0)
            {
                exprs.Insert(0, Stack.Pop());
                AggregateExpressionCount--;
            }

            Stack.Push(Expr.MakeAggregate(exprs));
        }

        public override void ExitExprListSingle(NSPredicateParser.ExprListSingleContext context) 
        {
            AggregateExpressionCount++;
        }

        public override void ExitExprListAccum(NSPredicateParser.ExprListAccumContext context)
        {
            AggregateExpressionCount++;
        }

        public override void ExitValueString(NSPredicateParser.ValueStringContext context)
        {
            var str = context.STRING().GetText();
            str = str.Substring(1, str.Length - 2); // Strip quotes
            Stack.Push(Expr.MakeConstant(str));
        }

        public override void ExitValueNumber(NSPredicateParser.ValueNumberContext context)
        {
            var numStr = context.NUMBER().GetText();
            if (numStr.Contains("."))
            {
                double val = double.Parse(numStr);
                Stack.Push(Expr.MakeConstant(val));
            }
            else
            {
                int val = int.Parse(numStr);
                Stack.Push(Expr.MakeConstant(val));
            }
        }

        public override void ExitValueFormat(NSPredicateParser.ValueFormatContext context)
        {
            // Figure out the index of the positional argument that we have here
            int start = context.Start.StartIndex;
            int position = PositionalArgumentLocations[start];

            var format = context.FORMAT().GetText();

            if (format == "%K")
            {
                Stack.Push(Expr.MakeKeyPath(FormatArguments[position]));
            }
            else
            {
                Stack.Push(Expr.MakeConstant(FormatArguments[position]));
            }
        }
            
        public override void ExitValueSelf(NSPredicateParser.ValueSelfContext context)
        {
            Stack.Push(Expr.MakeEvaluatedObject());
        }

        // --- INDEX ---
        public override void ExitIndexFirst(NSPredicateParser.IndexFirstContext context)
        {
            Stack.Push(Expr.MakeSymbolic(SymbolicValueType.FIRST));
        }

        public override void ExitIndexLast(NSPredicateParser.IndexLastContext context)
        {
            Stack.Push(Expr.MakeSymbolic(SymbolicValueType.LAST));
        }

        public override void ExitIndexSize(NSPredicateParser.IndexSizeContext context)
        {
            Stack.Push(Expr.MakeSymbolic(SymbolicValueType.SIZE));
        }


    }
}

