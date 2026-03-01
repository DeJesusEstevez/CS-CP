using RedLangCompiler.Enumerations;

namespace RedLangCompiler.Nodes;

public class UnaryExprNode : ExpressionNode
{
    public UnaryOp Op { get; set; }
    public ExpressionNode Operand { get; set; } = default!;
}
