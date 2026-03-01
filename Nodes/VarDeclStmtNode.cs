namespace RedLangCompiler.Nodes;

public class VarDeclStmtNode : StatementNode
{
    public string Name { get; set; } = string.Empty;
    public TypeSpecNode Type { get; set; } = default!;
    public ExpressionNode? Initializer { get; set; }
}
