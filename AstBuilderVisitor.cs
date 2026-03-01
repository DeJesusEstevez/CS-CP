using System;
using System.Linq;
using Antlr4.Runtime;
using RedLangCompiler.Exceptions;
using RedLangCompiler.Nodes;
using RedLangCompiler.Enumerations;

namespace RedLangCompiler
{
    /// <summary>
    /// Construye un AST tipado a partir del parse tree generado por ANTLR.
    /// </summary>
    public class AstBuilderVisitor : RedLangBaseVisitor<AstNode>
    {
        public override AstNode VisitProgram(RedLang.ProgramContext context)
        {
            var program = new ProgramNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            foreach (var unitCtx in context.unit())
            {
                var node = Visit(unitCtx);
                switch (node)
                {
                    case UseNode use:
                        program.Uses.Add(use);
                        break;
                    case ObjectDeclNode obj:
                        program.Objects.Add(obj);
                        break;
                    case FuncDeclNode func:
                        program.Functions.Add(func);
                        break;
                }
            }

            return program;
        }

        public override AstNode VisitUnit(RedLang.UnitContext context)
        {
            if (context.use_stmt() != null) return Visit(context.use_stmt());
            if (context.object_decl() != null) return Visit(context.object_decl());
            return Visit(context.func_decl());
        }

