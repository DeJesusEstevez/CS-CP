using System.Collections.Generic;

namespace RedLangCompiler.Nodes;

public class CallExprNode : ExpressionNode
{
    public List<string> Path { get; } = new();
    public List<ExpressionNode> Arguments { get; } = new();
}
