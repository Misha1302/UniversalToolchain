namespace UniversalToolchain.ModuleContracts;

public sealed record BytecodeDriftReport(IReadOnlyList<ModuleBytecodeDrift> Modules)
{
    public bool HasDrift => Modules.Any(static x => x.HasDrift);
}
