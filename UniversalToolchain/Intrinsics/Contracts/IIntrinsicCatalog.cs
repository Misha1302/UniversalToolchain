namespace BasicCore.Contracts;

public interface IIntrinsicCatalog
{
    IReadOnlyCollection<IntrinsicSemanticDescriptor> All { get; }
    IntrinsicSemanticDescriptor Resolve(IntrinsicSymbol symbol);

    bool TryResolve(IntrinsicSymbol symbol, out IntrinsicSemanticDescriptor descriptor);
}