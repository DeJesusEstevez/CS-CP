using System;
using System.IO;
using System.Linq;
using Antlr4.Runtime;
using RedLangCompiler.Nodes;
//sarah estuvo aqui

namespace RedLangCompiler
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string code = args.Length > 0 && File.Exists(args[0])
                ? File.ReadAllText(args[0])
                : SampleCode();

            // Stream de entrada para el lexer
            AntlrInputStream inputStream = new(code);

            // Lexer & token stream generados por ANTLR
            ExprLexer lexer = new(inputStream);
            CommonTokenStream tokenStream = new(lexer);

            // Parser generado por ANTLR a partir de la gramática
            RedLang parser = new(tokenStream);
            RedLang.ProgramContext tree = parser.program();

            // Visitor que construye el AST
            AstBuilderVisitor visitor = new();
            var ast = (ProgramNode)visitor.Visit(tree);

            // Imprimir el AST resultante
            PrintAst(ast);
        }

        private static string SampleCode() =>
@"use System;
use Generics;

object Program
{
	entry func Main():i
	{
		declare z:s;
		declare x:i = 5;
		declare y:i = 3;

		loop (declare j:i = 0; j < 10; set j = j + 1)
		{
			show(j + y);
		}

		check (x > y)
		{
			show(""La variable x es mas grande que la variable y"");
		}
		otherwise
		{
			show(""La variable y es mas grande que la variable x"");
		}

		repeat (x < 7)
		{
			show(""While loop"");
			set x = x + 1;
		}

		show(""Suma:"");
		show(x + y);

		show(""Resta:"");
		show(x - y);

		show(""Multiplicación:"");
		show(x * y);

		show(""División:"");
		show(x / y);

		show(""Módulo:"");
		show(x % y)

		show(""Ingresa un dato:"");
		ask(z);

		show(""Ingresaste:"");
		show(z);
		
		declare obj:Math = Math();

		obj.suma(x, y);

		declare test:i = obj.suma(x, y);

		set x = obj.suma(x, y);
		
		show(""Factorial:"");
		show(obj.factorial(x));
	}
}

object Math
{
	func suma(a:i, c:i):i
	{
		gives a + c;
	}
	
