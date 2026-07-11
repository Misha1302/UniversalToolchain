using System.Collections.ObjectModel;
using System.Reflection;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Emission;

public enum SsaCallableLoweringTargetKind
{
    AirIntrinsic,
    ManagedCall,
    CilOpcode,
    InterpreterPrimitive,
    Reject
}

public sealed class SsaCallableLoweringTarget
{
    private SsaCallableLoweringTarget(
        CallableId callable,
        SsaCallableLoweringTargetKind kind,
        string? targetId,
        string? diagnosticCode,
        string? diagnosticMessage)
    {
        Callable = callable;
        Kind = kind;
        TargetId = targetId;
        DiagnosticCode = diagnosticCode;
        DiagnosticMessage = diagnosticMessage;
    }

    public CallableId Callable { get; }

    public SsaCallableLoweringTargetKind Kind { get; }

    public string? TargetId { get; }

    public string? DiagnosticCode { get; }

    public string? DiagnosticMessage { get; }

    public static SsaCallableLoweringTarget AirIntrinsic(CallableId callable, string intrinsicId)
    {
        if (string.IsNullOrWhiteSpace(intrinsicId))
            throw new ArgumentException("AIR intrinsic identifier must not be empty.", nameof(intrinsicId));

        return new SsaCallableLoweringTarget(callable, SsaCallableLoweringTargetKind.AirIntrinsic, intrinsicId.Trim(), null, null);
    }

    public static SsaCallableLoweringTarget ManagedCall(CallableId callable) =>
        new(callable, SsaCallableLoweringTargetKind.ManagedCall, null, null, null);

    public static SsaCallableLoweringTarget CilOpcode(CallableId callable, string opcodeId)
    {
        if (string.IsNullOrWhiteSpace(opcodeId))
            throw new ArgumentException("CIL opcode identifier must not be empty.", nameof(opcodeId));

        return new SsaCallableLoweringTarget(callable, SsaCallableLoweringTargetKind.CilOpcode, opcodeId.Trim(), null, null);
    }

    public static SsaCallableLoweringTarget InterpreterPrimitive(CallableId callable, string primitiveId)
    {
        if (string.IsNullOrWhiteSpace(primitiveId))
            throw new ArgumentException("Interpreter primitive identifier must not be empty.", nameof(primitiveId));

        return new SsaCallableLoweringTarget(callable, SsaCallableLoweringTargetKind.InterpreterPrimitive, primitiveId.Trim(), null, null);
    }

    public static SsaCallableLoweringTarget Reject(CallableId callable, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Reject diagnostic code must not be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Reject diagnostic message must not be empty.", nameof(message));

        return new SsaCallableLoweringTarget(callable, SsaCallableLoweringTargetKind.Reject, null, code.Trim(), message.Trim());
    }
}

public sealed class SsaCallableLoweringTargetSet
{
    private readonly ReadOnlyCollection<SsaCallableLoweringTarget> _targets;
    private readonly IReadOnlyDictionary<CallableId, IReadOnlyList<SsaCallableLoweringTarget>> _byCallable;

    public SsaCallableLoweringTargetSet(IEnumerable<SsaCallableLoweringTarget>? targets = null)
    {
        var ordered = (targets ?? [])
            .OrderBy(static x => x.Callable)
            .ThenBy(static x => x.Kind)
            .ThenBy(static x => x.TargetId, StringComparer.Ordinal)
            .ToArray();

        var duplicates = ordered
            .GroupBy(static x => (x.Callable, x.Kind, x.TargetId))
            .FirstOrDefault(static x => x.Count() > 1);
        if (duplicates is not null)
            throw new ArgumentException($"Duplicate SSA callable lowering target for callable '{duplicates.Key.Callable}'.", nameof(targets));

        _targets = new ReadOnlyCollection<SsaCallableLoweringTarget>(ordered);
        _byCallable = ordered
            .GroupBy(static x => x.Callable)
            .ToDictionary(static x => x.Key, static x => (IReadOnlyList<SsaCallableLoweringTarget>)x.ToArray());
    }

    public static SsaCallableLoweringTargetSet Empty { get; } = new();

    public IReadOnlyList<SsaCallableLoweringTarget> Values => _targets;

    public IReadOnlyList<SsaCallableLoweringTarget> GetTargets(CallableId callable) =>
        _byCallable.TryGetValue(callable, out var targets) ? targets : [];
}

