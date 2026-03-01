using System;
using System.Collections.Generic;
using System.Linq;
using RedLangCompiler.Exceptions;
using RedLangCompiler.Nodes;

namespace RedLangCompiler.Semantics;

public class SemanticAnalyzer
{
    private readonly Dictionary<string, ObjectSymbol> _objects = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FunctionSymbol> _functions = new(StringComparer.Ordinal);
    private readonly SymbolTable _symbols = new();
    private readonly Stack<TypeInfo> _returnStack = new();

    public void Analyze(ProgramNode program)
    {
        CollectObjects(program);
        CollectFunctions(program);
        AddBuiltins();

        // Chequear objetos (campos, inicializadores, métodos)
        foreach (var obj in program.Objects)
        {
            AnalyzeObject(obj);
        }

        // Chequear funciones globales
        foreach (var func in program.Functions)
        {
            AnalyzeFunction(func);
        }
    }

    private void CollectObjects(ProgramNode program)
    {
        foreach (var obj in program.Objects)
        {
            if (_objects.ContainsKey(obj.Name))
            {
                throw new CompilationException(
                    $"El objeto '{obj.Name}' ya fue declarado.",
                    obj.Line,
                    obj.Column);
            }

            _objects[obj.Name] = new ObjectSymbol(
                obj.Name,
                new Dictionary<string, FieldSymbol>(StringComparer.Ordinal),
                new Dictionary<string, FunctionSymbol>(StringComparer.Ordinal));
        }

        // Con objetos ya registrados, resolvemos campos y métodos.
        foreach (var obj in program.Objects)
        {
            var objSym = _objects[obj.Name];

            foreach (var field in obj.Fields)
            {
                var fieldType = ResolveType(field.Type);
                if (objSym.Fields.ContainsKey(field.Name))
                {
                    throw new CompilationException(
                        $"El campo '{field.Name}' ya existe en el objeto '{obj.Name}'.",
                        field.Line,
                        field.Column);
                }

                objSym.Fields[field.Name] = new FieldSymbol(field.Name, fieldType);
            }

            foreach (var method in obj.Methods)
            {
                if (objSym.Methods.ContainsKey(method.Name))
                {
                    throw new CompilationException(
                        $"El método '{method.Name}' ya existe en el objeto '{obj.Name}'.",
                        method.Line,
                        method.Column);
                }

                var fnSym = BuildMethodSymbol(method, obj.Name);
                objSym.Methods[method.Name] = fnSym;
            }
        }
    }

    private void CollectFunctions(ProgramNode program)
    {
        foreach (var func in program.Functions)
        {
            if (_functions.ContainsKey(func.Name))
            {
                throw new CompilationException(
                    $"La función '{func.Name}' ya fue declarada.",
                    func.Line,
                    func.Column);
            }

            var fnSym = BuildFunctionSymbol(func);
            _functions[func.Name] = fnSym;
        }
    }

    private void AddBuiltins()
    {
        // show(any):void
        _functions.TryAdd(
            "show",
            new FunctionSymbol(
                "show",
                new List<ParameterSymbol> { new("value", TypeInfo.Any) },
                TypeInfo.Void,
                isBuiltin: true));

        // ask(s):string (se acepta cualquier tipo para simplificar prompts)
        _functions.TryAdd(
            "ask",
            new FunctionSymbol(
                "ask",
                new List<ParameterSymbol> { new("prompt", TypeInfo.Any) },
                TypeInfo.String,
                isBuiltin: true));
    }

    private FunctionSymbol BuildFunctionSymbol(FuncDeclNode func, string? declaringObject = null)
    {
        var parameters = new List<ParameterSymbol>();
        foreach (var param in func.Parameters)
        {
            var paramType = ResolveType(param.Type);
            parameters.Add(new ParameterSymbol(param.Name, paramType));
        }

        var returnType = ResolveType(func.ReturnType);
        return new FunctionSymbol(func.Name, parameters, returnType, func.IsEntry, isBuiltin: false, declaringObject);
    }

