using System;
using Antlr4.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

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

        private dynamic _Parse(Func<NSPredicateParser, IParseTree> startRule) {
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
                    PositionalArgumentLocations[i - 1] = positional;
                    positional++;
                }
            }

            // Run the input through antlr to generate the predicate tree
            AntlrInputStream input = new AntlrInputStream(PredicateFormat);
            NSPredicateLexer lexer = Lexer = new NSPredicateLexer(input);
            CommonTokenStream tokens = new CommonTokenStream(lexer);
            NSPredicateParser parser = Parser = new NSPredicateParser(tokens);
            IParseTree tree = startRule(parser);
            ParseTreeWalker.Default.Walk(this, tree);
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

        // --- CompoundPredicate ---

        public override void ExitPredicateAnd(NSPredicateParser.PredicateAndContext context)
        {
            Predicate rhs = Stack.Pop();
            Predicate lhs = Stack.Pop();

            Stack.Push(CompoundPredicate.And(new Predicate[] { lhs, rhs }));
        }

        public override void ExitPredicateOr(NSPredicateParser.PredicateOrContext context)
        {
            Predicate rhs = Stack.Pop();
            Predicate lhs = Stack.Pop();

            Stack.Push(CompoundPredicate.Or(new Predicate[] { lhs, rhs }));
        }

        public override void ExitPredicateNot(NSPredicateParser.PredicateNotContext context)
        {
            Predicate subpred = Stack.Pop();
            Stack.Push(CompoundPredicate.Not(subpred));
        }
            

        // --- PredicateTrue/False ---

        public override void ExitPredicateTrue(NSPredicateParser.PredicateTrueContext context)
        {
            Stack.Push(Predicate.Constant(true));
        }

        public override void ExitPredicateFalse(NSPredicateParser.PredicateFalseContext context)
        {
            Stack.Push(Predicate.Constant(false));
        }

        // --- ComparisonPredicate ---

        public override void ExitComparisonPredicateNone(NSPredicateParser.ComparisonPredicateNoneContext context)
        {
            ComparisonPredicate pred = Stack.Pop();
            var subpred = ComparisonPredicate.Comparison(pred.LeftExpression, pred.PredicateOperatorType, pred.RightExpression, ComparisonPredicateModifier.Any, pred.Options);
            var not = CompoundPredicate.Not(subpred);
            Stack.Push(not);
        }

        public override void ExitComparisonPredicateAll(NSPredicateParser.ComparisonPredicateAllContext context)
        {
            ComparisonPredicate pred = Stack.Pop();
            var all = ComparisonPredicate.Comparison(pred.LeftExpression, pred.PredicateOperatorType, pred.RightExpression, ComparisonPredicateModifier.All, pred.Options);
            Stack.Push(all);
        }

        public override void ExitComparisonPredicateAny(NSPredicateParser.ComparisonPredicateAnyContext context)
        {
            ComparisonPredicate pred = Stack.Pop();
            var any = ComparisonPredicate.Comparison(pred.LeftExpression, pred.PredicateOperatorType, pred.RightExpression, ComparisonPredicateModifier.Any, pred.Options);
            Stack.Push(any);
        }

        public override void ExitComparisonPredicateSome(NSPredicateParser.ComparisonPredicateSomeContext context)
        {
            ComparisonPredicate pred = Stack.Pop();
            var any = ComparisonPredicate.Comparison(pred.LeftExpression, pred.PredicateOperatorType, pred.RightExpression, ComparisonPredicateModifier.Any, pred.Options);
            Stack.Push(any);
        }

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
            var rhs = Stack.Pop();
            var lhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("raise:toPower:", lhs, rhs));
        }

//        | expression '*' expression     # ExprMult
        public override void ExitExprMult(NSPredicateParser.ExprMultContext context)
        {
            var rhs = Stack.Pop();
            var lhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("multiply:by:", lhs, rhs));
        }

//        | expression '/' expression     # ExprDiv
        public override void ExitExprDiv(NSPredicateParser.ExprDivContext context)
        {
            var rhs = Stack.Pop();
            var lhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("divide:by:", lhs, rhs));
        }

//        | expression '+' expression     # ExprAdd
        public override void ExitExprAdd(NSPredicateParser.ExprAddContext context)
        {
            var rhs = Stack.Pop();
            var lhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("add:to:", rhs, lhs));
        }

