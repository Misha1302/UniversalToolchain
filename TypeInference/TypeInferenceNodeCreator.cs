// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCore.ParserWrapper;
using BasicTypesExtensions;

namespace TypeInference;

public class TypeInferenceNodeCreator : IAstNodeCreator
{
    public ExtensibleEnum<AstNodeTag> AstNodeType { get; } =
        ExtensibleEnum<AstNodeTag>.CreateOrGet("VariableDefinitionWithoutType");

    public bool TryCreateNode(AstNode scope, int childIndex)
    {
        var identifierNode = scope.SafeGet(childIndex);
        var equalityNode = scope.SafeGet(childIndex + 1);
        var expressionNode = scope.SafeGet(childIndex + 2);

        if (identifierNode?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Identifier"))
            return false;

        if (equalityNode?.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"))
            return false;

        if (expressionNode == null)
            return false;

        // Проверяем, что это не сравнение (должно быть первое присваивание переменной)
        if (scope.Children.Take(childIndex).Any(n =>
                n.NodeType == ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable") &&
                n.Text == identifierNode.Text))
            return false;

        // Создаем новый узел переменной без типа вместо изменения существующего
        var variableNode = new AstNode(
            AstNodeType,
            identifierNode.LexemeValue,
            [expressionNode]
        );
        variableNode.AddTag("VariableDefinitionWithoutType");

        // Заменяем идентификатор на новый узел переменной
        scope.Children[childIndex] = variableNode;

        // Удаляем узел равенства и выражение из scope
        scope.Children.RemoveAt(childIndex + 1); // Удаляем equality
        scope.Children.RemoveAt(childIndex + 1); // Удаляем expression

        return true;
    }
}