    private FunctionSymbol BuildMethodSymbol(MethodDeclNode method, string declaringObject)
    {
        var parameters = new List<ParameterSymbol>();
        foreach (var param in method.Parameters)
        {
            var paramType = ResolveType(param.Type);
            parameters.Add(new ParameterSymbol(param.Name, paramType));
        }

        var returnType = ResolveType(method.ReturnType);
        return new FunctionSymbol(method.Name, parameters, returnType, isEntry: false, isBuiltin: false, declaringObject);
    }

    private void AnalyzeObject(ObjectDeclNode objDecl)
    {
        var objSym = _objects[objDecl.Name];

        // Scope con campos accesibles
        _symbols.PushScope();
        foreach (var field in objDecl.Fields)
        {
            var fieldSym = objSym.Fields[field.Name];
            _symbols.Add(fieldSym, field.Line, field.Column);

            if (field.Initializer != null)
            {
                var initType = AnalyzeExpression(field.Initializer);
                if (!fieldSym.Type.IsAssignableFrom(initType))
                {
                    throw new CompilationException(
                        $"El valor asignado al campo '{field.Name}' no es compatible con el tipo '{fieldSym.Type.DisplayName}'.",
                        field.Initializer.Line,
                        field.Initializer.Column);
                }
            }
        }

        // Inicializadores de objeto (expresiones sueltas dentro del cuerpo)
        foreach (var init in objDecl.Initializers)
        {
            _ = AnalyzeExpression(init.Expression);
        }

        // Métodos
        foreach (var method in objDecl.Methods)
        {
            AnalyzeMethod(objDecl, method);
        }

        _symbols.PopScope();
    }

    private void AnalyzeMethod(ObjectDeclNode objDecl, MethodDeclNode method)
    {
        var objSym = _objects[objDecl.Name];
        var methodSym = objSym.Methods[method.Name];

        _symbols.PushScope(); // campos
        foreach (var fieldSym in objSym.Fields.Values)
        {
            _symbols.Add(fieldSym, method.Line, method.Column);
        }

        _symbols.PushScope(); // parámetros + locales
        foreach (var paramSym in methodSym.Parameters)
        {
            _symbols.Add(paramSym, method.Line, method.Column);
        }

        _returnStack.Push(methodSym.ReturnType);
        AnalyzeBlock(method.Body);
        _returnStack.Pop();

        _symbols.PopScope(); // parámetros/locales
        _symbols.PopScope(); // campos
    }

    private void AnalyzeFunction(FuncDeclNode func)
    {
        var fnSym = _functions[func.Name];

        _symbols.PushScope();
        foreach (var paramSym in fnSym.Parameters)
        {
            _symbols.Add(paramSym, func.Line, func.Column);
        }

        _returnStack.Push(fnSym.ReturnType);
        AnalyzeBlock(func.Body);
        _returnStack.Pop();

        _symbols.PopScope();
    }

    private void AnalyzeBlock(BlockNode block)
    {
        _symbols.PushScope();
        foreach (var stmt in block.Statements)
        {
            AnalyzeStatement(stmt);
        }
        _symbols.PopScope();
    }

    private void AnalyzeStatement(StatementNode stmt)
    {
        switch (stmt)
        {
            case VarDeclStmtNode v:
                AnalyzeVarDecl(v);
                break;
            case SetStmtNode s:
                AnalyzeSet(s);
                break;
            case ExprStmtNode e:
                _ = AnalyzeExpression(e.Expression);
                break;
            case GivesStmtNode g:
                AnalyzeGives(g);
                break;
            case CheckStmtNode chk:
                AnalyzeCheck(chk);
                break;
            case RepeatStmtNode rep:
                AnalyzeRepeat(rep);
                break;
            case LoopStmtNode loop:
                AnalyzeLoop(loop);
                break;
            default:
                throw new CompilationException("Sentencia no soportada en el analizador.", stmt.Line, stmt.Column);
        }
    }

