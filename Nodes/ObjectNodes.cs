using System.Collections.Generic;

namespace RedLangCompiler.Nodes;

public class ObjectDeclNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public List<FieldDeclNode> Fields { get; } = new();
    public List<MethodDeclNode> Methods { get; } = new();
    public List<ExprStmtNode> Initializers { get; } = new();
}

public class FieldDeclNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public TypeSpecNode Type { get; set; } = default!;
    public ExpressionNode? Initializer { get; set; }
}

public class MethodDeclNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public List<ParamNode> Parameters { get; } = new();
    public TypeSpecNode ReturnType { get; set; } = default!;
    public BlockNode Body { get; set; } = default!;
}