//        | expression '-' expression     # ExprSub
        public override void ExitExprSub(NSPredicateParser.ExprSubContext context)
        {
            var rhs = Stack.Pop();
            var lhs = Stack.Pop();

            Stack.Push(Expr.MakeFunction("from:subtract:", lhs, rhs));
        }

//        | '-' expression                # ExprUnaryMinus
        public override void ExitExprUnaryMinus(NSPredicateParser.ExprUnaryMinusContext context)
        {
            var e = Stack.Pop();

            Stack.Push(Expr.MakeFunction("negate:", e));
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
            Expr rhs = Stack.Pop();
            Expr lhs = Stack.Pop();

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

        //        | 'SUBQUERY' '(' expression ',' variable ',' predicate ')'    # ExprSubquery

        public override void ExitExprSubquery([NotNull] NSPredicateParser.ExprSubqueryContext context)
        {
            var subpredicate = Stack.Pop();
            var variable = Stack.Pop();
            var collection = Stack.Pop();

            Stack.Push(Expr.MakeSubquery(collection, variable.Variable, subpredicate));
        }

        //        | IDENTIFIER '(' ')'            # ExprNoArgFunction
        public override void ExitExprNoArgFunction(NSPredicateParser.ExprNoArgFunctionContext context)
        {
            string fn = context.IDENTIFIER().GetText().ToLower();
            Stack.Push(Expr.MakeFunction(fn));
        }

        class FunctionArgSentinel { }

        //        | IDENTIFIER '(' expression_list ')'    # ExprArgFunction
        public override void EnterExprArgFunction(NSPredicateParser.ExprArgFunctionContext context)
        {
            Stack.Push(new FunctionArgSentinel());
        }

        public override void ExitExprArgFunction([NotNull] NSPredicateParser.ExprArgFunctionContext context)
        {
            var fn = context.IDENTIFIER().GetText().ToLower() + ":";
            var args = new List<Expr>();
            do
            {
                var expr = Stack.Pop();
                if (expr is Expr)
                {
                    args.Insert(0, expr);
                }
                else
                {
                    break;
                }
            } while (true);

            // Handle FUNCTION("a:b:", a, b) syntax
            if (fn == "function:")
            {
                fn = args[0].ConstantValue as string;
                args.RemoveAt(0);
            }

            Stack.Push(Expr.MakeFunction(fn, args));
        }

//        | variable ASSIGN expression            # ExprAssign
        public override void ExitExprAssign(NSPredicateParser.ExprAssignContext context) 
        {
            Expr value = Stack.Pop();
            Expr name = Stack.Pop();

            Stack.Push(Expr.MakeAssignment(name.Variable, value));
        }

        // --- ConstantExprs ---
        public override void ExitValueEmptyAggregate(NSPredicateParser.ValueEmptyAggregateContext context)
        {
            Stack.Push(Expr.MakeAggregate(new Expr[] { }));
        }

        private class AggregateSentinel { }

        public override void ExitValueAggregate(NSPredicateParser.ValueAggregateContext context)
        {
            var exprs = new List<Expr>();
            do
            {
                var expr = Stack.Pop();
                if (expr is Expr)
                {
                    exprs.Insert(0, expr);
                } else
                {
                    Debug.Assert(expr is AggregateSentinel);
                    break;
                }
            } while (true);
            
            Stack.Push(Expr.MakeAggregate(exprs));
        }

        public override void EnterValueAggregate([NotNull] NSPredicateParser.ValueAggregateContext context)
        {
            Stack.Push(new AggregateSentinel());
        }

        public override void ExitVariable([NotNull] NSPredicateParser.VariableContext context)
        {
            var identifier = "$" + context.IDENTIFIER().GetText();
            Stack.Push(Expr.MakeVariable(identifier));
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

        public override void ExitValueNull(NSPredicateParser.ValueNullContext context)
        {
            Stack.Push(Expr.MakeConstant(null));
        }

        public override void ExitValueTrue(NSPredicateParser.ValueTrueContext context)
        {
            Stack.Push(Expr.MakeConstant(true));
        }

        public override void ExitValueFalse(NSPredicateParser.ValueFalseContext context)
        {
            Stack.Push(Expr.MakeConstant(false));
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