    private void AnalyzeVarDecl(VarDeclStmtNode decl)
    {
        var type = ResolveType(decl.Type);
        if (_symbols.LookupCurrent(decl.Name) != null)
        {
            throw new CompilationException(
                $"La variable '{decl.Name}' ya fue declarada en este alcance.",
                decl.Line,
                decl.Column);
        }

        _symbols.Add(new VariableSymbol(decl.Name, type), decl.Line, decl.Column);

        if (decl.Initializer != null)
        {
            var initType = AnalyzeExpression(decl.Initializer);
            if (!type.IsAssignableFrom(initType))
            {
                throw new CompilationException(
                    $"El valor no es compatible con el tipo de '{decl.Name}' ({type.DisplayName}).",
                    decl.Initializer.Line,
                    decl.Initializer.Column);
            }
        }
    }

    private void AnalyzeSet(SetStmtNode set)
    {
        if (_symbols.Lookup(set.Target.Name) is not VariableSymbol targetSym)
        {
            throw new CompilationException(
                $"La variable '{set.Target.Name}' no está declarada.",
                set.Target.Line,
                set.Target.Column);
        }

        var targetType = targetSym.Type;

        if (set.Target.Index != null)
        {
            if (!targetType.IsArray)
            {
                throw new CompilationException(
                    $"La variable '{set.Target.Name}' no es un arreglo.",
                    set.Target.Line,
                    set.Target.Column);
            }

            var indexType = AnalyzeExpression(set.Target.Index);
            if (indexType.Kind != BaseKind.Int)
            {
                throw new CompilationException(
                    "El índice de un arreglo debe ser de tipo entero.",
                    set.Target.Index.Line,
                    set.Target.Index.Column);
            }

            var elementType = targetType.ElementType;
            var valueType = AnalyzeExpression(set.Value);
            if (!elementType.IsAssignableFrom(valueType))
            {
                throw new CompilationException(
                    $"No se puede asignar un valor de tipo '{valueType.DisplayName}' al arreglo '{set.Target.Name}'.",
                    set.Value.Line,
                    set.Value.Column);
            }
            return;
        }

        var exprType = AnalyzeExpression(set.Value);
        if (!targetType.IsAssignableFrom(exprType))
        {
            throw new CompilationException(
                $"No se puede asignar un valor de tipo '{exprType.DisplayName}' a '{set.Target.Name}'.",
                set.Value.Line,
                set.Value.Column);
        }
    }

    private void AnalyzeGives(GivesStmtNode gives)
    {
        if (_returnStack.Count == 0)
        {
            throw new CompilationException(
                "La sentencia 'gives' solo puede usarse dentro de una función o método.",
                gives.Line,
                gives.Column);
        }

        var expected = _returnStack.Peek();
        var exprType = AnalyzeExpression(gives.Expression);
        if (!expected.IsAssignableFrom(exprType))
        {
            throw new CompilationException(
                $"El valor devuelto no coincide con el tipo de retorno '{expected.DisplayName}'.",
                gives.Expression.Line,
                gives.Expression.Column);
        }
    }

    private void AnalyzeCheck(CheckStmtNode chk)
    {
        var condType = AnalyzeExpression(chk.Condition);
        RequireBool(condType, chk.Condition);
        AnalyzeBlock(chk.ThenBlock);
        if (chk.ElseBlock != null) AnalyzeBlock(chk.ElseBlock);
    }

    private void AnalyzeRepeat(RepeatStmtNode rep)
    {
        var condType = AnalyzeExpression(rep.Condition);
        RequireBool(condType, rep.Condition);
        AnalyzeBlock(rep.Body);
    }

    private void AnalyzeLoop(LoopStmtNode loop)
    {
        _symbols.PushScope();

        if (loop.Init != null) AnalyzeStatement(loop.Init);

        if (loop.Condition != null)
        {
            var condType = AnalyzeExpression(loop.Condition);
            RequireBool(condType, loop.Condition);
        }

        AnalyzeBlock(loop.Body);

        if (loop.Action != null) AnalyzeStatement(loop.Action);

        _symbols.PopScope();
    }

