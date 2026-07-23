namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Declares one executable testcase variant and the expected relation to its comparison peers.
/// </summary>
public sealed class PlanFuzzPlanVariant
{
    public PlanFuzzPlanVariant(
        string variantId,
        string configurationId,
        string backendId,
        PlanFuzzVariantRole role,
        PlanFuzzExpectedRelation expectedRelation,
        string? mutationId = null,
        string? seededFaultId = null)
    {
        VariantId = RequireText(variantId, nameof(variantId));
        ConfigurationId = RequireText(configurationId, nameof(configurationId));
        BackendId = RequireText(backendId, nameof(backendId));
        Role = role;
        ExpectedRelation = expectedRelation;
        MutationId = string.IsNullOrWhiteSpace(mutationId) ? null : mutationId;
        SeededFaultId = string.IsNullOrWhiteSpace(seededFaultId) ? null : seededFaultId;
    }

    public string VariantId { get; }
    public string ConfigurationId { get; }
    public string BackendId { get; }
    public PlanFuzzVariantRole Role { get; }
    public PlanFuzzExpectedRelation ExpectedRelation { get; }
    public string? MutationId { get; }
    public string? SeededFaultId { get; }

    private static string RequireText(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Thrower.Argument<string>(parameterName, $"Argument '{parameterName}' must not be empty.");
        return value;
    }
}