public sealed class SsaCallableLoweringPlan
{
    public SsaCallableLoweringPlan(
        CallableDescriptor callable,
        SsaCallableLoweringTarget target,
        AirIntrinsicDescriptor? intrinsic = null,
        MethodBase? managedMember = null)
    {
        Callable = callable ?? throw new ArgumentNullException(nameof(callable));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        Intrinsic = intrinsic;
        ManagedMember = managedMember;
    }

    public CallableDescriptor Callable { get; }

    public SsaCallableLoweringTarget Target { get; }

    public AirIntrinsicDescriptor? Intrinsic { get; }

    public MethodBase? ManagedMember { get; }
}

public sealed record SsaCallableLoweringFailure(string Code, string Message);

public sealed class SsaCallableLoweringPlanner
{
    private readonly SemanticDescriptorSet _semanticDescriptors;
    private readonly SsaCallableLoweringTargetSet _targets;
    private readonly AirIntrinsicDescriptorSet _airIntrinsics;
    private readonly SsaManagedCallableBindingSet? _managedBindings;

    public SsaCallableLoweringPlanner(
        SemanticDescriptorSet semanticDescriptors,
        SsaCallableLoweringTargetSet targets,
        AirIntrinsicDescriptorSet airIntrinsics,
        SsaManagedCallableBindingSet? managedBindings = null)
    {
        _semanticDescriptors = semanticDescriptors ?? throw new ArgumentNullException(nameof(semanticDescriptors));
        _targets = targets ?? throw new ArgumentNullException(nameof(targets));
        _airIntrinsics = airIntrinsics ?? throw new ArgumentNullException(nameof(airIntrinsics));
        _managedBindings = managedBindings;
    }

    public SsaCallableLoweringPlanner WithSemanticDescriptors(SemanticDescriptorSet semanticDescriptors) =>
        new(semanticDescriptors, _targets, _airIntrinsics, _managedBindings);

    public SsaCallableLoweringPlanner WithManagedBindings(SsaManagedCallableBindingSet managedBindings) =>
        new(_semanticDescriptors, _targets, _airIntrinsics, managedBindings ?? throw new ArgumentNullException(nameof(managedBindings)));

    public SsaCallableLoweringPlanner WithAdditionalCallables(IReadOnlyList<CallableDescriptor> additionalCallables)
    {
        ArgumentNullException.ThrowIfNull(additionalCallables);
        return additionalCallables.Count == 0
            ? this
            : new SsaCallableLoweringPlanner(
                MergeSemanticDescriptors(_semanticDescriptors, additionalCallables),
                _targets,
                _airIntrinsics,
                _managedBindings);
    }

    public bool TrySelect(SsaCall call, out SsaCallableLoweringPlan plan, out SsaCallableLoweringFailure failure)
    {
        ArgumentNullException.ThrowIfNull(call);

        plan = default!;
        failure = default!;

        if (!_semanticDescriptors.TryGetCallable(call.Callee, out var callable))
        {
            failure = new SsaCallableLoweringFailure(
                "ssa.to-air.call-descriptor.missing",
                $"SSA call '{call.Id}' targets callable '{call.Callee}', but no semantic descriptor is available.");
            return false;
        }

        var targets = _targets.GetTargets(call.Callee);
        if (SsaManagedCallables.IsManagedCallable(call.Callee))
            targets = targets.Concat([SsaCallableLoweringTarget.ManagedCall(call.Callee)]).ToArray();

        if (targets.Count == 0)
        {
            failure = new SsaCallableLoweringFailure(
                "ssa.to-air.call-lowering.missing",
                $"SSA call '{call.Id}' to '{call.Callee}' has no AIR lowering candidate.");
            return false;
        }

        var supported = new List<(int Priority, SsaCallableLoweringPlan Plan)>();
        var failures = new List<SsaCallableLoweringFailure>();
        foreach (var target in targets.OrderBy(static x => Priority(x.Kind)).ThenBy(static x => x.TargetId, StringComparer.Ordinal))
        {
            if (TryPlanTarget(call, callable, target, out var candidate, out var targetFailure))
                supported.Add((Priority(target.Kind), candidate));
            else
                failures.Add(targetFailure);
        }

        if (supported.Count == 0)
        {
            failure = failures.FirstOrDefault() ??
                new SsaCallableLoweringFailure(
                    "ssa.to-air.call-lowering.missing",
                    $"SSA call '{call.Id}' to '{call.Callee}' has no supported lowering target.");
            return false;
        }

        var bestPriority = supported.Min(static x => x.Priority);
        var best = supported.Where(x => x.Priority == bestPriority).Select(static x => x.Plan).ToArray();
        if (best.Length == 1)
        {
            plan = best[0];
            return true;
        }

        failure = new SsaCallableLoweringFailure(
            "ssa.to-air.call-lowering.ambiguous",
            $"SSA call '{call.Id}' to '{call.Callee}' has {best.Length} supported lowering targets with priority {bestPriority}.");
        return false;
    }

