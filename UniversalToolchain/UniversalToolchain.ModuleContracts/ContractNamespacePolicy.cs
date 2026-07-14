namespace UniversalToolchain.ModuleContracts;

public static class ContractNamespacePolicy
{
    private static readonly IReadOnlyList<ContractNamespaceOwner> BuiltInReservations =
    [
        ContractNamespaceOwner.Core,
        ContractNamespaceOwner.Wist,
        ContractNamespaceOwner.Backend,
        ContractNamespaceOwner.Optimizer
    ];

    public static IReadOnlyList<ToolchainDiagnostic> ValidateOwnership(
        ContractId id,
        ContractNamespaceOwner owner,
        IEnumerable<ContractNamespaceOwner>? namespaceReservations = null) =>
        ValidateOwnership(id.ArgNotNull().FullName, owner, namespaceReservations);

    public static IReadOnlyList<ToolchainDiagnostic> ValidateOwnership(
        string identifier,
        ContractNamespaceOwner owner,
        IEnumerable<ContractNamespaceOwner>? namespaceReservations = null)
    {
        identifier = ContractIdentifierValidation.RequireNonEmpty(identifier, nameof(identifier));
        owner = owner.ArgNotNull();

        var reservations = NormalizeReservations(namespaceReservations);
        if (!owner.ReservesPrefix)
            return ValidateExternalOwner(identifier, owner, reservations);

        var expectedPrefix = owner.NamespacePrefix!;
        if (IsWithinNamespace(identifier, expectedPrefix))
            return [];

        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.InvalidNamespaceOwnership,
                ToolchainDiagnosticSeverity.Error,
                $"Contract identifier '{identifier}' is owned by '{owner}' but does not use namespace '{expectedPrefix}'.",
                null,
                [new ToolchainDiagnosticHint("Move the identifier to one of the module's declared namespace reservations or change the provider declaration.")])
        ];
    }

    internal static bool IsWithinNamespace(string identifier, string normalizedPrefix)
    {
        var root = normalizedPrefix.TrimEnd('.');
        return string.Equals(identifier, root, StringComparison.Ordinal) ||
               identifier.StartsWith(normalizedPrefix, StringComparison.Ordinal);
    }

    internal static IReadOnlyList<ContractNamespaceOwner> NormalizeReservations(
        IEnumerable<ContractNamespaceOwner>? reservations) =>
        BuiltInReservations
            .Concat(reservations ?? [])
            .Where(static owner => owner.ReservesPrefix)
            .DistinctBy(static owner => owner.NamespacePrefix, StringComparer.Ordinal)
            .OrderBy(static owner => owner.NamespacePrefix, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<ToolchainDiagnostic> ValidateExternalOwner(
        string identifier,
        ContractNamespaceOwner owner,
        IReadOnlyList<ContractNamespaceOwner> reservations)
    {
        var conflictingOwner = reservations.FirstOrDefault(reservation =>
            reservation.NamespacePrefix != null &&
            IsWithinNamespace(identifier, reservation.NamespacePrefix));

        if (conflictingOwner == null)
            return [];

        return
        [
            new ToolchainDiagnostic(
                ModuleContractDiagnosticCodes.InvalidNamespaceOwnership,
                ToolchainDiagnosticSeverity.Error,
                $"Contract identifier '{identifier}' owned by '{owner}' must not use namespace reserved by '{conflictingOwner}'.",
                null,
                [new ToolchainDiagnosticHint("Use a package-owned namespace or declare an explicit namespace reservation for the package.")])
        ];
    }
}
