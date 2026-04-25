namespace UniversalToolchain.Functions.Abstractions;

public interface IBuiltinFunctionRuntimeBindingProvider
{
    IReadOnlyList<BuiltinFunctionRuntimeBinding> GetRuntimeBindings();
}