    private TypeInfo AnalyzeExpression(ExpressionNode expr)
    {
        return expr switch
        {
            IntLiteralNode => TypeInfo.Int,
            FloatLiteralNode => TypeInfo.Float,
            StringLiteralNode => TypeInfo.String,
            BoolLiteralNode => TypeInfo.Bool,
            NullLiteralNode => TypeInfo.Null,
            IdentifierExprNode id => AnalyzeIdentifier(id),
            ArrayLiteralNode arr => AnalyzeArrayLiteral(arr),
            IndexExprNode idx => AnalyzeIndex(idx),
            UnaryExprNode un => AnalyzeUnary(un),
            BinaryExprNode bin => AnalyzeBinary(bin),
            CallExprNode call => AnalyzeCall(call),
            _ => throw new CompilationException("Expresión no soportada en el analizador.", expr.Line, expr.Column)
        };
    }

    private TypeInfo AnalyzeIdentifier(IdentifierExprNode id)
    {
        if (_symbols.Lookup(id.Name) is VariableSymbol sym)
        {
            return sym.Type;
        }

        throw new CompilationException($"El identificador '{id.Name}' no está declarado.", id.Line, id.Column);
    }

    private TypeInfo AnalyzeArrayLiteral(ArrayLiteralNode arr)
    {
        if (arr.Elements.Count == 0)
        {
            throw new CompilationException("No se permiten arreglos vacíos, el tipo no puede inferirse.", arr.Line, arr.Column);
        }

        var current = AnalyzeExpression(arr.Elements[0]);

        for (int i = 1; i < arr.Elements.Count; i++)
        {
            var next = AnalyzeExpression(arr.Elements[i]);
            current = UnifyArrayElementType(current, next, arr.Elements[i]);
        }

        return new TypeInfo(current.Kind, current.ObjectName, current.IsNullable, arr.Elements.Count);
    }

    private static TypeInfo UnifyArrayElementType(TypeInfo a, TypeInfo b, AstNode nodeForError)
    {
        if (a.Kind == BaseKind.Any || b.Kind == BaseKind.Any) return TypeInfo.Any;

        if (a.IsNumeric && b.IsNumeric)
        {
            return (a.Kind == BaseKind.Float || b.Kind == BaseKind.Float) ? TypeInfo.Float : TypeInfo.Int;
        }

        if (a.Kind == b.Kind &&
            a.ObjectName == b.ObjectName &&
            a.IsNullable == b.IsNullable)
        {
            return a;
        }

        throw new CompilationException(
            $"Los elementos del arreglo deben ser del mismo tipo; se encontraron '{a.DisplayName}' y '{b.DisplayName}'.",
            nodeForError.Line,
            nodeForError.Column);
    }

    private TypeInfo AnalyzeIndex(IndexExprNode idx)
    {
        var targetType = AnalyzeExpression(idx.Target);
        if (!targetType.IsArray)
        {
            throw new CompilationException("Solo se pueden indexar arreglos.", idx.Target.Line, idx.Target.Column);
        }

        var indexType = AnalyzeExpression(idx.Index);
        if (indexType.Kind != BaseKind.Int)
        {
            throw new CompilationException("El índice de un arreglo debe ser entero.", idx.Index.Line, idx.Index.Column);
        }

        return targetType.ElementType;
    }

    private TypeInfo AnalyzeUnary(UnaryExprNode un)
    {
        var operand = AnalyzeExpression(un.Operand);

        return un.Op switch
        {
            Enumerations.UnaryOp.Negate => operand.IsNumeric
                ? operand
                : throw new CompilationException("El operador '-' solo acepta enteros o flotantes.", un.Line, un.Column),
            Enumerations.UnaryOp.Not => operand.Kind == BaseKind.Bool
                ? TypeInfo.Bool
                : throw new CompilationException("El operador 'not' solo acepta valores booleanos.", un.Line, un.Column),
            _ => throw new CompilationException("Operador unario no soportado.", un.Line, un.Column)
        };
    }

