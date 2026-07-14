namespace BasicCore.Capabilities;

/// <summary>
///     Canonical stable identifiers used at the typed-intrinsic compatibility boundary.
/// </summary>
public static class IntrinsicCapabilityIds
{
    public const string CallCSharp = "call C#";
    public const string CallCSharpConstructor = "call C# ctor";
    public const string LoadLocal = "load_local";
    public const string StoreLocal = "store_local";
    public const string LoadLocalReference = "load_local_ref";
    public const string LoadExternal = "load_external";
    public const string StoreExternal = "store_external";
    public const string BooleanAnd = "boolean_and";
    public const string BooleanOr = "boolean_or";
    public const string BooleanNot = "boolean_not";
}