    private bool TryPlanTarget(
        SsaCall call,
        CallableDescriptor callable,
        SsaCallableLoweringTarget target,
        out SsaCallableLoweringPlan plan,
        out SsaCallableLoweringFailure failure)
    {
        plan = default!;
        failure = default!;

        switch (target.Kind)
        {
            case SsaCallableLoweringTargetKind.AirIntrinsic:
                return TryPlanAirIntrinsic(call, callable, target, out plan, out failure);
            case SsaCallableLoweringTargetKind.ManagedCall:
                return TryPlanManagedCall(call, callable, target, out plan, out failure);
            case SsaCallableLoweringTargetKind.CilOpcode:
                failure = new SsaCallableLoweringFailure(
                    "ssa.to-air.cil-target.unsupported",
                    $"SSA call '{call.Id}' targets CIL opcode '{target.TargetId}', but the current route emits AIR.");
                return false;
            case SsaCallableLoweringTargetKind.InterpreterPrimitive:
                failure = new SsaCallableLoweringFailure(
                    "ssa.to-air.interpreter-target.unsupported",
                    $"SSA call '{call.Id}' targets interpreter primitive '{target.TargetId}', but the current route emits AIR.");
                return false;
            case SsaCallableLoweringTargetKind.Reject:
                failure = new SsaCallableLoweringFailure(
                    target.DiagnosticCode ?? "ssa.to-air.call-lowering.rejected",
                    target.DiagnosticMessage ?? $"SSA call '{call.Id}' to '{call.Callee}' is rejected by its lowering target.");
                return false;
            default:
                failure = new SsaCallableLoweringFailure(
                    "ssa.to-air.call-lowering.target",
                    $"SSA call '{call.Id}' to '{call.Callee}' has unsupported lowering target kind '{target.Kind}'.");
                return false;
        }
    }

    private bool TryPlanAirIntrinsic(
        SsaCall call,
        CallableDescriptor callable,
        SsaCallableLoweringTarget target,
        out SsaCallableLoweringPlan plan,
        out SsaCallableLoweringFailure failure)
    {
        plan = default!;
        failure = default!;

        if (!_airIntrinsics.TryGet(target.TargetId!, out var intrinsic))
        {
            failure = new SsaCallableLoweringFailure(
                "ssa.to-air.intrinsic-capability.missing",
                $"SSA call '{call.Id}' lowers to AIR intrinsic '{target.TargetId}', but that intrinsic is not available in the AIR capability descriptor set.");
            return false;
        }

        if (!CallableAndIntrinsicShapesMatch(call, callable, intrinsic, out failure))
            return false;

        plan = new SsaCallableLoweringPlan(callable, target, intrinsic);
        return true;
    }

    private bool TryPlanManagedCall(
        SsaCall call,
        CallableDescriptor callable,
        SsaCallableLoweringTarget target,
        out SsaCallableLoweringPlan plan,
        out SsaCallableLoweringFailure failure)
    {
        plan = default!;
        failure = default!;

        if (_managedBindings is not null && _managedBindings.TryGet(call.Callee, out var binding))
        {
            if (!SignaturesMatch(callable.Signature, binding.Descriptor.Signature))
            {
                failure = new SsaCallableLoweringFailure(
                    "ssa.to-air.managed-call-descriptor.shape",
                    $"SSA managed call '{call.Id}' descriptor signature does not match its execution-scoped binding descriptor.");
                return false;
            }

            plan = new SsaCallableLoweringPlan(callable, target, managedMember: binding.Member);
            return true;
        }

        failure = new SsaCallableLoweringFailure(
            "ssa.to-air.managed-call.binding.missing",
            $"SSA managed call '{call.Id}' to '{call.Callee}' has no execution-scoped managed member binding.");
        return false;
    }

    private static bool SignaturesMatch(CallableSignature left, CallableSignature right) =>
        left.ParameterTypes.SequenceEqual(right.ParameterTypes) &&
        left.ResultTypes.SequenceEqual(right.ResultTypes);

