namespace IntermediateRepresentationAbstractions;

public static class UOpCodeExtensions
{
    public static bool IsAnyJump(this UOpCode opcode) =>
        opcode is UOpCode.Jmp or UOpCode.JmpIf or UOpCode.JmpIfNot;
}