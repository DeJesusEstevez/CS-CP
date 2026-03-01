using System.Collections.Generic;

namespace RedLangCompiler.Nodes;

public class ArrayLiteralNode : ExpressionNode
{
    public List<ExpressionNode> Elements { get; } = new();
}

public class IndexExprNode : ExpressionNode
{
    public ExpressionNode Target { get; set; } = default!;
    public ExpressionNode Index { get; set; } = default!;
}
