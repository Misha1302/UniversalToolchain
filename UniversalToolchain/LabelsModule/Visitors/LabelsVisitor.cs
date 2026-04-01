using ExceptionsManager;

namespace LabelsModule.Visitors;

public class LabelsVisitor(LabelsSharedData labelsSharedData) : IAstVisitor
{
    private readonly HashSet<string> _markedLabels = [];

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.Get("Label")) return;

        var name = data.Node.Text;
        if (_markedLabels.Contains(name)) Thrower.MultipleDefinition($"label '{name}''");
        _markedLabels.Add(name);
        var method = new AbstractMethodImpl(
            $"Label_!Intrinsic_{name}",
            (il, _) => il.SetLabel(labelsSharedData.GetGuidByName(name))
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}