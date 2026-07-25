namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

/// <summary>
/// Owns Acme adapter, configuration, mutation, and seeded-fault identifiers.
/// </summary>
public static class AcmePlanFuzzConstants
{
    public const string AdapterId = "acme-pricing";
    public const string AdapterVersion = "0.4.0";
    public const string LanguageId = "Acme.PricingLanguage";
    public const string LanguageVersion = "1.0.0";
    public const string GeneratorSchemaVersion = "acme-pricing-generator-v4";
    public const string ModelKind = "acme.pricing.expression";
    public const int ModelSchemaVersion = 1;
    public const string InterpreterBackend = "interpreter";
    public const string CompiledBackend = "compiled";
    public const string BaselineConfiguration = "baseline";
    public const string ReversedRegistryConfiguration = "registry-reversed";
    public const string WrongArithmeticConfiguration = "seeded-wrong-arithmetic";
    public const string IndependentExtensionConfiguration = "independent-extension";
    public const string ExcludedActivationConfiguration = "seeded-excluded-owner-activation";
    public const string ExtensionInterferenceConfiguration = "seeded-extension-interference";
    public const string SurfaceEvidenceFailureConfiguration = "test-surface-evidence-failure";
    public const string RegistryOrderMutation = "PM-001-registry-order-permutation";
    public const string IndependentExtensionMutation = "PM-002-independent-unused-extension";
    public const string WrongArithmeticFault = "SF-001-wrong-backend-arithmetic";
    public const string ExcludedActivationFault = "SF-005-excluded-provider-activated";
    public const string ExtensionInterferenceFault = "SF-011-extension-noninterference";
    public const string CoreFeatureId = "acme.pricing.core";
    public const string IndependentFeatureId = "acme.pricing.independent-extension";
    public const string IndependentContributionId = "acme.pricing.independent-transform";
    public const string IndependentExtensionEvidenceId = "acme.pricing.extension.independent";
    public const string UnknownOwnerId = "acme.pricing.unknown-owner";
}
