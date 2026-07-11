using System.Collections.ObjectModel;

namespace UniversalToolchain.Wist;

/// <summary>
///     Describes a typed compiled Wist program without exposing backend-specific artifacts.
/// </summary>
public sealed class WistProgramMetadata
{
    public WistProgramMetadata(
        string sourceText,
        string backend,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<Type> parameterTypes,
        Type returnType,
        WistOptimizationReport? optimizationReport = null)
    {
        SourceText = sourceText ?? throw new ArgumentNullException(nameof(sourceText));
        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        ParameterNames = new ReadOnlyCollection<string>(parameterNames.ToArray());
        ParameterTypes = new ReadOnlyCollection<Type>(parameterTypes.ToArray());
        ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
        OptimizationReport = optimizationReport ?? WistOptimizationReport.Disabled;
    }

    /// <summary>
    ///     Gets source text used to produce the compiled program.
    /// </summary>
    public string SourceText { get; }

    /// <summary>
    ///     Gets the selected public backend alias.
    /// </summary>
    public string Backend { get; }

    /// <summary>
    ///     Gets stable parameter names in delegate invocation order.
    /// </summary>
    public IReadOnlyList<string> ParameterNames { get; }

    /// <summary>
    ///     Gets stable parameter types in delegate invocation order.
    /// </summary>
    public IReadOnlyList<Type> ParameterTypes { get; }

    /// <summary>
    ///     Gets delegate return type.
    /// </summary>
    public Type ReturnType { get; }

    /// <summary>
    /// Gets the observed optimization-route report for this compilation.
    /// </summary>
    public WistOptimizationReport OptimizationReport { get; }
}
