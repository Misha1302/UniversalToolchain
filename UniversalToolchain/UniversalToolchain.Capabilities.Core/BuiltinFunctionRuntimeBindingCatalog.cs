using System.Collections.ObjectModel;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class BuiltinFunctionRuntimeBindingCatalog
{
    private readonly ReadOnlyCollection<BuiltinFunctionRuntimeBinding> _runtimeBindings;

    public BuiltinFunctionRuntimeBindingCatalog(IEnumerable<BuiltinFunctionRuntimeBinding> runtimeBindings)
    {
        ArgumentNullException.ThrowIfNull(runtimeBindings);

        _runtimeBindings = new ReadOnlyCollection<BuiltinFunctionRuntimeBinding>(runtimeBindings
            .OrderBy(static x => x.Signature.Name, StringComparer.Ordinal)
            .ThenBy(static x => x.Signature.ParameterTypes.Count)
            .ThenBy(static x => string.Join("|", x.Signature.ParameterTypes.Select(static y => y.Name)), StringComparer.Ordinal)
            .ToList());
    }

    public IReadOnlyList<BuiltinFunctionRuntimeBinding> RuntimeBindings => _runtimeBindings;

    public IReadOnlyList<BuiltinFunctionRuntimeBinding> FindMatchingBindings(string name, IReadOnlyList<FunctionTypeDescriptor> parameterTypes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parameterTypes);

        return _runtimeBindings
            .Where(x => string.Equals(x.Signature.Name, name, StringComparison.Ordinal))
            .Where(x => x.Signature.ParameterTypes.Count == parameterTypes.Count)
            .Where(x => x.Signature.ParameterTypes.Select(static y => y.Name)
                .SequenceEqual(parameterTypes.Select(static y => y.Name), StringComparer.Ordinal))
            .ToList();
    }
}
