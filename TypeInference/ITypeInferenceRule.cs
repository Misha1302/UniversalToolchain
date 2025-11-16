// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;

namespace TypeInference;

public interface ITypeInferenceRule
{
    int Priority { get; }
    bool CanTryToInferType(AstNode node, TypeInferenceContext context);
    Type? TryInferType(AstNode node, TypeInferenceContext context);
}