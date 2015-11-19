/*  (Heavily) Adapted from Apportable's NSPredicate lexer/parser (which is MIT licensed)
    See here: https://github.com/apportable/Foundation
*/

grammar NSPredicate;

// Parser

predicate
    : comparison_predicate      # PredicateComparison
    // compound_predicate
    | predicate AND predicate   # PredicateAnd
    | predicate OR predicate    # PredicateOr
    | NOT predicate             # PredicateNot
    // --
    | TRUE_PREDICATE            # PredicateTrue
    | FALSE_PREDICATE           # PredicateFalse
    | '(' predicate ')'         # PredicateParens
    ;
    
comparison_predicate
    : unqualified_comparison_predicate          # ComparisonPredicateUnqualified
    | ANY unqualified_comparison_predicate      # ComparisonPredicateAny
    | SOME unqualified_comparison_predicate     # ComparisonPredicateSome
    | ALL unqualified_comparison_predicate      # ComparisonPredicateAll
    | NONE unqualified_comparison_predicate     # ComparisonPredicateNone
    ;

unqualified_comparison_predicate : expression operator expression # UnqualifiedComparisonPredicate
                                 ;

operator
    : BETWEEN                   # OperatorBetween
    | operator_with_options     # OperatorOptions
    ;

operator_with_options
    : operator_type             # OperatorOptionsBare
    | operator_type '[' IDENTIFIER ']' # OperatorOptionsSpecified
    ;
    
operator_type
    : EQUAL                     # OpEqualTo
    | NOT_EQUAL                 # OpNotEqualTo
    | LESS_THAN                 # OpLessThan
    | GREATER_THAN              # OpGreaterThan
    | LESS_THAN_OR_EQUAL        # OpLessThanOrEqualTo
    | GREATER_THAN_OR_EQUAL     # OpGreaterThanOrEqualTo
    | CONTAINS                  # OpContains
    | IN                        # OpIn
    | BEGINS_WITH               # OpBeginsWith
    | ENDS_WITH                 # OpEndsWith
    | LIKE                      # OpLike
    | MATCHES                   # OpMatches
    ;
    
expression
    : '-' expression                # ExprUnaryMinus
    // binary
    | expression '**' expression    # ExprPower
    | expression '*' expression     # ExprMult
    | expression '/' expression     # ExprDiv
    | expression '+' expression     # ExprAdd
    | expression '-' expression     # ExprSub
    // --
    | expression '[' index ']'      # ExprIndex
    | 'SUBQUERY' '(' expression ',' variable ',' predicate ')'    # ExprSubquery
    | IDENTIFIER '(' ')'            # ExprNoArgFunction
    | IDENTIFIER '(' expression_list ')'    # ExprArgFunction
    | variable ASSIGN expression            # ExprAssign
    // keypath
    | IDENTIFIER                            # ExprKeypathIdentifier
    | '@' IDENTIFIER                        # ExprKeypathAtIdentifier
    | expression '.' expression             # ExprKeypathBinaryExpressions
    // --
    | value_expression                      # ExprConstant
    | '(' expression ')'                    # ExprParens
    ;
    
index
    : expression    # IndexExpr
    | FIRST         # IndexFirst
    | LAST          # IndexLast
    | SIZE          # IndexSize
    ;
    
value_expression
    : STRING        # ValueString
    | NUMBER        # ValueNumber
    | FORMAT        # ValueFormat
    | variable      # ValueVariable
    | NULL          # ValueNull
    | TRUE          # ValueTrue
    | FALSE         # ValueFalse
    | SELF          # ValueSelf
    | '{' '}'       # ValueEmptyAggregate
    | '{' expression_list '}'   # ValueAggregate
    ;
    
expression_list
    : expression    # ExprListSingle
    | expression_list ',' expression # ExprListAccum
    ;
    
variable
    : '$' IDENTIFIER
    ;


    
// Lexer

TRUE_PREDICATE : 'TRUEPREDICATE' ;
FALSE_PREDICATE : 'FALSEPREDICATE' ;

AND : 'and' | 'AND' | '&&' ;
OR : 'or' | 'OR' | '||' ;
NOT : 'not' | 'NOT' | '!' ;

EQUAL : '=' | '==' ;
NOT_EQUAL : '!=' | '<>' ;
LESS_THAN : '<' ;
GREATER_THAN : '>' ;
LESS_THAN_OR_EQUAL : '<=' ;
GREATER_THAN_OR_EQUAL : '>=' ;

BETWEEN : 'between' | 'BETWEEN' ;
CONTAINS : 'contains' | 'CONTAINS' ;
IN : 'in' | 'IN' ;

BEGINS_WITH : 'beginswith' | 'BEGINSWITH' ;
ENDS_WITH : 'endswith' | 'ENDSWITH' ;
LIKE : 'like' | 'LIKE' ;
MATCHES : 'matches' | 'MATCHES' ;

ANY : 'any' | 'ANY' ;
ALL : 'all' | 'ALL' ;
NONE : 'none' | 'NONE' ;
SOME : 'some' | 'SOME' ;

NULL : 'null' | 'NULL' | 'nil' | 'Nil' ;
TRUE : 'true' | 'TRUE' | 'yes' | 'YES' ;
FALSE : 'false' | 'FALSE' | 'no' | 'NO' ;
SELF : 'self' | 'SELF' ;

FIRST : 'first' | 'FIRST' ;
LAST : 'last' | 'LAST' ;
SIZE : 'size' | 'SIZE' ;

ASSIGN : ':=' ;

STRING : DOUBLE_QUOTED_STRING | SINGLE_QUOTED_STRING ;

DOUBLE_QUOTED_STRING	:	'"' (ESC | ~["\\])* '"' ;
SINGLE_QUOTED_STRING	:	'\'' (ESC | ~[\'\\])* '\'' ;
fragment ESC	:	'\\' (["\'\\/bfnrt] | UNICODE) ;
fragment UNICODE	:	'u' HEX HEX HEX HEX ;
fragment HEX	:	[0-9a-fA-F] ;

FORMAT
    : '%@'
    | '%K'
    | '%d'
    | '%ld'
    | '%s'
    | '%f'
    | '%lf'
    ;


NUMBER	:	INT '.' [0-9]+ EXP?
		|	INT EXP
		|	INT
		;

fragment INT	:	'0' | [1-9][0-9]* ;
fragment EXP	:	[Ee] [+\-]? INT ;

IDENTIFIER	:	[a-zA-Z_#][a-zA-Z0-9_]* ;

WS	:	[ \t\n\r]+	-> skip ;

