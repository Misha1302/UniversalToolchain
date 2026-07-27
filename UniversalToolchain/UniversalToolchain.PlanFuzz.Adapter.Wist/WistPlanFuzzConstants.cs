namespace UniversalToolchain.PlanFuzz.Adapter.Wist;

/// <summary>
/// Owns stable adapter, configuration and route identifiers for Wist Level 0.
/// </summary>
public static class WistPlanFuzzConstants
{
    public const string AdapterId = "wist-restricted-int32";
    public const string AdapterVersion = "0.2.0";
    public const string LanguageId = "UniversalToolchain.Wist.RestrictedArithmetic";
    public const string GeneratorSchemaVersion = "wist-restricted-int32-generator-v2";
    public const string ModelKind = "wist.restricted.int32.expression";
    public const int ModelSchemaVersion = 1;

    public const string InterpreterBackend = "interpreter";
    public const string CilBackend = "cil";
    public const string DisabledConfiguration = "wist.ssa.disabled";
    public const string PreferConfiguration = "wist.ssa.prefer";
    public const string RequireConfiguration = "wist.ssa.require";
    public const string SsaRouteId = "wist.air-ssa-air";

    public const string SsaPreferMutation = "RM-001-ssa-prefer";
    public const string SsaRequireMutation = "RM-002-ssa-require";
}
