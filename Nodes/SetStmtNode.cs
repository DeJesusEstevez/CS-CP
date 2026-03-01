namespace RedLangCompiler.Nodes;

public class SetStmtNode : StatementNode
{
    public AssignTargetNode Target { get; set; } = default!;
    public ExpressionNode Value { get; set; } = default!;
}

public class AssignTargetNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public ExpressionNode? Index { get; set; }
}
