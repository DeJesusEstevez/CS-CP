using System.Collections.Generic;

namespace RedLangCompiler.Semantics;

public enum SymbolKind
{
    Variable,
    Parameter,
    Function,
    Method,
    Object,
    Field
}

public abstract class Symbol
{
    protected Symbol(string name, SymbolKind kind)
    {
        Name = name;
        Kind = kind;
    }

    public string Name { get; }
    public SymbolKind Kind { get; }
}

public class VariableSymbol : Symbol
{
    public VariableSymbol(string name, TypeInfo type)
        : base(name, SymbolKind.Variable)
    {
        Type = type;
    }

    public TypeInfo Type { get; }
}

public class ParameterSymbol : VariableSymbol
{
    public ParameterSymbol(string name, TypeInfo type)
        : base(name, type)
    {
    }
}

public class FieldSymbol : VariableSymbol
{
    public FieldSymbol(string name, TypeInfo type)
        : base(name, type)
    {
    }
}

public class FunctionSymbol : Symbol
{
    public FunctionSymbol(
        string name,
        List<ParameterSymbol> parameters,
        TypeInfo returnType,
        bool isEntry = false,
        bool isBuiltin = false,
        string? declaringObject = null)
        : base(name, declaringObject == null ? SymbolKind.Function : SymbolKind.Method)
    {
        Parameters = parameters;
        ReturnType = returnType;
        IsEntry = isEntry;
        IsBuiltin = isBuiltin;
        DeclaringObject = declaringObject;
    }

    public List<ParameterSymbol> Parameters { get; }
    public TypeInfo ReturnType { get; }
    public bool IsEntry { get; }
    public bool IsBuiltin { get; }
    public string? DeclaringObject { get; }
}

public class ObjectSymbol : Symbol
{
    public ObjectSymbol(
        string name,
        Dictionary<string, FieldSymbol> fields,
        Dictionary<string, FunctionSymbol> methods)
        : base(name, SymbolKind.Object)
    {
        Fields = fields;
        Methods = methods;
    }

    public Dictionary<string, FieldSymbol> Fields { get; }
    public Dictionary<string, FunctionSymbol> Methods { get; }
}
