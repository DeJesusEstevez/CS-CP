using System;
using System.Collections.Generic;

namespace RedLangCompiler.Semantics;

/// <summary>
/// Tabla de símbolos jerárquica (stack de scopes).
/// </summary>
public class SymbolTable
{
    private readonly Stack<Dictionary<string, Symbol>> _scopes = new();

    public SymbolTable()
    {
        PushScope(); // global
    }

    public void PushScope() => _scopes.Push(new Dictionary<string, Symbol>(StringComparer.Ordinal));

    public void PopScope()
    {
        if (_scopes.Count == 0) throw new InvalidOperationException("No hay scopes para cerrar.");
        _scopes.Pop();
    }

    public void Add(Symbol symbol, int line, int column)
    {
        var scope = _scopes.Peek();
        if (scope.ContainsKey(symbol.Name))
        {
            throw new Exceptions.CompilationException(
                $"El símbolo '{symbol.Name}' ya está declarado en este alcance.",
                line,
                column);
        }
        scope[symbol.Name] = symbol;
    }

    public Symbol? Lookup(string name)
    {
        foreach (var scope in _scopes)
        {
            if (scope.TryGetValue(name, out var symbol)) return symbol;
        }
        return null;
    }

    public Symbol? LookupCurrent(string name)
    {
        var scope = _scopes.Peek();
        return scope.TryGetValue(name, out var symbol) ? symbol : null;
    }
}
