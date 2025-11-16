// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace TypeInference.Rules;

public class BinaryOperationTypeInferenceRule : ITypeInferenceRule
{
    public int Priority => 800;

    public bool CanTryToInferType(AstNode node, TypeInferenceContext context)
    {
        var nodeType = node.NodeType;
        return nodeType == ExtensibleEnum<AstNodeTag>.Get("Addition") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Substraction") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Multiplication") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Division") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Equal") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("NotEqual") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Greater") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Less") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("GreaterOrEqual") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("LessOrEqual") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("And") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Or");
    }

    public Type? TryInferType(AstNode node, TypeInferenceContext context)
    {
        var resolver = new ExpressionTypeResolver();

        if (IsArithmeticOperation(node.NodeType))
        {
            if (node.Children.Count < 2)
                throw new TypeInferenceException($"Arithmetic operation '{node.NodeType}' requires two operands");

            var leftType = resolver.ResolveExpressionType(node.Children[0], context);
            var rightType = resolver.ResolveExpressionType(node.Children[1], context);

            if (!AreTypesCompatible(leftType, rightType))
                throw new TypeInferenceException(
                    $"Incompatible types for arithmetic operation: {leftType.Name} and {rightType.Name}");

            return leftType;
        }

        if (IsComparisonOperation(node.NodeType) || IsLogicalOperation(node.NodeType)) return typeof(bool);

        return null;
    }

    private bool IsArithmeticOperation(ExtensibleEnum<AstNodeTag> nodeType)
    {
        return nodeType == ExtensibleEnum<AstNodeTag>.Get("Addition") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Substraction") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Multiplication") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Division");
    }

    private bool IsComparisonOperation(ExtensibleEnum<AstNodeTag> nodeType)
    {
        return nodeType == ExtensibleEnum<AstNodeTag>.Get("Equal") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("NotEqual") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Greater") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Less") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("GreaterOrEqual") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("LessOrEqual");
    }

    private bool IsLogicalOperation(ExtensibleEnum<AstNodeTag> nodeType)
    {
        return nodeType == ExtensibleEnum<AstNodeTag>.Get("And") ||
               nodeType == ExtensibleEnum<AstNodeTag>.Get("Or");
    }

    private bool AreTypesCompatible(Type left, Type right)
    {
        return left == right;
    }
}