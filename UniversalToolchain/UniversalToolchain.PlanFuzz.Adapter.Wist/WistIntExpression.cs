namespace UniversalToolchain.PlanFuzz.Adapter.Wist;

public enum WistIntExpressionKind
{
    Constant,
    Parameter,
    Add,
    Subtract,
    Multiply
}

/// <summary>
/// Adapter-owned structured expression model. The generic PlanFuzz core never parses Wist source.
/// </summary>
public sealed class WistIntExpression
{
    private WistIntExpression(
        WistIntExpressionKind kind,
        int? constantValue,
        string? parameterName,
        WistIntExpression? left,
        WistIntExpression? right)
    {
        Kind = kind;
        ConstantValue = constantValue;
        ParameterName = parameterName;
        Left = left;
        Right = right;
    }

    public WistIntExpressionKind Kind { get; }
    public int? ConstantValue { get; }
    public string? ParameterName { get; }
    public WistIntExpression? Left { get; }
    public WistIntExpression? Right { get; }

    public static WistIntExpression Constant(int value) =>
        new(WistIntExpressionKind.Constant, value, null, null, null);

    public static WistIntExpression Parameter(string name = "x")
    {
        if (string.IsNullOrWhiteSpace(name))
            return Thrower.Argument<WistIntExpression>(nameof(name), "Parameter name must not be empty.");
        return new WistIntExpression(WistIntExpressionKind.Parameter, null, name, null, null);
    }

    public static WistIntExpression Add(WistIntExpression left, WistIntExpression right) =>
        Binary(WistIntExpressionKind.Add, left, right);

    public static WistIntExpression Subtract(WistIntExpression left, WistIntExpression right) =>
        Binary(WistIntExpressionKind.Subtract, left, right);

    public static WistIntExpression Multiply(WistIntExpression left, WistIntExpression right) =>
        Binary(WistIntExpressionKind.Multiply, left, right);

    public bool UsesParameter =>
        Kind == WistIntExpressionKind.Parameter ||
        (Left?.UsesParameter ?? false) ||
        (Right?.UsesParameter ?? false);

    public string Render() => Kind switch
    {
        WistIntExpressionKind.Constant => RenderConstant(ConstantValue!.Value),
        WistIntExpressionKind.Parameter => ParameterName!,
        WistIntExpressionKind.Add => $"({Left!.Render()} + {Right!.Render()})",
        WistIntExpressionKind.Subtract => $"({Left!.Render()} - {Right!.Render()})",
        WistIntExpressionKind.Multiply => $"({Left!.Render()} * {Right!.Render()})",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };

    public int Evaluate(int parameterValue) => Kind switch
    {
        WistIntExpressionKind.Constant => ConstantValue!.Value,
        WistIntExpressionKind.Parameter => parameterValue,
        WistIntExpressionKind.Add => checked(Left!.Evaluate(parameterValue) + Right!.Evaluate(parameterValue)),
        WistIntExpressionKind.Subtract => checked(Left!.Evaluate(parameterValue) - Right!.Evaluate(parameterValue)),
        WistIntExpressionKind.Multiply => checked(Left!.Evaluate(parameterValue) * Right!.Evaluate(parameterValue)),
        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };

    internal void Write(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", Kind.ToString());
        if (ConstantValue.HasValue)
            writer.WriteNumber("value", ConstantValue.Value);
        if (ParameterName != null)
            writer.WriteString("name", ParameterName);
        if (Left != null)
        {
            writer.WritePropertyName("left");
            Left.Write(writer);
        }
        if (Right != null)
        {
            writer.WritePropertyName("right");
            Right.Write(writer);
        }
        writer.WriteEndObject();
    }

    internal static WistIntExpression Read(JsonElement element)
    {
        var kind = Enum.Parse<WistIntExpressionKind>(element.GetProperty("kind").GetString().NotNull(), ignoreCase: false);
        return kind switch
        {
            WistIntExpressionKind.Constant => Constant(element.GetProperty("value").GetInt32()),
            WistIntExpressionKind.Parameter => Parameter(element.GetProperty("name").GetString().NotNull()),
            WistIntExpressionKind.Add => Add(Read(element.GetProperty("left")), Read(element.GetProperty("right"))),
            WistIntExpressionKind.Subtract => Subtract(Read(element.GetProperty("left")), Read(element.GetProperty("right"))),
            WistIntExpressionKind.Multiply => Multiply(Read(element.GetProperty("left")), Read(element.GetProperty("right"))),
            _ => Thrower.NotSupported<WistIntExpression>($"Unsupported Wist expression kind '{kind}'.")
        };
    }

    private static WistIntExpression Binary(
        WistIntExpressionKind kind,
        WistIntExpression left,
        WistIntExpression right) =>
        new(kind, null, null, left.ArgNotNull(), right.ArgNotNull());

    private static string RenderConstant(int value) => value < 0
        ? $"({value.ToString(CultureInfo.InvariantCulture)})"
        : value.ToString(CultureInfo.InvariantCulture);
}
