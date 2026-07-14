namespace IntermediateRepresentationAbstractions;

/// <summary>
///     A typed AIR constant. The declared type is required when the runtime value is <see langword="null" />
///     and may also be used by producers that need to preserve a wider static type.
/// </summary>
public sealed record AirConstant
{
    public AirConstant(Type declaredType, object? value)
    {
        ArgumentNullException.ThrowIfNull(declaredType);

        if (declaredType == typeof(void) || declaredType.IsByRef || declaredType.IsPointer)
            throw new ArgumentException($"Type '{declaredType}' cannot be used as an AIR constant type.", nameof(declaredType));

        if (value is null)
        {
            if (declaredType.IsValueType && Nullable.GetUnderlyingType(declaredType) is null)
                throw new ArgumentException($"Null is not valid for non-nullable value type '{declaredType}'.", nameof(value));
        }
        else if (!declaredType.IsInstanceOfType(value))
        {
            throw new ArgumentException(
                $"Constant value of runtime type '{value.GetType()}' is not assignable to declared type '{declaredType}'.",
                nameof(value));
        }

        DeclaredType = declaredType;
        Value = value;
    }

    public Type DeclaredType { get; }

    public object? Value { get; }

    public static AirConstant Null<T>() => new(typeof(T), null);
}

/// <summary>
///     Canonical access to AIR Push operands. Raw null operands are deliberately rejected because they carry no type.
/// </summary>
public static class AirPushOperand
{
    public static object Create<T>(T value) =>
        value is null ? new AirConstant(typeof(T), null) : value;

    public static Type GetDeclaredType(object? operand) => operand switch
    {
        AirConstant constant => constant.DeclaredType,
        null => throw new InvalidOperationException(
            "AIR Push contains an untyped null operand. Emit null through IGenericAbstractIR.Push<T> or AirConstant."),
        _ => operand.GetType()
    };

    public static object? GetValue(object? operand) => operand switch
    {
        AirConstant constant => constant.Value,
        null => throw new InvalidOperationException(
            "AIR Push contains an untyped null operand. Emit null through IGenericAbstractIR.Push<T> or AirConstant."),
        _ => operand
    };
}
