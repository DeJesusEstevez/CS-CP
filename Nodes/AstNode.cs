namespace RedLangCompiler.Nodes;

public abstract class AstNode
{
    public int Line { get; set; }
    public int Column { get; set; }

    public SourceSpan Span
    {
        get => new(Line, Column);
        set
        {
            Line = value.Line;
            Column = value.Column;
        }
    }
}

public abstract class StatementNode : AstNode { }

public abstract class ExpressionNode : AstNode { }
