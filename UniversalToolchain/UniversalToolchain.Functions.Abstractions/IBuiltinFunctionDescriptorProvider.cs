namespace UniversalToolchain.Functions.Abstractions;

public interface IBuiltinFunctionDescriptorProvider
{
    IReadOnlyList<BuiltinFunctionDescriptor> GetFunctions();
}