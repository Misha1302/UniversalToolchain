namespace UniversalToolchain.ModuleContracts;

public sealed record CompilerFactState
{
    public CompilerFactState(
        IReadOnlySet<CompilerFactId> available,
        IReadOnlySet<CompilerFactId> invalidated)
    {
        Available = available.ArgNotNull();
        Invalidated = invalidated.ArgNotNull();
    }

    public static CompilerFactState Empty { get; } = new(
        new HashSet<CompilerFactId>(),
        new HashSet<CompilerFactId>());

    public IReadOnlySet<CompilerFactId> Available { get; }

    public IReadOnlySet<CompilerFactId> Invalidated { get; }
}