	func factorial(num:i):i
	{
		check (num == 1)
		{
			gives num;
		}
		
		gives num * factorial(num - 1);
	}
}";

        private static void PrintAst(AstNode? node, int indent = 0)
        {
            if (node == null) return;
            string pad = new(' ', indent * 2);

            switch (node)
            {
                case ProgramNode program:
                    Console.WriteLine($"{pad}Program");
                    foreach (var use in program.Uses) PrintAst(use, indent + 1);
                    foreach (var obj in program.Objects) PrintAst(obj, indent + 1);
                    foreach (var func in program.Functions) PrintAst(func, indent + 1);
                    break;

                case UseNode use:
                    Console.WriteLine($"{pad}Use {use.Module}");
                    break;

                case ObjectDeclNode obj:
                    Console.WriteLine($"{pad}Object {obj.Name}");
                    foreach (var field in obj.Fields) PrintAst(field, indent + 1);
                    foreach (var init in obj.Initializers) PrintAst(init, indent + 1);
                    foreach (var method in obj.Methods) PrintAst(method, indent + 1);
                    break;

                case FieldDeclNode field:
                    Console.WriteLine($"{pad}Field {field.Name}: {PrettyType(field.Type)}");
                    if (field.Initializer != null) PrintAst(field.Initializer, indent + 1);
                    break;

                case MethodDeclNode method:
                    Console.WriteLine($"{pad}Method {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {PrettyType(p.Type)}"))}): {PrettyType(method.ReturnType)}");
                    PrintAst(method.Body, indent + 1);
                    break;

                case FuncDeclNode func:
                    var entry = func.IsEntry ? "entry " : string.Empty;
                    Console.WriteLine($"{pad}{entry}Func {func.Name}({string.Join(", ", func.Parameters.Select(p => $"{p.Name}: {PrettyType(p.Type)}"))}): {PrettyType(func.ReturnType)}");
                    PrintAst(func.Body, indent + 1);
                    break;

                case ParamNode param:
                    Console.WriteLine($"{pad}Param {param.Name}: {PrettyType(param.Type)}");
                    break;

                case BlockNode block:
                    Console.WriteLine($"{pad}Block");
                    foreach (var stmt in block.Statements) PrintAst(stmt, indent + 1);
                    break;

                case VarDeclStmtNode v:
                    Console.WriteLine($"{pad}Declare {v.Name}: {PrettyType(v.Type)}");
                    if (v.Initializer != null) PrintAst(v.Initializer, indent + 1);
                    break;

                case SetStmtNode set:
                    Console.WriteLine($"{pad}Set {set.Target.Name}");
                    if (set.Target.Index != null) PrintAst(set.Target.Index, indent + 1);
                    PrintAst(set.Value, indent + 1);
                    break;

                case ExprStmtNode es:
                    Console.WriteLine($"{pad}Expr");
                    PrintAst(es.Expression, indent + 1);
                    break;

                case GivesStmtNode gs:
                    Console.WriteLine($"{pad}Gives");
                    PrintAst(gs.Expression, indent + 1);
                    break;

                case CheckStmtNode chk:
                    Console.WriteLine($"{pad}Check");
                    PrintAst(chk.Condition, indent + 1);
                    Console.WriteLine($"{pad}Then");
                    PrintAst(chk.ThenBlock, indent + 2);
                    if (chk.ElseBlock != null)
                    {
                        Console.WriteLine($"{pad}Else");
                        PrintAst(chk.ElseBlock, indent + 2);
                    }
                    break;

                case RepeatStmtNode rep:
                    Console.WriteLine($"{pad}Repeat");
                    PrintAst(rep.Condition, indent + 1);
                    PrintAst(rep.Body, indent + 1);
                    break;

                case LoopStmtNode loop:
                    Console.WriteLine($"{pad}Loop");
                    if (loop.Init != null)
                    {
                        Console.WriteLine($"{pad} Init");
                        PrintAst(loop.Init, indent + 2);
                    }
                    if (loop.Condition != null)
                    {
                        Console.WriteLine($"{pad} Cond");
                        PrintAst(loop.Condition, indent + 2);
                    }
                    if (loop.Action != null)
                    {
                        Console.WriteLine($"{pad} Action");
                        PrintAst(loop.Action, indent + 2);
                    }
                    PrintAst(loop.Body, indent + 1);
                    break;

                case AssignTargetNode target:
                    Console.WriteLine($"{pad}Target {target.Name}");
                    if (target.Index != null) PrintAst(target.Index, indent + 1);
                    break;

                case BinaryExprNode bin:
                    Console.WriteLine($"{pad}{bin.Op}");
                    PrintAst(bin.Left, indent + 1);
                    PrintAst(bin.Right, indent + 1);
                    break;

                case UnaryExprNode un:
                    Console.WriteLine($"{pad}{un.Op}");
                    PrintAst(un.Operand, indent + 1);
                    break;

                case IdentifierExprNode id:
                    Console.WriteLine($"{pad}Id {id.Name}");
                    break;

                case IntLiteralNode i:
                    Console.WriteLine($"{pad}Int {i.Value}");
                    break;

                case FloatLiteralNode f:
                    Console.WriteLine($"{pad}Float {f.Value}");
                    break;

                case StringLiteralNode s:
                    Console.WriteLine($"{pad}String \"{s.Value}\"");
                    break;

                case BoolLiteralNode b:
                    Console.WriteLine($"{pad}Bool {b.Value}");
                    break;

                case NullLiteralNode:
                    Console.WriteLine($"{pad}Null");
                    break;

                case CallExprNode call:
                    Console.WriteLine($"{pad}Call {string.Join('.', call.Path)}");
                    foreach (var arg in call.Arguments) PrintAst(arg, indent + 1);
                    break;

                case ArrayLiteralNode arr:
                    Console.WriteLine($"{pad}Array");
                    foreach (var el in arr.Elements) PrintAst(el, indent + 1);
                    break;

                case IndexExprNode idx:
                    Console.WriteLine($"{pad}Index");
                    PrintAst(idx.Target, indent + 1);
                    PrintAst(idx.Index, indent + 1);
                    break;

                default:
                    Console.WriteLine($"{pad}{node.GetType().Name}");
                    break;
            }
        }

        private static string PrettyType(TypeSpecNode type)
        {
            var suffix = type.ArrayLength.HasValue ? $"[{type.ArrayLength}]" : string.Empty;
            var nullable = type.IsNullable ? "?" : string.Empty;
            return $"{type.BaseType.Name}{suffix}{nullable}";
        }
    }
}
