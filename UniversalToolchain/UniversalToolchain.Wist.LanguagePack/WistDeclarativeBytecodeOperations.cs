using AbstractIrExtensions;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;

namespace UniversalToolchain.Wist.LanguagePack;

internal sealed record WistStoreLocalOperation(string StorageKey) : IBytecodeOperationData;
internal sealed record WistStoreExternalOperation(int Slot, Type ValueType) : IBytecodeOperationData;
internal sealed record WistJumpOperation(Guid Label) : IBytecodeOperationData;
internal sealed record WistJumpIfFalseOperation(Guid Label) : IBytecodeOperationData;
internal sealed record WistLabelOperation(Guid Label) : IBytecodeOperationData;
internal sealed record WistDynamicArithmeticOperation(string MethodName) : IBytecodeOperationData;

internal sealed class WistStoreLocalOperationHandler : BytecodeOperationHandler<WistStoreLocalOperation>
{
    protected override IAbstractIR Emit(WistStoreLocalOperation operation, IAbstractMethodConvertable.Context context)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("Assignment requires a value on the stack.");
        var air = new AbstractIR();
        air.SetValueToLocal(operation.StorageKey, context.Stack[^1]);
        return air;
    }
}

internal sealed class WistStoreExternalOperationHandler : BytecodeOperationHandler<WistStoreExternalOperation>
{
    protected override IAbstractIR Emit(WistStoreExternalOperation operation, IAbstractMethodConvertable.Context context)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx("Assignment requires a value on the stack.");
        var air = new AbstractIR();
        air.StExternal(operation.Slot, operation.ValueType);
        return air;
    }
}

internal sealed class WistJumpOperationHandler : BytecodeOperationHandler<WistJumpOperation>
{
    protected override IAbstractIR Emit(WistJumpOperation operation, IAbstractMethodConvertable.Context context)
    {
        var air = new AbstractIR();
        air.Jmp(operation.Label);
        return air;
    }
}

internal sealed class WistJumpIfFalseOperationHandler : BytecodeOperationHandler<WistJumpIfFalseOperation>
{
    protected override IAbstractIR Emit(WistJumpIfFalseOperation operation, IAbstractMethodConvertable.Context context)
    {
        var air = new AbstractIR();
        air.JmpIfNot(operation.Label);
        return air;
    }
}

internal sealed class WistLabelOperationHandler : BytecodeOperationHandler<WistLabelOperation>
{
    protected override IAbstractIR Emit(WistLabelOperation operation, IAbstractMethodConvertable.Context context)
    {
        var air = new AbstractIR();
        air.SetLabel(operation.Label);
        return air;
    }
}

internal sealed class WistDynamicArithmeticOperationHandler : BytecodeOperationHandler<WistDynamicArithmeticOperation>
{
    protected override IAbstractIR Emit(WistDynamicArithmeticOperation operation, IAbstractMethodConvertable.Context context)
    {
        if (context.Stack.Count == 0)
            Thrower.InvalidOpEx($"Dynamic arithmetic '{operation.MethodName}' requires an operand type on the stack.");
        var air = new AbstractIR();
        air.CallCSharp(context.Stack[^1].GetMethod(operation.MethodName).NotNull());
        return air;
    }
}
