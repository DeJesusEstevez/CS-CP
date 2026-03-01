using System.Collections.Generic;

namespace RedLangCompiler.Nodes;

public class BlockNode : AstNode
{
    public List<StatementNode> Statements { get; } = new();
}