    private static bool CallableAndIntrinsicShapesMatch(
        SsaCall call,
        CallableDescriptor callable,
        AirIntrinsicDescriptor intrinsic,
        out SsaCallableLoweringFailure failure)
    {
        failure = default!;

        if (callable.Signature.ParameterTypes.Count != intrinsic.ParameterTypes.Count)
        {
            failure = ShapeFailure(
                call,
                intrinsic,
                $"callable descriptor has {callable.Signature.ParameterTypes.Count} parameters, but AIR intrinsic has {intrinsic.ParameterTypes.Count} stack parameters.");
            return false;
        }

        if (callable.Signature.ResultTypes.Count != intrinsic.ResultTypes.Count)
        {
            failure = ShapeFailure(
                call,
                intrinsic,
                $"callable descriptor has {callable.Signature.ResultTypes.Count} results, but AIR intrinsic has {intrinsic.ResultTypes.Count} stack results.");
            return false;
        }

        for (var index = 0; index < callable.Signature.ParameterTypes.Count; index++)
        {
            if (!TryMapType(callable.Signature.ParameterTypes[index], out var airType) ||
                airType != intrinsic.ParameterTypes[index])
            {
                failure = ShapeFailure(
                    call,
                    intrinsic,
                    $"parameter {index} maps to AIR type '{airType}', but intrinsic expects '{intrinsic.ParameterTypes[index]}'.");
                return false;
            }
        }

        for (var index = 0; index < callable.Signature.ResultTypes.Count; index++)
        {
            if (!TryMapType(callable.Signature.ResultTypes[index], out var airType) ||
                airType != intrinsic.ResultTypes[index])
            {
                failure = ShapeFailure(
                    call,
                    intrinsic,
                    $"result {index} maps to AIR type '{airType}', but intrinsic produces '{intrinsic.ResultTypes[index]}'.");
                return false;
            }
        }

        return true;
    }

    private static SsaCallableLoweringFailure ShapeFailure(
        SsaCall call,
        AirIntrinsicDescriptor intrinsic,
        string reason) =>
        new(
            "ssa.to-air.call-lowering.shape",
            $"SSA call '{call.Id}' to '{call.Callee}' cannot lower to AIR intrinsic '{intrinsic.Id}': {reason}");

    private static bool TryMapType(SemanticTypeId semanticType, out AirValueTypeId airType)
    {
        if (semanticType == SsaPreviewSemanticTypes.Bool)
        {
            airType = AirValueTypes.Bool;
            return true;
        }

        if (semanticType == SsaPreviewSemanticTypes.Int32)
        {
            airType = AirValueTypes.Int32;
            return true;
        }

        if (semanticType == SsaPreviewSemanticTypes.Float64)
        {
            airType = AirValueTypes.Float64;
            return true;
        }

        airType = default;
        return false;
    }

    private static int Priority(SsaCallableLoweringTargetKind kind) =>
        kind switch
        {
            SsaCallableLoweringTargetKind.AirIntrinsic => 0,
            SsaCallableLoweringTargetKind.ManagedCall => 1,
            SsaCallableLoweringTargetKind.CilOpcode => 2,
            SsaCallableLoweringTargetKind.InterpreterPrimitive => 3,
            SsaCallableLoweringTargetKind.Reject => 4,
            _ => 100
        };

    private static SemanticDescriptorSet MergeSemanticDescriptors(
        SemanticDescriptorSet baseDescriptors,
        IReadOnlyList<CallableDescriptor> additionalCallables)
    {
        var types = baseDescriptors.Types
            .GroupBy(static x => x.Id)
            .Select(static x => x.First())
            .ToArray();
        var callables = baseDescriptors.Callables
            .Concat(additionalCallables)
            .GroupBy(static x => x.Id)
            .Select(static x => x.First())
            .ToArray();

        return new SemanticDescriptorSet(types, callables);
    }
}

public sealed class SsaCallAirIntrinsicLowering
{
    public SsaCallAirIntrinsicLowering(CallableId callable, string intrinsicId)
    {
        if (string.IsNullOrWhiteSpace(intrinsicId))
            throw new ArgumentException("AIR intrinsic identifier must not be empty.", nameof(intrinsicId));

        Callable = callable;
        IntrinsicId = intrinsicId.Trim();
    }

    public CallableId Callable { get; }

    public string IntrinsicId { get; }

    public SsaCallableLoweringTarget ToTarget() =>
        SsaCallableLoweringTarget.AirIntrinsic(Callable, IntrinsicId);
}

