namespace RedLangCompiler.Nodes;

public class CheckStmtNode : StatementNode
{
    public ExpressionNode Condition { get; set; } = default!;
    public BlockNode ThenBlock { get; set; } = default!;
    public BlockNode? ElseBlock { get; set; }
}

public class RepeatStmtNode : StatementNode
{
    public ExpressionNode Condition { get; set; } = default!;
    public BlockNode Body { get; set; } = default!;
}

public class LoopStmtNode : StatementNode
{
    public StatementNode? Init { get; set; }
    public ExpressionNode? Condition { get; set; }
    public StatementNode? Action { get; set; }
    public BlockNode Body { get; set; } = default!;
}
