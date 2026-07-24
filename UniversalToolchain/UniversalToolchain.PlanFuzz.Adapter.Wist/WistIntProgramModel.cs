namespace UniversalToolchain.PlanFuzz.Adapter.Wist;

/// <summary>
/// Stores one restricted Int32 expression, its deterministic input and generation provenance.
/// </summary>
public sealed record WistIntProgramModel(WistIntExpression Expression, int ParameterValue, string Origin)
{
    public bool UsesParameter => Expression.ArgNotNull().UsesParameter;

    public string RenderSource() => Expression.Render();

    public int EvaluateReference() => Expression.Evaluate(ParameterValue);

    public PlanFuzzPayload ToPayload()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("expression");
            Expression.Write(writer);
            writer.WriteString("origin", Origin);
            writer.WriteNumber("parameterValue", ParameterValue);
            writer.WriteEndObject();
        }
        return PlanFuzzPayload.FromJson(Encoding.UTF8.GetString(stream.ToArray()));
    }

    public static WistIntProgramModel FromPayload(PlanFuzzPayload payload)
    {
        payload = payload.ArgNotNull();
        using var document = payload.Parse();
        var root = document.RootElement;
        return new WistIntProgramModel(
            WistIntExpression.Read(root.GetProperty("expression")),
            root.GetProperty("parameterValue").GetInt32(),
            root.GetProperty("origin").GetString().NotNull());
    }
}
