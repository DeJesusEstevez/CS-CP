namespace RedLangCompiler.Nodes;

public class TypeSpecNode : AstNode
{
    public BaseTypeNode BaseType { get; set; } = default!;
    public int? ArrayLength { get; set; }
    public bool IsNullable { get; set; }
}

public class BaseTypeNode : AstNode
{
    public string Name { get; set; } = string.Empty;
    public bool IsBuiltin { get; set; }
}