        public override AstNode VisitUse_stmt(RedLang.Use_stmtContext context)
        {
            return new UseNode
            {
                Module = context.IDENT().GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitObject_decl(RedLang.Object_declContext context)
        {
            var obj = new ObjectDeclNode
            {
                Name = context.IDENT().GetText(),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            foreach (var elemCtx in context.class_element())
            {
                var node = Visit(elemCtx);
                switch (node)
                {
                    case FieldDeclNode field:
                        obj.Fields.Add(field);
                        break;
                    case MethodDeclNode method:
                        obj.Methods.Add(method);
                        break;
                    case ExprStmtNode exprStmt:
                        obj.Initializers.Add(exprStmt);
                        break;
                }
            }

            return obj;
        }

        public override AstNode VisitClass_element(RedLang.Class_elementContext context)
        {
            if (context.field_decl() != null) return Visit(context.field_decl());
            if (context.method_decl() != null) return Visit(context.method_decl());
            return Visit(context.expr_stmt());
        }

        public override AstNode VisitField_decl(RedLang.Field_declContext context)
        {
            return new FieldDeclNode
            {
                Name = context.IDENT().GetText(),
                Type = BuildType(context.type_spec()),
                Initializer = context.expr() != null ? BuildExpr(context.expr()) : null,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitMethod_decl(RedLang.Method_declContext context)
        {
            var method = new MethodDeclNode
            {
                Name = context.IDENT().GetText(),
                ReturnType = BuildRetType(context.ret_type()),
                Body = BuildBlock(context.block()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            if (context.paren_params()?.param() != null)
            {
                foreach (var paramCtx in context.paren_params().param())
                {
                    method.Parameters.Add(BuildParam(paramCtx));
                }
            }

            return method;
        }

        public override AstNode VisitFunc_decl(RedLang.Func_declContext context)
        {
            var func = new FuncDeclNode
            {
                IsEntry = context.ENTRY() != null,
                Name = context.IDENT().GetText(),
                ReturnType = BuildRetType(context.ret_type()),
                Body = BuildBlock(context.block()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            if (context.paren_params()?.param() != null)
            {
                foreach (var paramCtx in context.paren_params().param())
                {
                    func.Parameters.Add(BuildParam(paramCtx));
                }
            }

            return func;
        }

        public override AstNode VisitExpr_stmt(RedLang.Expr_stmtContext context)
        {
            return new ExprStmtNode
            {
                Expression = BuildCall(context.call_expr()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitGives_stmt(RedLang.Gives_stmtContext context)
        {
            return new GivesStmtNode
            {
                Expression = BuildExpr(context.expr()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitVar_decl_stmt(RedLang.Var_decl_stmtContext context)
        {
            return new VarDeclStmtNode
            {
                Name = context.IDENT().GetText(),
                Type = BuildType(context.type_spec()),
                Initializer = context.expr() != null ? BuildExpr(context.expr()) : null,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitSet_stmt(RedLang.Set_stmtContext context)
        {
            return new SetStmtNode
            {
                Target = BuildAssignTarget(context.assign_target()),
                Value = BuildExpr(context.expr()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitCheck_stmt(RedLang.Check_stmtContext context)
        {
            return new CheckStmtNode
            {
                Condition = BuildExpr(context.expr()),
                ThenBlock = BuildBlock(context.block(0)),
                ElseBlock = context.block().Length > 1 ? BuildBlock(context.block(1)) : null,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitRepeat_stmt(RedLang.Repeat_stmtContext context)
        {
            return new RepeatStmtNode
            {
                Condition = BuildExpr(context.expr()),
                Body = BuildBlock(context.block()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        public override AstNode VisitLoop_stmt(RedLang.Loop_stmtContext context)
        {
            var loop = new LoopStmtNode
            {
                Init = BuildLoopInit(context.loop_init_opt()),
                Condition = context.cond_opt()?.expr() != null ? BuildExpr(context.cond_opt().expr()) : null,
                Action = BuildLoopAction(context.loop_action_opt()),
                Body = BuildBlock(context.block()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            return loop;
        }

        public override AstNode VisitBlock(RedLang.BlockContext context)
        {
            return BuildBlock(context);
        }

        public override AstNode VisitStmt(RedLang.StmtContext context)
        {
            if (context.var_decl_stmt() != null) return Visit(context.var_decl_stmt());
            if (context.set_stmt() != null) return Visit(context.set_stmt());
            if (context.check_stmt() != null) return Visit(context.check_stmt());
            if (context.repeat_stmt() != null) return Visit(context.repeat_stmt());
            if (context.loop_stmt() != null) return Visit(context.loop_stmt());
            if (context.expr_stmt() != null) return Visit(context.expr_stmt());
            if (context.gives_stmt() != null) return Visit(context.gives_stmt());

            throw new CompilationException("Tipo de sentencia no soportado en el AST.", context.Start.Line, context.Start.Column + 1);
        }

        // Helpers
        private BlockNode BuildBlock(RedLang.BlockContext context)
        {
            var block = new BlockNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            foreach (var stmtCtx in context.stmt())
            {
                if (Visit(stmtCtx) is StatementNode stmt)
                {
                    block.Statements.Add(stmt);
                }
            }

            return block;
        }

        private ParamNode BuildParam(RedLang.ParamContext context)
        {
            return new ParamNode
            {
                Name = context.IDENT().GetText(),
                Type = BuildType(context.type_spec()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        private TypeSpecNode BuildType(RedLang.Type_specContext context)
        {
            var node = new TypeSpecNode
            {
                BaseType = BuildBaseType(context.base_type()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1,
                IsNullable = context.QUESTION() != null
            };

            if (context.INT_LIT() != null)
            {
                node.ArrayLength = int.Parse(context.INT_LIT().GetText());
            }

            return node;
        }

        private TypeSpecNode BuildRetType(RedLang.Ret_typeContext context)
        {
            return new TypeSpecNode
            {
                BaseType = BuildBaseType(context.base_type()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1,
                IsNullable = context.QUESTION() != null
            };
        }

        private BaseTypeNode BuildBaseType(RedLang.Base_typeContext context)
        {
            bool isBuiltin = context.BASETYPE() != null;
            string name = isBuiltin ? context.BASETYPE()!.GetText() : context.IDENT()!.GetText();

            return new BaseTypeNode
            {
                Name = name,
                IsBuiltin = isBuiltin,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        private StatementNode? BuildLoopInit(RedLang.Loop_init_optContext? context)
        {
            if (context == null) return null;
            if (context.declare_core() != null) return BuildDeclareCore(context.declare_core());
            if (context.set_core() != null) return BuildSetCore(context.set_core());
            return null;
        }

        private StatementNode? BuildLoopAction(RedLang.Loop_action_optContext? context)
        {
            if (context == null) return null;
            if (context.set_core() != null) return BuildSetCore(context.set_core());
            if (context.call_expr() != null)
            {
                return new ExprStmtNode
                {
                    Expression = BuildCall(context.call_expr()),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            return null;
        }

        private VarDeclStmtNode BuildDeclareCore(RedLang.Declare_coreContext context)
        {
            return new VarDeclStmtNode
            {
                Name = context.IDENT().GetText(),
                Type = BuildType(context.type_spec()),
                Initializer = context.expr() != null ? BuildExpr(context.expr()) : null,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        private SetStmtNode BuildSetCore(RedLang.Set_coreContext context)
        {
            return new SetStmtNode
            {
                Target = BuildAssignTarget(context.assign_target()),
                Value = BuildExpr(context.expr()),
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        private AssignTargetNode BuildAssignTarget(RedLang.Assign_targetContext context)
        {
            return new AssignTargetNode
            {
                Name = context.IDENT().GetText(),
                Index = context.expr() != null ? BuildExpr(context.expr()) : null,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        private ExpressionNode BuildExpr(RedLang.ExprContext context) => BuildOr(context.or_expr());

        private ExpressionNode BuildOr(RedLang.Or_exprContext context)
        {
            var expr = BuildAnd(context.and_expr(0));
            for (int i = 1; i < context.and_expr().Length; i++)
            {
                expr = new BinaryExprNode
                {
                    Op = BinaryOp.Or,
                    Left = expr,
                    Right = BuildAnd(context.and_expr(i)),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }
            return expr;
        }

        private ExpressionNode BuildAnd(RedLang.And_exprContext context)
        {
            var expr = BuildNot(context.not_expr(0));
            for (int i = 1; i < context.not_expr().Length; i++)
            {
                expr = new BinaryExprNode
                {
                    Op = BinaryOp.And,
                    Left = expr,
                    Right = BuildNot(context.not_expr(i)),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }
            return expr;
        }

        private ExpressionNode BuildNot(RedLang.Not_exprContext context)
        {
            var expr = BuildCmp(context.cmp_expr());
            if (context.NOT() != null && context.NOT().Length > 0)
            {
                foreach (var _ in context.NOT())
                {
                    expr = new UnaryExprNode
                    {
                        Op = UnaryOp.Not,
                        Operand = expr,
                        Line = context.Start.Line,
                        Column = context.Start.Column + 1
                    };
                }
            }
            return expr;
        }

        private ExpressionNode BuildCmp(RedLang.Cmp_exprContext context)
        {
            var left = BuildAdd(context.add_expr(0));
            if (context.add_expr().Length == 1) return left;

            var right = BuildAdd(context.add_expr(1));
            BinaryOp op = context switch
            {
                _ when context.EQEQ() != null => BinaryOp.Equal,
                _ when context.NOTEQ() != null => BinaryOp.NotEqual,
                _ when context.GTEQ() != null => BinaryOp.GreaterOrEqual,
                _ when context.LTEQ() != null => BinaryOp.LessOrEqual,
                _ when context.GT() != null => BinaryOp.Greater,
                _ => BinaryOp.Less
            };

            return new BinaryExprNode
            {
                Op = op,
                Left = left,
                Right = right,
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }

        private ExpressionNode BuildAdd(RedLang.Add_exprContext context)
        {
            var expr = BuildMul(context.mul_expr(0));
            int childIndex = 1; // operador entre mul_expr

            for (int i = 1; i < context.mul_expr().Length; i++)
            {
                string opText = context.GetChild(childIndex).GetText();
                childIndex += 2;

                var right = BuildMul(context.mul_expr(i));
                var op = opText switch
                {
                    "+" => BinaryOp.Add,
                    "-" => BinaryOp.Subtract,
                    _ => throw new InvalidOperationException($"Operador inesperado {opText}")
                };

                expr = new BinaryExprNode
                {
                    Op = op,
                    Left = expr,
                    Right = right,
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }
            return expr;
        }

        private ExpressionNode BuildMul(RedLang.Mul_exprContext context)
        {
            var expr = BuildUnary(context.unary_expr(0));
            int childIndex = 1;

            for (int i = 1; i < context.unary_expr().Length; i++)
            {
                string opText = context.GetChild(childIndex).GetText();
                childIndex += 2;

                var right = BuildUnary(context.unary_expr(i));
                var op = opText switch
                {
                    "*" => BinaryOp.Multiply,
                    "/" => BinaryOp.Divide,
                    "%" => BinaryOp.Modulo,
                    _ => throw new InvalidOperationException($"Operador inesperado {opText}")
                };

                expr = new BinaryExprNode
                {
                    Op = op,
                    Left = expr,
                    Right = right,
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }
            return expr;
        }

        private ExpressionNode BuildUnary(RedLang.Unary_exprContext context)
        {
            var expr = BuildPostfix(context.postfix_expr());
            if (context.MINUS() != null)
            {
                expr = new UnaryExprNode
                {
                    Op = UnaryOp.Negate,
                    Operand = expr,
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }
            return expr;
        }

        private ExpressionNode BuildPostfix(RedLang.Postfix_exprContext context)
        {
            ExpressionNode expr = BuildAtom(context.atom());

            for (int i = 0; i < context.expr().Length; i++)
            {
                expr = new IndexExprNode
                {
                    Target = expr,
                    Index = BuildExpr(context.expr(i)),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            return expr;
        }

        private ExpressionNode BuildAtom(RedLang.AtomContext context)
        {
            if (context.literal() != null) return BuildLiteral(context.literal());
            if (context.array_lit() != null) return BuildArrayLiteral(context.array_lit());
            if (context.call_expr() != null) return BuildCall(context.call_expr());
            if (context.IDENT() != null)
            {
                return new IdentifierExprNode
                {
                    Name = context.IDENT().GetText(),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            // Paréntesis
            return BuildExpr(context.expr());
        }

        private ExpressionNode BuildCall(RedLang.Call_exprContext context)
        {
            var call = new CallExprNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            foreach (var id in context.call_head().IDENT())
            {
                call.Path.Add(id.GetText());
            }

            if (context.paren_args().arg_list() != null)
            {
                foreach (var exprCtx in context.paren_args().arg_list().expr())
                {
                    call.Arguments.Add(BuildExpr(exprCtx));
                }
            }

            return call;
        }

        private ExpressionNode BuildArrayLiteral(RedLang.Array_litContext context)
        {
            var array = new ArrayLiteralNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };

            if (context.expr_list() != null)
            {
                foreach (var exprCtx in context.expr_list().expr())
                {
                    array.Elements.Add(BuildExpr(exprCtx));
                }
            }

            return array;
        }

        private ExpressionNode BuildLiteral(RedLang.LiteralContext context)
        {
            if (context.FLOAT_LIT() != null)
            {
                return new FloatLiteralNode
                {
                    Value = double.Parse(context.FLOAT_LIT().GetText()),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            if (context.INT_LIT() != null)
            {
                return new IntLiteralNode
                {
                    Value = int.Parse(context.INT_LIT().GetText()),
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            if (context.STRING_LIT() != null)
            {
                string text = context.STRING_LIT().GetText();
                // quitar comillas
                if (text.Length >= 2) text = text.Substring(1, text.Length - 2);

                return new StringLiteralNode
                {
                    Value = text,
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            if (context.TRUE() != null || context.FALSE() != null)
            {
                return new BoolLiteralNode
                {
                    Value = context.TRUE() != null,
                    Line = context.Start.Line,
                    Column = context.Start.Column + 1
                };
            }

            return new NullLiteralNode
            {
                Line = context.Start.Line,
                Column = context.Start.Column + 1
            };
        }
    }
}
