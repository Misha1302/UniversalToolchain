using System.Reflection;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.ModuleContracts;

/// <summary>
/// Supplies a stable, explicit module identity for a runtime-exported component that does not
/// publish richer contract facets itself. Components without an exact runtime export are rejected.
/// </summary>
public sealed class DeclaredRuntimeComponentContractDescriptorProvider : IModuleContractDescriptorProvider
{
    private readonly IReadOnlyList<IModuleContractFacet> _facets;

    public DeclaredRuntimeComponentContractDescriptorProvider(Type componentType)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        var export = componentType.GetCustomAttribute<DialectRuntimeExportAttribute>(inherit: false)
                     ?? throw new InvalidOperationException(
                         $"Runtime component '{componentType.FullName ?? componentType.Name}' must declare DialectRuntimeExportAttribute or implement IModuleContractDescriptorProvider.");
        ModuleId = CreateModuleId(export.ComponentKind, export.CanonicalAlias);
        _facets = [new VerifierContractFacet(ModuleId, [])];
    }

    public ModuleId ModuleId { get; }

    public IReadOnlyList<IModuleContractFacet> GetFacets() => _facets;

    private static ModuleId CreateModuleId(string componentKind, string canonicalAlias)
    {
        static string Normalize(string value)
        {
            var chars = value.Trim().Select(static ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '.').ToArray();
            var normalized = new string(chars);
            while (normalized.Contains("..", StringComparison.Ordinal))
                normalized = normalized.Replace("..", ".", StringComparison.Ordinal);
            normalized = normalized.Trim('.');
            return string.IsNullOrWhiteSpace(normalized) ? "unknown" : normalized;
        }

        return new ModuleId($"runtime.{Normalize(componentKind)}.{Normalize(canonicalAlias)}");
    }
}
