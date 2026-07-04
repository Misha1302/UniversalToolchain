namespace UniversalToolchain.Wist;

/// <summary>
///     Represents a compiled typed Wist program.
/// </summary>
public sealed class WistProgram<TDelegate>
    where TDelegate : Delegate
{
    internal WistProgram(TDelegate compiledDelegate, WistProgramMetadata metadata)
    {
        CompiledDelegate = compiledDelegate ?? throw new ArgumentNullException(nameof(compiledDelegate));
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    /// <summary>
    ///     Gets the typed delegate produced by the selected compiled backend.
    /// </summary>
    public TDelegate CompiledDelegate { get; }

    /// <summary>
    ///     Gets stable metadata about the compiled program.
    /// </summary>
    public WistProgramMetadata Metadata { get; }
}
