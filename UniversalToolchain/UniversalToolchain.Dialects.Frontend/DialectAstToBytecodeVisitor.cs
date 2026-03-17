using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using ExceptionsManager;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Frontend;

/// <summary>
/// Materializes dialect AST semantics into bytecode so later stages can consume framework-native outputs.
/// </summary>
public sealed class DialectAstToBytecodeVisitor : IAstVisitor
{
    private static readonly AstNodeType ScopeType = AstNodeType.CreateOrGet("Scope");
    private readonly DialectDefinitionSliceParser _sliceParser = new();

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data == null)
        {
            Thrower.ArgumentNull(nameof(data));
        }

        var isRootScope = data.Node.NodeType == ScopeType && data.Node.Parent == null;
        if (!isRootScope)
        {
            return;
        }

        var slice = _sliceParser.Parse(data.Node);
        data.Bytecode.Instructions.Add(new BytecodeInstruction(new DialectSliceToAirConvertable(slice)));
    }
}
