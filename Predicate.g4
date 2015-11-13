/*  Adapted from Apportable's NSPredicate lexer/parser (which is MIT licensed)
    See here: https://github.com/apportable/Foundation
*/

grammar Predicate;

options {
    language=CSharp;
}   

// Parser

predicate
    : comparison_predicate
    // compound_predicate
    | predicate AND predicate
    | predicate OR predicate
    | NOT predicate
    // --
    | TRUE_PREDICATE
    | FALSE_PREDICATE
    | '(' predicate ')'
    ;
    
comparison_predicate
    : unqualified_comparison_predicate
    | ANY unqualified_comparison_predicate
    | SOME unqualified_comparison_predicate
    | ALL unqualified_comparison_predicate
    | NONE unqualified_comparison_predicate
    ;

unqualified_comparison_predicate : expression operator expression ;

operator
    : BETWEEN
    | operator_with_options
    ;

operator_with_options
    : operator_type
    | operator_type '[' IDENTIFIER ']'
    ;
    
operator_type
    : EQUAL
    | NOT_EQUAL
    | LESS_THAN
    | GREATER_THAN
    | LESS_THAN_OR_EQUAL
    | GREATER_THAN_OR_EQUAL
    | CONTAINS
    | IN
    | BEGINS_WITH
    | ENDS_WITH
    | LIKE
    | MATCHES
    ;
    
expression
    // binary
    : expression '**' expression
    | expression '*' expression
    | expression '/' expression
    | expression '+' expression
    | expression '-' expression
    | '-' expression
    // --
    | expression '[' index ']'
    | IDENTIFIER '(' ')'
    | IDENTIFIER '(' expression_list ')'
    | variable ASSIGN expression
    // keypath
    | IDENTIFIER
    | '@' IDENTIFIER
    | expression '.' expression
    // --
    | value_expression
    | '(' expression ')'
    ;
    
index
    : expression
    | FIRST
    | LAST
    | SIZE
    ;
    
value_expression
    : STRING
    | NUMBER
    | '%' format
    | variable
    | NULL
    | TRUE
    | FALSE
    | SELF
    | '{' '}'
    | '{' expression_list '}'
    ;
    
expression_list
    : expression
    | expression_list ',' expression
    ;

format
    : '@'
    | IDENTIFIER
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

NUMBER	:	'-'? INT '.' [0-9]+ EXP?
		|	'-'? INT EXP
		|	'-'? INT
		;

fragment INT	:	'0' | [1-9][0-9]* ;
fragment EXP	:	[Ee] [+\-]? INT ;

IDENTIFIER	:	[a-zA-Z_#][a-zA-Z0-9_]* ;

WS	:	[ \t\n\r]+	-> skip ;

