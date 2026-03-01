namespace RedLangCompiler.Nodes;

public class GivesStmtNode : StatementNode
{
    public ExpressionNode Expression { get; set; } = default!;
}
