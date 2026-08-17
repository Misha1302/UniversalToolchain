namespace UniversalToolchain.Wist.LanguagePack;

internal readonly record struct WistSemanticOperationId(string Value)
{
    public override string ToString() => Value;
}

internal static class WistSemanticOperations
{
    public static WistSemanticOperationId Add { get; } = new("wist.semantic.arithmetic.add");
    public static WistSemanticOperationId Subtract { get; } = new("wist.semantic.arithmetic.subtract");
    public static WistSemanticOperationId Multiply { get; } = new("wist.semantic.arithmetic.multiply");
    public static WistSemanticOperationId Divide { get; } = new("wist.semantic.arithmetic.divide");
    public static WistSemanticOperationId UnaryMinus { get; } = new("wist.semantic.arithmetic.unary-minus");
    public static WistSemanticOperationId NativeAdd { get; } = new("wist.semantic.native.add");
    public static WistSemanticOperationId NativeSubtract { get; } = new("wist.semantic.native.subtract");
    public static WistSemanticOperationId NativeMultiply { get; } = new("wist.semantic.native.multiply");
    public static WistSemanticOperationId NativeDivide { get; } = new("wist.semantic.native.divide");
    public static WistSemanticOperationId NativeUnaryMinus { get; } = new("wist.semantic.native.unary-minus");
    public static WistSemanticOperationId Equal { get; } = new("wist.semantic.comparison.equal");
    public static WistSemanticOperationId NotEqual { get; } = new("wist.semantic.comparison.not-equal");
    public static WistSemanticOperationId Greater { get; } = new("wist.semantic.comparison.greater");
    public static WistSemanticOperationId Less { get; } = new("wist.semantic.comparison.less");
    public static WistSemanticOperationId GreaterOrEqual { get; } = new("wist.semantic.comparison.greater-or-equal");
    public static WistSemanticOperationId LessOrEqual { get; } = new("wist.semantic.comparison.less-or-equal");
    public static WistSemanticOperationId BooleanNot { get; } = new("wist.semantic.boolean.not");
}

internal readonly record struct WistSemanticTypeId(string AssemblyQualifiedName)
{
    public static WistSemanticTypeId FromType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return new WistSemanticTypeId(type.AssemblyQualifiedName
            ?? throw new InvalidOperationException($"Runtime type '{type}' has no assembly-qualified semantic identity."));
    }

    public Type Resolve() => Type.GetType(AssemblyQualifiedName, throwOnError: true)!;
}

internal enum WistSemanticSymbolKind
{
    Local,
    ExternalVariable,
    ExternalConstant
}

internal readonly record struct WistSemanticSymbolId(
    WistSemanticSymbolKind Kind,
    string Name,
    string StorageKey,
    WistSemanticTypeId Type,
    int ExternalSlot = -1)
{
    public bool CanAssign => Kind != WistSemanticSymbolKind.ExternalConstant;
}

internal enum WistNativeLiteralKind
{
    Int32,
    Int64,
    Single,
    Double,
    Decimal
}

internal readonly record struct WistNativeLiteralValue(
    WistNativeLiteralKind Kind,
    int Int32Value,
    long Int64Value,
    float SingleValue,
    double DoubleValue,
    decimal DecimalValue)
{
    public static WistNativeLiteralValue FromRuntimeValue(object value) => value switch
    {
        int typed => new(WistNativeLiteralKind.Int32, typed, default, default, default, default),
        long typed => new(WistNativeLiteralKind.Int64, default, typed, default, default, default),
        float typed => new(WistNativeLiteralKind.Single, default, default, typed, default, default),
        double typed => new(WistNativeLiteralKind.Double, default, default, default, typed, default),
        decimal typed => new(WistNativeLiteralKind.Decimal, default, default, default, default, typed),
        _ => throw new InvalidOperationException(
            $"Unsupported native numeric semantic value type '{value.GetType().FullName}'.")
    };

    public object Materialize() => Kind switch
    {
        WistNativeLiteralKind.Int32 => Int32Value,
        WistNativeLiteralKind.Int64 => Int64Value,
        WistNativeLiteralKind.Single => SingleValue,
        WistNativeLiteralKind.Double => DoubleValue,
        WistNativeLiteralKind.Decimal => DecimalValue,
        _ => throw new InvalidOperationException($"Unsupported native literal kind '{Kind}'.")
    };
}

internal abstract class WistSemanticNode
{
    protected WistSemanticNode(IEnumerable<WistSemanticNode>? children = null)
    {
        Children = Array.AsReadOnly((children ?? []).ToArray());
    }

    public IReadOnlyList<WistSemanticNode> Children { get; }
}

internal enum WistSemanticSequenceKind
{
    Program,
    Scope
}

internal sealed class WistSemanticSequenceNode(
    WistSemanticSequenceKind kind,
    IEnumerable<WistSemanticNode> children) : WistSemanticNode(children)
{
    public WistSemanticSequenceKind Kind { get; } = kind;
}

