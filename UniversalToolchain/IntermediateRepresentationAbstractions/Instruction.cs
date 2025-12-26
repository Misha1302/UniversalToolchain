namespace IntermediateRepresentationAbstractions;

public class Instruction(
    UOpCode uOpCode,
    List<object>? operands = null,
    List<object>? metadata = null,
    string? comment = null
)
{
    public UOpCode UOpCode { get; } = uOpCode;
    public List<object> Operands { get; } = operands ?? [];
    public List<object> Metadata { get; } = metadata ?? [];
    public string? Comment { get; } = comment;


    public override string ToString()
    {
        var operandsStr = Operands.Any()
            ? " " + string.Join(" ", Operands.Select(o => o.ToString()))
            : "";
        var commentStr = Comment != null ? $" ; {Comment}" : "";
        return $"{UOpCode}{operandsStr}{commentStr}";
    }
}