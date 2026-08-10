using NumbersModule.Contracts;
using UniversalToolchain.ModuleContracts;

namespace NumbersModule.Visitors;

[AutoRegisterService]
public class NumberAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Number"))
            return;

        var numText = (data.Node.LexemeValue?.Text).NotNull().Replace("_", "");
        var num = double.Parse(numText, NumberStyles.Any);

        var method = new AbstractMethodImpl(
            $"PushNumber_{num}",
            (il, _) =>
            {
                il.Push(num);
                il.CallCSharp(typeof(RealNumberImpl).GetConstructor([typeof(double)]).NotNull());
            });
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method).WithContract(
            NumbersContractIds.Module,
            NumbersContractIds.NumberNode,
            NumbersContractIds.PushRealNumber));
    }
}
