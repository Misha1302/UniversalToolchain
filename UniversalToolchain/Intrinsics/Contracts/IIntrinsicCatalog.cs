namespace UniversalToolchain.Intrinsics.Contracts;

public interface IIntrinsicCatalog
{
    IntrinsicSemanticDescriptor Resolve(IntrinsicSymbol symbol);

    bool TryResolve(IntrinsicSymbol symbol, out IntrinsicSemanticDescriptor descriptor);

    IReadOnlyCollection<IntrinsicSemanticDescriptor> All { get; }
}