internal sealed class WistNumberNode(double value) : WistSemanticNode
{
    public double Value { get; } = value;
}

internal sealed class WistNativeNumberNode(WistNativeLiteralValue value) : WistSemanticNode
{
    public WistNativeLiteralValue Value { get; } = value;
}

internal sealed class WistBooleanLiteralNode(bool value) : WistSemanticNode
{
    public bool Value { get; } = value;
}

internal sealed class WistSymbolReferenceNode(WistSemanticSymbolId symbol, bool isWriteTarget) : WistSemanticNode
{
    public WistSemanticSymbolId Symbol { get; } = symbol;
    public bool IsWriteTarget { get; } = isWriteTarget;
}

internal sealed class WistSemanticOperationNode(
    WistSemanticOperationId operation,
    IEnumerable<WistSemanticNode> operands) : WistSemanticNode(operands)
{
    public WistSemanticOperationId Operation { get; } = operation;
}

internal sealed class WistAssignmentNode(
    WistSymbolReferenceNode target,
    WistSemanticNode value) : WistSemanticNode([target, value])
{
    public WistSymbolReferenceNode Target { get; } = target;
    public WistSemanticNode Value { get; } = value;
}

internal sealed class WistShortCircuitNode(
    bool isAnd,
    WistSemanticNode left,
    WistSemanticNode right,
    Guid falseLabel,
    Guid trueLabel,
    Guid endLabel) : WistSemanticNode([left, right])
{
    public bool IsAnd { get; } = isAnd;
    public WistSemanticNode Left { get; } = left;
    public WistSemanticNode Right { get; } = right;
    public Guid FalseLabel { get; } = falseLabel;
    public Guid TrueLabel { get; } = trueLabel;
    public Guid EndLabel { get; } = endLabel;
}

internal sealed class WistConditionalBranchNode(
    WistSemanticNode condition,
    WistSemanticNode body,
    IReadOnlyList<WistSemanticNode> alternatives,
    Guid elseLabel,
    Guid endLabel) : WistSemanticNode([condition, body, .. alternatives])
{
    public WistSemanticNode Condition { get; } = condition;
    public WistSemanticNode Body { get; } = body;
    public IReadOnlyList<WistSemanticNode> Alternatives { get; } = Array.AsReadOnly(alternatives.ToArray());
    public Guid ElseLabel { get; } = elseLabel;
    public Guid EndLabel { get; } = endLabel;
}

internal sealed class WistElseNode(WistSemanticNode body) : WistSemanticNode([body])
{
    public WistSemanticNode Body { get; } = body;
}

internal sealed class WistIfExpressionNode(
    WistSemanticNode condition,
    WistSemanticNode whenTrue,
    WistSemanticNode whenFalse) : WistSemanticNode([condition, whenTrue, whenFalse])
{
    public WistSemanticNode Condition { get; } = condition;
    public WistSemanticNode WhenTrue { get; } = whenTrue;
    public WistSemanticNode WhenFalse { get; } = whenFalse;
}

internal sealed class WistWhileNode(
    WistSemanticNode condition,
    WistSemanticNode body,
    Guid startLabel,
    Guid endLabel) : WistSemanticNode([condition, body])
{
    public WistSemanticNode Condition { get; } = condition;
    public WistSemanticNode Body { get; } = body;
    public Guid StartLabel { get; } = startLabel;
    public Guid EndLabel { get; } = endLabel;
}

internal sealed class WistForNode(
    WistSemanticNode initialization,
    WistSemanticNode condition,
    WistSemanticNode step,
    WistSemanticNode body,
    Guid startLabel,
    Guid endLabel) : WistSemanticNode([initialization, condition, step, body])
{
    public WistSemanticNode Initialization { get; } = initialization;
    public WistSemanticNode Condition { get; } = condition;
    public WistSemanticNode Step { get; } = step;
    public WistSemanticNode Body { get; } = body;
    public Guid StartLabel { get; } = startLabel;
    public Guid EndLabel { get; } = endLabel;
}

internal sealed class WistLabelNode(string name) : WistSemanticNode
{
    public string Name { get; } = name;
}

internal sealed class WistGotoNode(string name) : WistSemanticNode
{
    public string Name { get; } = name;
}

internal sealed class WistFunctionCallNode(
    string functionName,
    IEnumerable<WistSemanticNode> arguments) : WistSemanticNode(arguments)
{
    public string FunctionName { get; } = functionName;
    public IReadOnlyList<WistSemanticNode> Arguments => Children;
}

internal sealed class WistCSharpCallNode(
    string fullName,
    IEnumerable<WistSemanticNode> arguments) : WistSemanticNode(arguments)
{
    public string FullName { get; } = fullName;
    public IReadOnlyList<WistSemanticNode> Arguments => Children;
}

internal sealed class WistDefineArgumentNode(string name, string typeName) : WistSemanticNode
{
    public string Name { get; } = name;
    public string TypeName { get; } = typeName;
}

internal sealed class WistSemanticProgram(WistSemanticNode root)
{
    public WistSemanticNode Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
}
