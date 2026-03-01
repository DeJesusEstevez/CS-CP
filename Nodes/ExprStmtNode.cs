namespace RedLangCompiler.Nodes;

public class ExprStmtNode : StatementNode
{
    public ExpressionNode Expression { get; set; } = default!;
}
