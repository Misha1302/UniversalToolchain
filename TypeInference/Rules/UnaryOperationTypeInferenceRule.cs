// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace TypeInference.Rules;

public class UnaryOperationTypeInferenceRule : ITypeInferenceRule
{
    public int Priority => 850;

    public bool CanTryToInferType(AstNode node, TypeInferenceContext context)
    {
        // Проверяем унарный минус и другие унарные операции
        return node.NodeType == ExtensibleEnum<AstNodeTag>.Get("Substraction") &&
               node.Children.Count == 1;
    }

    public Type TryInferType(AstNode node, TypeInferenceContext context)
    {
        var resolver = new ExpressionTypeResolver();
        var operandType = resolver.ResolveExpressionType(node.Children[0], context);

        // Для унарного минуса тип результата такой же как у операнда
        return operandType;
    }
}