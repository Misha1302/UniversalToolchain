namespace UniversalToolchain.ModuleContracts;

public sealed record CompilerFactState
{
    public CompilerFactState(
        IReadOnlySet<CompilerFactId> available,
        IReadOnlySet<CompilerFactId> invalidated)
    {
        Available = available.ArgNotNull();
        Invalidated = invalidated.ArgNotNull();
        if (Available.Overlaps(Invalidated))
            throw new ArgumentException("A compiler fact cannot be both valid and invalid.");
    }

    public static CompilerFactState Empty { get; } = new(
        new HashSet<CompilerFactId>(),
        new HashSet<CompilerFactId>());

    public IReadOnlySet<CompilerFactId> Available { get; }

    public IReadOnlySet<CompilerFactId> Invalidated { get; }

    public CompilerFactValidity GetValidity(CompilerFactId factId)
    {
        if (Available.Contains(factId))
            return CompilerFactValidity.Valid;
        return Invalidated.Contains(factId)
            ? CompilerFactValidity.Invalid
            : CompilerFactValidity.Unknown;
    }
}
