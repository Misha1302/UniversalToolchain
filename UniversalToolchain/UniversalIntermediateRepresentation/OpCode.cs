namespace UniversalIntermediateRepresentation;

public enum OpCode : byte
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
    
    // Управление данными
    StLoc = 0x30,
    LdLoc = 0x31,

    // Мета-инструкции
    Annotate = 0x50,
    Intrinsic = 0x51
}