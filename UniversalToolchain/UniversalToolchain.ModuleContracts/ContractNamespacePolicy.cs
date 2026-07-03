namespace UniversalToolchain.ModuleContracts;

public static class ContractNamespacePolicy
{
    public static IReadOnlyList<ToolchainDiagnostic> ValidateOwnership(ContractId id, ContractNamespaceOwner owner)
    {
        id = id.ArgNotNull();

        var expectedPrefix = GetExpectedPrefix(owner);
        if (expectedPrefix == null)
            return ValidateThirdParty(id);

        if (id.Namespace.StartsWith(expectedPrefix, StringComparison.Ordinal))
            return [];

        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.InvalidNamespaceOwnership,
                ToolchainDiagnosticSeverity.Error,
                $"Contract id '{id}' is owned by '{owner}' but does not use namespace prefix '{expectedPrefix}'.",
                null,
                [new ToolchainDiagnosticHint("Move the id to its owning namespace or change the declared owner.")])
        ];
    }

    private static IReadOnlyList<ToolchainDiagnostic> ValidateThirdParty(ContractId id)
    {
        if (!HasReservedPrefix(id.Namespace))
            return [];

        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.InvalidNamespaceOwnership,
                ToolchainDiagnosticSeverity.Error,
                $"Third-party contract id '{id}' must not use reserved namespace '{id.Namespace}'.",
                null,
                [new ToolchainDiagnosticHint("Use a package-owned namespace outside core.*, wist.*, backend.* and optimizer.*.")])
        ];
    }

    private static string? GetExpectedPrefix(ContractNamespaceOwner owner) => owner switch
    {
        ContractNamespaceOwner.Core => "core.",
        ContractNamespaceOwner.Wist => "wist.",
        ContractNamespaceOwner.Backend => "backend.",
        ContractNamespaceOwner.Optimizer => "optimizer.",
        ContractNamespaceOwner.ThirdParty => null,
        _ => Thrower.InvalidOpEx<string>("Unknown contract namespace owner.")
    };

    private static bool HasReservedPrefix(string value) =>
        value.StartsWith("core.", StringComparison.Ordinal) ||
        value.StartsWith("wist.", StringComparison.Ordinal) ||
        value.StartsWith("backend.", StringComparison.Ordinal) ||
        value.StartsWith("optimizer.", StringComparison.Ordinal);
}
