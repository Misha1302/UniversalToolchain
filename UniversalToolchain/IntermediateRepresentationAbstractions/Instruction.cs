using System.Collections.ObjectModel;

namespace IntermediateRepresentationAbstractions;

/// <summary>
///     Immutable AIR instruction. Operand and metadata collections are copied at construction time so verified AIR
///     cannot be mutated behind a read-only instruction-list boundary.
/// </summary>
public sealed class Instruction
{
    private readonly ReadOnlyCollection<object?> _metadata;
    private readonly ReadOnlyCollection<object?> _operands;

    public Instruction(
        UOpCode uOpCode,
        IEnumerable<object?>? operands = null,
        IEnumerable<object?>? metadata = null,
        string? comment = null)
    {
        UOpCode = uOpCode;
        _operands = new ReadOnlyCollection<object?>((operands ?? []).ToList());
        _metadata = new ReadOnlyCollection<object?>((metadata ?? []).ToList());
        Comment = comment;
    }

    public UOpCode UOpCode { get; }

    public IReadOnlyList<object?> Operands => _operands;

    public IReadOnlyList<object?> Metadata => _metadata;

    public string? Comment { get; }

    public override string ToString()
    {
        var operandsStr = _operands.Count == 0
            ? string.Empty
            : " " + string.Join(" ", _operands.Select(static operand => operand?.ToString() ?? "null"));
        var commentStr = Comment is null ? string.Empty : $" ; {Comment}";
        return $"{UOpCode}{operandsStr}{commentStr}";
    }
}
