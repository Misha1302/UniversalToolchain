namespace UniversalToolchain.ModuleContracts;

public readonly record struct ContractSchemaVersion : IComparable<ContractSchemaVersion>
{
    public ContractSchemaVersion(int major, int minor)
    {
        if (major < 0)
            Thrower.Argument(nameof(major), "Contract schema major version must not be negative.");

        if (minor < 0)
            Thrower.Argument(nameof(minor), "Contract schema minor version must not be negative.");

        Major = major;
        Minor = minor;
    }

    public int Major { get; }

    public int Minor { get; }

    public int CompareTo(ContractSchemaVersion other)
    {
        var majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public static bool operator >(ContractSchemaVersion left, ContractSchemaVersion right) => left.CompareTo(right) > 0;

    public static bool operator <(ContractSchemaVersion left, ContractSchemaVersion right) => left.CompareTo(right) < 0;

    public override string ToString() => $"{Major}.{Minor}";
}
