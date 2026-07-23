namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

/// <summary>
/// Represents the structured source model owned by the Acme adapter.
/// </summary>
public sealed record AcmePricingProgramModel(decimal UnitPrice, decimal Quantity, decimal Discount)
{
    public string RenderSource() =>
        $"{Format(UnitPrice)} * {Format(Quantity)} - {Format(Discount)}";

    public PlanFuzzPayload ToPayload()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("discount", Discount);
            writer.WriteNumber("quantity", Quantity);
            writer.WriteNumber("unitPrice", UnitPrice);
            writer.WriteEndObject();
        }
        return PlanFuzzPayload.FromJson(Encoding.UTF8.GetString(stream.ToArray()));
    }

    public static AcmePricingProgramModel FromPayload(PlanFuzzPayload payload)
    {
        payload = payload.ArgNotNull();
        using var document = payload.Parse();
        var root = document.RootElement;
        return new AcmePricingProgramModel(
            root.GetProperty("unitPrice").GetDecimal(),
            root.GetProperty("quantity").GetDecimal(),
            root.GetProperty("discount").GetDecimal());
    }

    private static string Format(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
}
