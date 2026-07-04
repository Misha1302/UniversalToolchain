using System.Collections.ObjectModel;
using UniversalToolchain.Ir.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

public sealed record SsaValue(SsaValueId Id, SsaTypeId Type);

public sealed record SsaBlockParameter(SsaValue Value);

public interface ISsaInstruction
{
    SsaOperationId Id { get; }

    IReadOnlyList<SsaValueId> Operands { get; }

    IReadOnlyList<SsaValue> Results { get; }

    SsaAttributeBag Attributes { get; }
}

public sealed class SsaOperation : ISsaInstruction
{
    public SsaOperation(
        SsaOperationId id,
        SsaOpId opId,
        IEnumerable<SsaValueId>? operands = null,
        IEnumerable<SsaValue>? results = null,
        SsaAttributeBag? attributes = null)
    {
        Id = id;
        OpId = opId;
        Operands = new ReadOnlyCollection<SsaValueId>((operands ?? []).ToList());
        Results = new ReadOnlyCollection<SsaValue>((results ?? []).ToList());
        Attributes = attributes ?? SsaAttributeBag.Empty;
    }

    public SsaOperationId Id { get; }

    public SsaOpId OpId { get; }

    public IReadOnlyList<SsaValueId> Operands { get; }

    public IReadOnlyList<SsaValue> Results { get; }

    public SsaAttributeBag Attributes { get; }
}

public sealed record SsaBlockTransfer(SsaBlockId Target, IReadOnlyList<SsaValueId> Arguments)
{
    public SsaBlockTransfer(SsaBlockId target, IEnumerable<SsaValueId>? arguments = null)
        : this(target, new ReadOnlyCollection<SsaValueId>((arguments ?? []).ToList()))
    {
    }
}

public enum SsaTerminatorKind
{
    Return,
    Jump,
    Branch,
    Unreachable
}

public sealed class SsaTerminator
{
    private SsaTerminator(
        SsaTerminatorKind kind,
        IEnumerable<SsaValueId>? operands = null,
        IEnumerable<SsaBlockTransfer>? transfers = null)
    {
        Kind = kind;
        Operands = new ReadOnlyCollection<SsaValueId>((operands ?? []).ToList());
        Transfers = new ReadOnlyCollection<SsaBlockTransfer>((transfers ?? []).ToList());
    }

    public SsaTerminatorKind Kind { get; }

    public IReadOnlyList<SsaValueId> Operands { get; }

    public IReadOnlyList<SsaBlockTransfer> Transfers { get; }

    public static SsaTerminator Return(IEnumerable<SsaValueId>? values = null) =>
        new(SsaTerminatorKind.Return, operands: values);

    public static SsaTerminator Jump(SsaBlockId target, IEnumerable<SsaValueId>? arguments = null) =>
        new(SsaTerminatorKind.Jump, transfers: [new SsaBlockTransfer(target, arguments)]);

    public static SsaTerminator Branch(
        SsaValueId condition,
        SsaBlockId trueTarget,
        IEnumerable<SsaValueId>? trueArguments,
        SsaBlockId falseTarget,
        IEnumerable<SsaValueId>? falseArguments) =>
        new(
            SsaTerminatorKind.Branch,
            operands: [condition],
            transfers:
            [
                new SsaBlockTransfer(trueTarget, trueArguments),
                new SsaBlockTransfer(falseTarget, falseArguments)
            ]);

    public static SsaTerminator Unreachable() => new(SsaTerminatorKind.Unreachable);
}

public sealed class SsaBlock
{
    public SsaBlock(
        SsaBlockId id,
        IEnumerable<SsaBlockParameter>? parameters = null,
        IEnumerable<SsaOperation>? operations = null,
        SsaTerminator? terminator = null,
        IEnumerable<ISsaInstruction>? instructions = null)
    {
        if (operations is not null && instructions is not null)
            throw new ArgumentException("Specify either operations or instructions, not both.", nameof(instructions));

        var instructionList = instructions?.ToList()
            ?? (operations ?? []).Cast<ISsaInstruction>().ToList();

        Id = id;
        Parameters = new ReadOnlyCollection<SsaBlockParameter>((parameters ?? []).ToList());
        Instructions = new ReadOnlyCollection<ISsaInstruction>(instructionList);
        Operations = new ReadOnlyCollection<SsaOperation>(instructionList.OfType<SsaOperation>().ToList());
        Calls = new ReadOnlyCollection<SsaCall>(instructionList.OfType<SsaCall>().ToList());
        Terminator = terminator;
    }

    public SsaBlockId Id { get; }

    public IReadOnlyList<SsaBlockParameter> Parameters { get; }

    public IReadOnlyList<ISsaInstruction> Instructions { get; }

    public IReadOnlyList<SsaOperation> Operations { get; }

    public IReadOnlyList<SsaCall> Calls { get; }

    public SsaTerminator? Terminator { get; }
}

public sealed class SsaFunction
{
    public SsaFunction(
        SsaFunctionId id,
        SsaBlockId entryBlockId,
        IEnumerable<SsaBlock> blocks,
        IEnumerable<SsaBlockParameter>? parameters = null,
        SsaTypeId? returnType = null)
    {
        Id = id;
        EntryBlockId = entryBlockId;
        Parameters = new ReadOnlyCollection<SsaBlockParameter>((parameters ?? []).ToList());
        Blocks = new ReadOnlyCollection<SsaBlock>(blocks.ToList());
        ReturnType = returnType;
    }

    public SsaFunctionId Id { get; }

    public SsaBlockId EntryBlockId { get; }

    public IReadOnlyList<SsaBlockParameter> Parameters { get; }

    public IReadOnlyList<SsaBlock> Blocks { get; }

    public SsaTypeId? ReturnType { get; }
}

public sealed class SsaModule
{
    public SsaModule(SsaModuleId id, IEnumerable<SsaFunction> functions)
    {
        Id = id;
        Functions = new ReadOnlyCollection<SsaFunction>(functions.ToList());
    }

    public SsaModuleId Id { get; }

    public IReadOnlyList<SsaFunction> Functions { get; }
}

public sealed class SsaArtifact(SsaModule module) : IIrArtifact
{
    public IrKind Kind => SsaIrKinds.Ssa;

    public SsaModule Module { get; } = module;
}
