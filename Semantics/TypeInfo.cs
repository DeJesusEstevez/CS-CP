using System;

namespace RedLangCompiler.Semantics;

public enum BaseKind
{
    Int,
    Float,
    String,
    Bool,
    Object,
    Any,
    Void,
    Null
}

/// <summary>
/// Tipo estático ya resuelto (post-AST) para chequeo semántico.
/// </summary>
public readonly record struct TypeInfo(
    BaseKind Kind,
    string? ObjectName = null,
    bool IsNullable = false,
    int? ArrayLength = null)
{
    public static readonly TypeInfo Int = new(BaseKind.Int);
    public static readonly TypeInfo Float = new(BaseKind.Float);
    public static readonly TypeInfo String = new(BaseKind.String);
    public static readonly TypeInfo Bool = new(BaseKind.Bool);
    public static readonly TypeInfo Any = new(BaseKind.Any);
    public static readonly TypeInfo Void = new(BaseKind.Void);
    public static readonly TypeInfo Null = new(BaseKind.Null, null, true);

    public bool IsArray => ArrayLength.HasValue;
    public bool IsNumeric => Kind is BaseKind.Int or BaseKind.Float;

    public TypeInfo ElementType =>
        IsArray ? new TypeInfo(Kind, ObjectName, IsNullable) : this;

    public string DisplayName =>
        Kind switch
        {
            BaseKind.Object => ObjectName ?? "<object>",
            BaseKind.Int => "i",
            BaseKind.Float => "f",
            BaseKind.String => "s",
            BaseKind.Bool => "b",
            BaseKind.Any => "any",
            BaseKind.Void => "void",
            BaseKind.Null => "null",
            _ => Kind.ToString().ToLower()
        } + (IsArray ? $"[{ArrayLength}]" : string.Empty) + (IsNullable ? "?" : string.Empty);

    public bool IsAssignableFrom(TypeInfo other)
    {
        if (Kind == BaseKind.Any || other.Kind == BaseKind.Any) return true;
        if (Kind == BaseKind.Void || other.Kind == BaseKind.Void) return false;

        if (other.Kind == BaseKind.Null)
        {
            return IsNullable || Kind == BaseKind.Any;
        }

        if (IsArray != other.IsArray) return false;
        if (IsArray && ArrayLength != other.ArrayLength) return false;

        if (Kind == BaseKind.Float && other.Kind == BaseKind.Int) return true; // promoción implícita

        if (Kind != other.Kind) return false;
        if (Kind == BaseKind.Object && !string.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsNullable && other.IsNullable) return false;

        return true;
    }
}
