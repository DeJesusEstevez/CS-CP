using RedLangCompiler.Enumerations;

namespace RedLangCompiler.Nodes;

public class BinaryExprNode : ExpressionNode
{
    public BinaryOp Op { get; set; }
    public ExpressionNode Left { get; set; } = default!;
    public ExpressionNode Right { get; set; } = default!;
}