    private TypeInfo AnalyzeBinary(BinaryExprNode bin)
    {
        var left = AnalyzeExpression(bin.Left);
        var right = AnalyzeExpression(bin.Right);

        return bin.Op switch
        {
            Enumerations.BinaryOp.Or or Enumerations.BinaryOp.And => AnalyzeLogical(bin, left, right),
            Enumerations.BinaryOp.Equal or Enumerations.BinaryOp.NotEqual => AnalyzeEquality(bin, left, right),
            Enumerations.BinaryOp.Greater or Enumerations.BinaryOp.Less or Enumerations.BinaryOp.GreaterOrEqual or Enumerations.BinaryOp.LessOrEqual => AnalyzeComparison(bin, left, right),
            Enumerations.BinaryOp.Add or Enumerations.BinaryOp.Subtract or Enumerations.BinaryOp.Multiply or Enumerations.BinaryOp.Divide => AnalyzeArithmetic(bin, left, right),
            Enumerations.BinaryOp.Modulo => AnalyzeModulo(bin, left, right),
            _ => throw new CompilationException("Operador binario no soportado.", bin.Line, bin.Column)
        };
    }

    private TypeInfo AnalyzeLogical(BinaryExprNode bin, TypeInfo left, TypeInfo right)
    {
        RequireBool(left, bin.Left);
        RequireBool(right, bin.Right);
        return TypeInfo.Bool;
    }

    private TypeInfo AnalyzeEquality(BinaryExprNode bin, TypeInfo left, TypeInfo right)
    {
        if (left.Kind == BaseKind.Null || right.Kind == BaseKind.Null)
        {
            var other = left.Kind == BaseKind.Null ? right : left;
            if (!other.IsNullable && other.Kind != BaseKind.Any)
            {
                throw new CompilationException("Solo se puede comparar 'null' con tipos anulables.", bin.Line, bin.Column);
            }
            return TypeInfo.Bool;
        }

        if (left.Kind == BaseKind.Any || right.Kind == BaseKind.Any) return TypeInfo.Bool;
        if (left.IsNumeric && right.IsNumeric) return TypeInfo.Bool;

        if (left.Kind == right.Kind &&
            left.ObjectName == right.ObjectName &&
            left.IsArray == right.IsArray)
        {
            return TypeInfo.Bool;
        }

        throw new CompilationException(
            $"No se puede comparar '{left.DisplayName}' con '{right.DisplayName}'.",
            bin.Line,
            bin.Column);
    }

    private TypeInfo AnalyzeComparison(BinaryExprNode bin, TypeInfo left, TypeInfo right)
    {
        if (!(left.IsNumeric && right.IsNumeric))
        {
            throw new CompilationException(
                "Los operadores relacionales solo aceptan operandos numéricos.",
                bin.Line,
                bin.Column);
        }
        return TypeInfo.Bool;
    }

    private TypeInfo AnalyzeArithmetic(BinaryExprNode bin, TypeInfo left, TypeInfo right)
    {
        if (!(left.IsNumeric && right.IsNumeric))
        {
            throw new CompilationException(
                "Los operadores aritméticos solo aceptan operandos numéricos.",
                bin.Line,
                bin.Column);
        }

        return (left.Kind == BaseKind.Float || right.Kind == BaseKind.Float) ? TypeInfo.Float : TypeInfo.Int;
    }

    private TypeInfo AnalyzeModulo(BinaryExprNode bin, TypeInfo left, TypeInfo right)
    {
        if (left.Kind != BaseKind.Int || right.Kind != BaseKind.Int)
        {
            throw new CompilationException(
                "El operador '%' solo acepta enteros.",
                bin.Line,
                bin.Column);
        }
        return TypeInfo.Int;
    }

