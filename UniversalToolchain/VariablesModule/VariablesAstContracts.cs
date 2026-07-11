using BasicCore.ParserWrapper;

namespace VariablesModule;

internal static class VariablesAstContracts
{
    public const string DefinitionTag = "VariableDefinition";
    public const string DefinitionWithTypeTag = "VariableDefinitionWithType";
    public const string DefinitionWithoutTypeTag = "VariableDefinitionWithoutType";

    public static bool IsDefinition(AstNode node) => node.AllTags.Contains(DefinitionTag);

    public static bool HasDeclaredType(AstNode node) => node.AllTags.Contains(DefinitionWithTypeTag);
}
