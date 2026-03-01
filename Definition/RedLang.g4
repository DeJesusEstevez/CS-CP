parser grammar RedLang;

options { tokenVocab=ExprLexer; }

/* =========================
   PARSER
   ========================= */

program : unit+ EOF ;

unit
    : use_stmt
    | object_decl
    | func_decl
    ;

use_stmt
    : USE IDENT SEMI
    ;

object_decl
    : OBJECT IDENT LBRACE class_element* RBRACE
    ;

class_element
    : field_decl
    | method_decl
    | expr_stmt
    ;

field_decl
    : DECLARE IDENT COLON type_spec (EQUAL expr)? SEMI
    ;

method_decl
    : FUNC IDENT paren_params COLON ret_type block
    ;

func_decl
    : ENTRY? FUNC IDENT paren_params COLON ret_type block
    ;

paren_params
    : LPAREN (param (COMMA param)*)? RPAREN
    ;

param
    : IDENT COLON type_spec
    ;

type_spec
    : base_type (LBRACK INT_LIT RBRACK)? QUESTION?
    ;

ret_type
    : base_type QUESTION?
    ;

base_type
    : BASETYPE
    | IDENT
    ;

block
    : LBRACE stmt* RBRACE
    ;

stmt
    : var_decl_stmt
    | set_stmt
    | check_stmt
    | repeat_stmt
    | loop_stmt
    | expr_stmt
    | gives_stmt
    ;

var_decl_stmt
    : DECLARE IDENT COLON type_spec (EQUAL expr)? SEMI
    ;

set_stmt
    : SET assign_target EQUAL expr SEMI
    ;

assign_target
    : IDENT (LBRACK expr RBRACK)?
    ;

expr_stmt
    : call_expr SEMI
    ;

gives_stmt
    : GIVES expr SEMI
    ;

check_stmt
    : CHECK LPAREN expr RPAREN block (OTHERWISE block)?
    ;

repeat_stmt
    : REPEAT LPAREN expr RPAREN block
    ;

loop_stmt
    : LOOP LPAREN loop_init_opt SEMI cond_opt SEMI loop_action_opt SEMI RPAREN block
    ;

loop_init_opt
    : declare_core
    | set_core
    | /* empty */
    ;

cond_opt
    : expr
    | /* empty */
    ;

loop_action_opt
    : set_core
    | call_expr
    | /* empty */
    ;

declare_core
    : DECLARE IDENT COLON type_spec (EQUAL expr)?
    ;

set_core
    : SET assign_target EQUAL expr
    ;

/* =========================
   EXPRESSIONS
   ========================= */

expr : or_expr ;

or_expr
    : and_expr (OR and_expr)*
    ;

and_expr
    : not_expr (AND not_expr)*
    ;

not_expr
    : NOT* cmp_expr
    ;

cmp_expr
    : add_expr ((EQEQ | NOTEQ | GTEQ | LTEQ | GT | LT) add_expr)?
    ;

add_expr
    : mul_expr ((PLUS | MINUS) mul_expr)*
    ;

mul_expr
    : unary_expr ((STAR | SLASH | PERCENT) unary_expr)*
    ;

unary_expr
    : MINUS? postfix_expr
    ;

postfix_expr
    : atom (LBRACK expr RBRACK)*
    ;

atom
    : literal
    | array_lit
    | call_expr
    | IDENT
    | LPAREN expr RPAREN
    ;

call_expr
    : call_head paren_args
    ;

call_head
    : IDENT (DOT IDENT)*
    ;

paren_args
    : LPAREN (arg_list)? RPAREN
    ;

arg_list
    : expr (COMMA expr)*
    ;

array_lit
    : LBRACK (expr_list)? RBRACK
    ;

expr_list
    : expr (COMMA expr)*
    ;

literal
    : FLOAT_LIT
    | INT_LIT
    | STRING_LIT
    | TRUE
    | FALSE
    | NULL
    ;


