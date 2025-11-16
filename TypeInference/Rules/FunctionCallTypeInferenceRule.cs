// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using AssemblyFinder;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace TypeInference.Rules;

public class FunctionCallTypeInferenceRule : ITypeInferenceRule
{
    public int Priority => 700;

    public bool CanTryToInferType(AstNode node, TypeInferenceContext context)
    {
        return node.NodeType == ExtensibleEnum<AstNodeTag>.Get("CSharpFunctionCall");
    }

    public Type TryInferType(AstNode node, TypeInferenceContext context)
    {
        var methodName = node.Text;
        var method = MethodsFinder.GetMethod(methodName);

        if (method == null)
            throw new TypeInferenceException($"Method '{methodName}' not found");

        return method.ReturnType;
    }
}