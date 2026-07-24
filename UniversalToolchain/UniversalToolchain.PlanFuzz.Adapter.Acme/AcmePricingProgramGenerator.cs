namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

/// <summary>
/// Generates valid deterministic pricing expressions from an adapter-owned structured model.
/// </summary>
public sealed class AcmePricingProgramGenerator
{
    public AcmePricingProgramModel Generate(PlanFuzzRandom random)
    {
        random = random.ArgNotNull();
        var unitPrice = (random.NextInt32(100_000) + 1) / 100m;
        var quantity = random.NextInt32(100) + 1;
        var discount = (random.NextInt32(50_000) + 1) / 100m;
        return new AcmePricingProgramModel(unitPrice, quantity, discount);
    }
}
