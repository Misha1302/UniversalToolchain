namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

/// <summary>
/// Owns Acme adapter, configuration, mutation, and seeded-fault identifiers.
/// </summary>
public static class AcmePlanFuzzConstants
{
    public const string AdapterId = "acme-pricing";
    public const string AdapterVersion = "0.1.0";
    public const string LanguageId = "Acme.PricingLanguage";
    public const string LanguageVersion = "1.0.0";
    public const string GeneratorSchemaVersion = "acme-pricing-generator-v1";
    public const string ModelKind = "acme.pricing.expression";
    public const int ModelSchemaVersion = 1;
    public const string InterpreterBackend = "interpreter";
    public const string CompiledBackend = "compiled";
    public const string BaselineConfiguration = "baseline";
    public const string ReversedRegistryConfiguration = "registry-reversed";
    public const string WrongArithmeticConfiguration = "seeded-wrong-arithmetic";
    public const string RegistryOrderMutation = "PM-001-registry-order-permutation";
    public const string WrongArithmeticFault = "SF-001-wrong-backend-arithmetic";
}
