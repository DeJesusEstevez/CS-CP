using System.Collections.Generic;

namespace RedLangCompiler.Nodes;

public class ProgramNode : AstNode
{
    public List<UseNode> Uses { get; } = new();
    public List<ObjectDeclNode> Objects { get; } = new();
    public List<FuncDeclNode> Functions { get; } = new();
}

public class UseNode : StatementNode
{
    public string Module { get; set; } = string.Empty;
}
