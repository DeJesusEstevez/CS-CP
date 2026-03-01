using System.Collections.Generic;

namespace RedLangCompiler.Nodes;

public class FuncDeclNode : AstNode
{
    public bool IsEntry { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<ParamNode> Parameters { get; } = new();
    public TypeSpecNode ReturnType { get; set; } = default!;
    public BlockNode Body { get; set; } = default!;
}

public class ParamNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public TypeSpecNode Type { get; set; } = default!;
}