public sealed class SsaCallAirIntrinsicLoweringSet
{
    private readonly ReadOnlyCollection<SsaCallAirIntrinsicLowering> _lowerings;
    private readonly Dictionary<CallableId, SsaCallAirIntrinsicLowering> _byCallable;

    public SsaCallAirIntrinsicLoweringSet(IEnumerable<SsaCallAirIntrinsicLowering>? lowerings = null)
    {
        var ordered = (lowerings ?? [])
            .OrderBy(static x => x.Callable)
            .ToList();

        _byCallable = new Dictionary<CallableId, SsaCallAirIntrinsicLowering>();
        foreach (var lowering in ordered)
        {
            if (!_byCallable.TryAdd(lowering.Callable, lowering))
                throw new ArgumentException($"Duplicate SSA call lowering for callable '{lowering.Callable}'.", nameof(lowerings));
        }

        _lowerings = new ReadOnlyCollection<SsaCallAirIntrinsicLowering>(ordered);
    }

    public static SsaCallAirIntrinsicLoweringSet Empty { get; } = new();

    public IReadOnlyList<SsaCallAirIntrinsicLowering> Values => _lowerings;

    public bool TryGet(CallableId callable, out SsaCallAirIntrinsicLowering lowering) =>
        _byCallable.TryGetValue(callable, out lowering!);

    public SsaCallableLoweringTargetSet ToTargetSet() =>
        new(_lowerings.Select(static x => x.ToTarget()));
}

public sealed class SsaCallAirIntrinsicLoweringPlan
{
    public SsaCallAirIntrinsicLoweringPlan(
        CallableDescriptor callable,
        SsaCallAirIntrinsicLowering lowering,
        AirIntrinsicDescriptor intrinsic)
    {
        Callable = callable ?? throw new ArgumentNullException(nameof(callable));
        Lowering = lowering ?? throw new ArgumentNullException(nameof(lowering));
        Intrinsic = intrinsic ?? throw new ArgumentNullException(nameof(intrinsic));
    }

    public CallableDescriptor Callable { get; }

    public SsaCallAirIntrinsicLowering Lowering { get; }

    public AirIntrinsicDescriptor Intrinsic { get; }
}

public sealed record SsaCallAirIntrinsicLoweringFailure(string Code, string Message);

public sealed class SsaCallAirIntrinsicLoweringPlanner
{
    private readonly SsaCallableLoweringPlanner _planner;

    public SsaCallAirIntrinsicLoweringPlanner(
        SemanticDescriptorSet semanticDescriptors,
        SsaCallAirIntrinsicLoweringSet lowerings,
        AirIntrinsicDescriptorSet airIntrinsics)
    {
        _planner = new SsaCallableLoweringPlanner(
            semanticDescriptors,
            lowerings.ToTargetSet(),
            airIntrinsics);
    }

    public SsaCallableLoweringPlanner AsCallablePlanner() => _planner;

    public bool TrySelect(
        SsaCall call,
        out SsaCallAirIntrinsicLoweringPlan plan,
        out SsaCallAirIntrinsicLoweringFailure failure)
    {
        if (!_planner.TrySelect(call, out var targetPlan, out var targetFailure) ||
            targetPlan.Target.Kind != SsaCallableLoweringTargetKind.AirIntrinsic ||
            targetPlan.Intrinsic is null)
        {
            plan = default!;
            failure = new SsaCallAirIntrinsicLoweringFailure(targetFailure.Code, targetFailure.Message);
            return false;
        }

        plan = new SsaCallAirIntrinsicLoweringPlan(
            targetPlan.Callable,
            new SsaCallAirIntrinsicLowering(targetPlan.Target.Callable, targetPlan.Target.TargetId!),
            targetPlan.Intrinsic);
        failure = default!;
        return true;
    }
}

public static class SsaPreviewAirIntrinsicLowerings
{
    public static SsaCallAirIntrinsicLoweringSet ArithmeticInt32 { get; } = new(
    [
        new SsaCallAirIntrinsicLowering(SsaPreviewCallables.AddInt32Unchecked, AirIntrinsicIds.AddInt32Unchecked),
        new SsaCallAirIntrinsicLowering(SsaPreviewCallables.SubtractInt32Unchecked, AirIntrinsicIds.SubtractInt32Unchecked),
        new SsaCallAirIntrinsicLowering(SsaPreviewCallables.MultiplyInt32Unchecked, AirIntrinsicIds.MultiplyInt32Unchecked),
        new SsaCallAirIntrinsicLowering(SsaPreviewCallables.EqualInt32, AirIntrinsicIds.EqualInt32)
    ]);
}
