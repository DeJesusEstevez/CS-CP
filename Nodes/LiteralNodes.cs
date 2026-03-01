namespace RedLangCompiler.Nodes;

public class IntLiteralNode : ExpressionNode
{
    public int Value { get; set; }
}

public class FloatLiteralNode : ExpressionNode
{
    public double Value { get; set; }
}

public class StringLiteralNode : ExpressionNode
{
    public string Value { get; set; } = string.Empty;
}

public class BoolLiteralNode : ExpressionNode
{
    public bool Value { get; set; }
}

public class NullLiteralNode : ExpressionNode { }
