namespace UniversalToolchain.Intrinsics.Contracts;

public interface IIntrinsicDescriptorProvider
{
    IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors();
}
