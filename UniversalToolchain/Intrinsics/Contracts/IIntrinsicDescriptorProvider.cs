namespace BasicCore.Contracts;

public interface IIntrinsicDescriptorProvider
{
    IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors();
}