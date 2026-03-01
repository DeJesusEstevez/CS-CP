// DELETE THIS CONTENT IF YOU PUT COMBINED GRAMMAR IN Parser TAB
lexer grammar ExprLexer;

/* =========================
   LEXER
   ========================= */

/* --- OPERADORES LARGOS PRIMERO --- */
GTEQ      : '>=' ;
LTEQ      : '<=' ;
EQEQ      : '==' ;
NOTEQ     : '!=' ;

/* --- OPERADORES SIMPLES --- */
GT        : '>' ;
LT        : '<' ;
PLUS      : '+' ;
MINUS     : '-' ;
STAR      : '*' ;
SLASH     : '/' ;
PERCENT   : '%' ;
EQUAL     : '=' ;

/* --- SÍMBOLOS --- */
COLON     : ':' ;
SEMI      : ';' ;
QUESTION  : '?' ;
LPAREN    : '(' ;
RPAREN    : ')' ;
LBRACE    : '{' ;
RBRACE    : '}' ;
LBRACK    : '[' ;
RBRACK    : ']' ;
COMMA     : ',' ;
DOT       : '.' ;

/* --- PALABRAS RESERVADAS --- */
DECLARE   : 'declare' ;
SET       : 'set' ;
CHECK     : 'check' ;
OTHERWISE : 'otherwise' ;
REPEAT    : 'repeat' ;
LOOP      : 'loop' ;
FUNC      : 'func' ;
ENTRY     : 'entry' ;
GIVES     : 'gives' ;
OBJECT    : 'object' ;
USE       : 'use' ;
TRUE      : 'true' ;
FALSE     : 'false' ;
NULL      : 'null' ;
AND       : 'and' ;
OR        : 'or' ;
NOT       : 'not' ;

/* --- TIPOS BASE (ANTES DE IDENT) --- */
BASETYPE  : 'i' | 'f' | 's' | 'b' ;

/* --- LITERALES --- */
FLOAT_LIT : [0-9]+ '.' [0-9]+ ;
INT_LIT   : [0-9]+ ;
STRING_LIT: '"' (~["\\\r\n] | '\\' [nrtbf"'\\])* '"' ;

/* --- IDENTIFICADORES --- */
IDENT     : [A-Za-z_] [A-Za-z0-9_]* ;

/* --- IGNORAR --- */
WS            : [ \t\r\n]+ -> skip ;
COMMENT       : '/*' .*? '*/' -> skip ;
LINE_COMMENT  : '//' ~[\r\n]* -> skip ;