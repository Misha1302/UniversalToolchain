using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicCatalog : IIntrinsicCatalog
{
    private readonly IReadOnlyDictionary<IntrinsicSymbol, IntrinsicSemanticDescriptor> _descriptors;

    public IntrinsicCatalog(IReadOnlyDictionary<IntrinsicSymbol, IntrinsicSemanticDescriptor> descriptors)
    {
        if (descriptors == null)
            Thrower.ArgumentNull(nameof(descriptors));

        _descriptors = descriptors;
    }

    public IReadOnlyCollection<IntrinsicSemanticDescriptor> All => _descriptors.Values.ToArray();

    public IntrinsicSemanticDescriptor Resolve(IntrinsicSymbol symbol)
    {
        if (!_descriptors.TryGetValue(symbol, out var descriptor))
            Thrower.InvalidOpEx($"Intrinsic semantic descriptor is not registered for symbol '{symbol}'.");

        return descriptor;
    }

    public bool TryResolve(IntrinsicSymbol symbol, out IntrinsicSemanticDescriptor descriptor)
    {
        return _descriptors.TryGetValue(symbol, out descriptor!);
    }
}