    private TypeInfo AnalyzeCall(CallExprNode call)
    {
        if (call.Path.Count == 0)
        {
            throw new CompilationException("Llamada inválida: falta el nombre de la función.", call.Line, call.Column);
        }

        if (call.Path.Count == 1)
        {
            var name = call.Path[0];
            if (_functions.TryGetValue(name, out var fn))
            {
                return CheckCallArguments(call, fn);
            }

            if (_objects.TryGetValue(name, out _))
            {
                if (call.Arguments.Count > 0)
                {
                    throw new CompilationException(
                        $"El objeto '{name}' no acepta argumentos al instanciarse.",
                        call.Line,
                        call.Column);
                }
                return new TypeInfo(BaseKind.Object, name);
            }

            throw new CompilationException($"La función u objeto '{name}' no está declarado.", call.Line, call.Column);
        }

        if (call.Path.Count == 2)
        {
            var targetName = call.Path[0];
            var methodName = call.Path[1];

            ObjectSymbol objSym;

            if (_symbols.Lookup(targetName) is VariableSymbol varSym)
            {
                if (varSym.Type.Kind != BaseKind.Object || string.IsNullOrEmpty(varSym.Type.ObjectName))
                {
                    throw new CompilationException(
                        $"'{targetName}' no es un objeto y no puede tener métodos.",
                        call.Line,
                        call.Column);
                }

                objSym = _objects[varSym.Type.ObjectName];
            }
            else if (_objects.TryGetValue(targetName, out var typeSym))
            {
                // Llamada estilo estático: Tipo.metodo()
                objSym = typeSym;
            }
            else
            {
                throw new CompilationException(
                    $"El objeto o variable '{targetName}' no está declarado.",
                    call.Line,
                    call.Column);
            }

            if (!objSym.Methods.TryGetValue(methodName, out var methodSym))
            {
                throw new CompilationException(
                    $"El método '{methodName}' no existe en '{objSym.Name}'.",
                    call.Line,
                    call.Column);
            }

            return CheckCallArguments(call, methodSym);
        }

        throw new CompilationException(
            "Las llamadas encadenadas con más de un nivel no están soportadas.",
            call.Line,
            call.Column);
    }

    private TypeInfo CheckCallArguments(CallExprNode call, FunctionSymbol fn)
    {
        if (fn.Parameters.Count != call.Arguments.Count)
        {
            throw new CompilationException(
                $"La función '{fn.Name}' espera {fn.Parameters.Count} argumento(s) pero se pasaron {call.Arguments.Count}.",
                call.Line,
                call.Column);
        }

        for (int i = 0; i < fn.Parameters.Count; i++)
        {
            var argType = AnalyzeExpression(call.Arguments[i]);
            var param = fn.Parameters[i];
            if (!param.Type.IsAssignableFrom(argType))
            {
                throw new CompilationException(
                    $"Argumento {i + 1} de '{fn.Name}' requiere '{param.Type.DisplayName}' pero se recibió '{argType.DisplayName}'.",
                    call.Arguments[i].Line,
                    call.Arguments[i].Column);
            }
        }

        return fn.ReturnType;
    }

    private void RequireBool(TypeInfo type, AstNode node)
    {
        if (type.Kind != BaseKind.Bool)
        {
            throw new CompilationException("Se esperaba una expresión booleana.", node.Line, node.Column);
        }
    }

    private TypeInfo ResolveType(TypeSpecNode spec)
    {
        var baseType = spec.BaseType.Name;
        TypeInfo info = baseType switch
        {
            "i" => TypeInfo.Int,
            "f" => TypeInfo.Float,
            "s" => TypeInfo.String,
            "b" => TypeInfo.Bool,
            _ when _objects.ContainsKey(baseType) => new TypeInfo(BaseKind.Object, baseType),
            _ => throw new CompilationException($"El tipo '{baseType}' no está declarado.", spec.Line, spec.Column)
        };

        info = spec.ArrayLength.HasValue
            ? new TypeInfo(info.Kind, info.ObjectName, spec.IsNullable, spec.ArrayLength)
            : new TypeInfo(info.Kind, info.ObjectName, spec.IsNullable);

        return info;
    }
}
