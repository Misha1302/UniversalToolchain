namespace BasicCore.Execution;

/// <summary>
///     Represents a prepared compiled artifact with mutable argument bindings and executable runtime.
/// </summary>
public interface ICompiledArtifactSession
{
    /// <summary>
    ///     Gets the total number of argument slots exposed by this session.
    /// </summary>
    int ArgumentCount { get; }

    /// <summary>
    ///     Sets an argument value by slot index.
    /// </summary>
    /// <param name="slot">Zero-based slot index.</param>
    /// <param name="value">Argument value to assign.</param>
    void SetArgument(int slot, object? value);

    /// <summary>
    ///     Sets an argument value by argument name.
    /// </summary>
    /// <param name="name">Argument name.</param>
    /// <param name="value">Argument value to assign.</param>
    void SetArgument(string name, object? value);

    /// <summary>
    ///     Executes the compiled artifact with current arguments.
    /// </summary>
    /// <returns>Execution result.</returns>
    object? Run();
}