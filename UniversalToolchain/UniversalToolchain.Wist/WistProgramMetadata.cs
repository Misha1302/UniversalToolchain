using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;

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
        : this(
            sourceText,
            backend,
            parameterNames,
            parameterTypes,
            returnType,
            optimizationReport,
            WistSourceRetentionPolicy.Full)
    {
    }

    internal WistProgramMetadata(
        string sourceText,
        string backend,
        IReadOnlyList<string> parameterNames,
        IReadOnlyList<Type> parameterTypes,
        Type returnType,
        WistOptimizationReport? optimizationReport,
        WistSourceRetentionPolicy sourceRetention)
    {
        ArgumentNullException.ThrowIfNull(sourceText);
        SourceRetention = RequireSourceRetention(sourceRetention);
        SourceLength = sourceText.Length;
        SourceText = sourceRetention == WistSourceRetentionPolicy.Full ? sourceText : null;
        SourceSha256 = sourceRetention == WistSourceRetentionPolicy.None
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sourceText))).ToLowerInvariant();
        Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        ParameterNames = new ReadOnlyCollection<string>(parameterNames.ToArray());
        ParameterTypes = new ReadOnlyCollection<Type>(parameterTypes.ToArray());
        ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
        OptimizationReport = optimizationReport ?? WistOptimizationReport.Disabled;
    }

    /// <summary>
    /// Gets the retention policy used when this metadata was created.
    /// </summary>
    public WistSourceRetentionPolicy SourceRetention { get; }

    /// <summary>
    /// Gets source text only when <see cref="SourceRetention"/> is <see cref="WistSourceRetentionPolicy.Full"/>.
    /// </summary>
    public string? SourceText { get; }

    /// <summary>
    /// Gets the lowercase SHA-256 of UTF-8 source bytes for Full/HashAndIdentity retention; otherwise null.
    /// This is an identity aid, not secret scrubbing.
    /// </summary>
    public string? SourceSha256 { get; }

    /// <summary>Gets the original source length in UTF-16 code units.</summary>
    public int SourceLength { get; }

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

    private static WistSourceRetentionPolicy RequireSourceRetention(WistSourceRetentionPolicy policy) =>
        Enum.IsDefined(policy)
            ? policy
            : throw new ArgumentOutOfRangeException(nameof(policy), policy, "Unknown Wist source-retention policy.");
}
