namespace UniversalToolchain.ModuleContracts;

/// <summary>
///     Describes a contract namespace reservation. Unlike the former enum, this type can
///     represent package-defined owners without changing the generic contract assembly.
/// </summary>
public sealed record ContractNamespaceOwner
{
    private ContractNamespaceOwner(string name, string? namespacePrefix, bool reservesPrefix)
    {
        Name = ContractIdentifierValidation.RequireNonEmpty(name, nameof(name));
        NamespacePrefix = NormalizePrefix(namespacePrefix);
        ReservesPrefix = reservesPrefix;

        if (reservesPrefix && NamespacePrefix == null)
            throw new ArgumentException("A namespace-reserving owner must declare a prefix.", nameof(namespacePrefix));
    }

    public static ContractNamespaceOwner Core { get; } = Reserved("core", "core");
    public static ContractNamespaceOwner Wist { get; } = Reserved("wist", "wist");
    public static ContractNamespaceOwner Backend { get; } = Reserved("backend", "backend");
    public static ContractNamespaceOwner Optimizer { get; } = Reserved("optimizer", "optimizer");
    public static ContractNamespaceOwner ThirdParty { get; } = External("third-party");

    public string Name { get; }
    public string? NamespacePrefix { get; }
    public bool ReservesPrefix { get; }

    public static ContractNamespaceOwner Reserved(string name, string namespacePrefix) =>
        new(name, namespacePrefix, true);

    public static ContractNamespaceOwner External(string name) =>
        new(name, null, false);

    public override string ToString() => Name;

    private static string? NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return null;

        var normalized = prefix.Trim().TrimEnd('.');
        ContractIdentifierValidation.RequireNonEmpty(normalized, nameof(prefix));
        return normalized + ".";
    }
}
