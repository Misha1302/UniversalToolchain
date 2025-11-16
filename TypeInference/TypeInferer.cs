// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using TypeInference.Rules;

namespace TypeInference;

public class TypeInferer
{
    private readonly ExpressionTypeResolver _resolver;

    public TypeInferer()
    {
        _resolver = new ExpressionTypeResolver();
        // Регистрируем правила
        _resolver.RegisterRule(new UnaryOperationTypeInferenceRule());
        _resolver.RegisterRule(new BinaryOperationTypeInferenceRule());
        _resolver.RegisterRule(new FunctionCallTypeInferenceRule());
        _resolver.RegisterRule(new ScopeTypeInferenceRule());
    }

    public void RegisterRule(ITypeInferenceRule rule)
    {
        _resolver.RegisterRule(rule);
    }

    public AstNode InferTypes(AstNode root)
    {
        var context = new TypeInferenceContext();
        return InferTypesRecursive(root, context);
    }

    private AstNode InferTypesRecursive(AstNode node, TypeInferenceContext context)
    {
        for (var i = 0; i < node.Children.Count; i++)
        {
            node.Children[i] = InferTypesRecursive(node.Children[i], context);

            if (node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality")) continue;
            if (!node.SafeGet(0)?.CurrentTags.Contains("VariableDefinitionWithoutType") ?? false) continue;
            if (node[0].CurrentTags.Contains("Type")) continue;
            if (i + 1 >= node.Children.Count) continue;

            ProcessVariableWithoutType(node.Children[i], node.Children[i + 1], context);
        }

        if (_resolver.TryResolveExpressionType(node, context, out var type))
        {
            node.Data.Set("Type", type);
            node.Data.Set("TypeAsStr", GetTypeDisplayName(type));
        }

        return node;
    }

    private AstNode ProcessVariableWithoutType(AstNode node, AstNode initExpression, TypeInferenceContext context)
    {
        var variableName = node.Text;

        try
        {
            var inferredType = _resolver.ResolveExpressionType(initExpression, context);
            context.DeclareVariable(variableName, inferredType);

            node.Data.Set("Type", inferredType);
            node.Data.Set("TypeAsStr", GetTypeDisplayName(inferredType));

            return node;
        }
        catch (TypeInferenceException ex)
        {
            throw new TypeInferenceException($"Cannot infer type for variable '{variableName}'. " +
                                             $"Please specify type explicitly. Reason: {ex.Message}");
        }
    }

    private string GetTypeDisplayName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetTypeDisplayName));
        return $"{type.Name[..type.Name.IndexOf('`')]}<{genericArgs}>";
    }
}