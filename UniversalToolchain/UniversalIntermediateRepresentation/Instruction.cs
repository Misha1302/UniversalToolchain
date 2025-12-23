namespace UniversalIntermediateRepresentation;

public class Instruction(
    OpCode opCode,
    List<Value>? operands = null,
    List<Value>? metadata = null,
    string? comment = null
)
{
    public OpCode OpCode { get; } = opCode;
    public List<Value> Operands { get; } = operands ?? [];
    public List<Value> Metadata { get; } = metadata ?? [];
    public string? Comment { get; } = comment;


    public override string ToString()
    {
        var operandsStr = Operands.Any()
            ? " " + string.Join(" ", Operands.Select(o => o.ToString()))
            : "";
        var commentStr = Comment != null ? $" ; {Comment}" : "";
        return $"{OpCode}{operandsStr}{commentStr}";
    }
}