using UniversalToolchain.Features.Core;

namespace UniversalToolchain.Functions.Abstractions;

public interface IBuiltinFunctionCatalog
{
    IReadOnlyList<BuiltinFunctionDescriptor> GetFunctions();

    BuiltinFunctionResolution Resolve(
        string name,
        IReadOnlyList<FunctionTypeDescriptor> argumentTypes,
        DialectFeatureExplanation featureExplanation,
        string backendAlias);
}
