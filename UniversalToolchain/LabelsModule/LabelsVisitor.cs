using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace LabelsModule;

public class LabelsVisitor(LabelsSharedData labelsSharedData) : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Label")) return;

        var name = data.Node.Text;
        var method = new AbstractMethodImpl(
            $"Label_!Intrinsic_{name}",
            (il, _) => il.SetLabel(labelsSharedData.GetGuidByName(name))
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}