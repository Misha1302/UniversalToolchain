namespace IntermediateRepresentationAbstractions;

public enum UOpCode : byte
{
    Nop = 0x00,

    // Стековые операции
    Push = 0x10,
    Drop = 0x11,

    // Управление потоком
    Jmp = 0x20,
    JmpIf = 0x21,
    JmpIfNot = 0x22,
    Label = 0x23,

    // Мета-инструкции
    Annotate = 0x50,
    Intrinsic = 0x51
}