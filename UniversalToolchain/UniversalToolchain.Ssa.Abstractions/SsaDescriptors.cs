using System.Collections.ObjectModel;
using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

public enum SsaEffectKind
{
    Pure,
    ReadsMemory,
    WritesMemory,
    CallsExternalCode,
    Terminates
}

public sealed class SsaEffectSummary
{
    private readonly ReadOnlyCollection<SsaEffectKind> _effects;

    public SsaEffectSummary(IEnumerable<SsaEffectKind>? effects = null)
    {
        _effects = new ReadOnlyCollection<SsaEffectKind>((effects ?? [])
            .Distinct()
            .Order()
            .ToList());
    }

    public static SsaEffectSummary Pure { get; } = new();

    public IReadOnlyList<SsaEffectKind> Effects => _effects;

    public bool IsPure => _effects.Count == 0 || _effects.All(static x => x == SsaEffectKind.Pure);
}

public sealed class SsaOpDescriptor
{
    public SsaOpDescriptor(
        SsaOpId id,
        IEnumerable<SsaTypeId>? operandTypes = null,
        IEnumerable<SsaTypeId>? resultTypes = null,
        SsaEffectSummary? effects = null,
        IEnumerable<SsaAttributeKey>? requiredAttributes = null,
        IEnumerable<SsaAttributeKey>? allowedAttributes = null)
    {
        Id = id;
        OperandTypes = new ReadOnlyCollection<SsaTypeId>((operandTypes ?? []).ToList());
        ResultTypes = new ReadOnlyCollection<SsaTypeId>((resultTypes ?? []).ToList());
        Effects = effects ?? SsaEffectSummary.Pure;

        var required = (requiredAttributes ?? []).Distinct().Order().ToList();
        var allowed = (allowedAttributes ?? []).Distinct().Order().ToList();
        foreach (var requiredAttribute in required)
        {
            if (!allowed.Contains(requiredAttribute))
                allowed.Add(requiredAttribute);
        }

        allowed.Sort();
        RequiredAttributes = new ReadOnlyCollection<SsaAttributeKey>(required);
        AllowedAttributes = new ReadOnlyCollection<SsaAttributeKey>(allowed);
    }

    public SsaOpId Id { get; }

    public IReadOnlyList<SsaTypeId> OperandTypes { get; }

    public IReadOnlyList<SsaTypeId> ResultTypes { get; }

    public SsaEffectSummary Effects { get; }

    public IReadOnlyList<SsaAttributeKey> RequiredAttributes { get; }

    public IReadOnlyList<SsaAttributeKey> AllowedAttributes { get; }
}

public sealed class SsaDescriptorSet
{
    private readonly ReadOnlyCollection<SsaOpDescriptor> _descriptors;
    private readonly Dictionary<SsaOpId, SsaOpDescriptor> _byId;

    public SsaDescriptorSet(IEnumerable<SsaOpDescriptor>? descriptors = null)
    {
        var ordered = (descriptors ?? [])
            .OrderBy(static x => x.Id)
            .ToList();

        _byId = new Dictionary<SsaOpId, SsaOpDescriptor>();
        foreach (var descriptor in ordered)
        {
            if (!_byId.TryAdd(descriptor.Id, descriptor))
                throw new ArgumentException($"Duplicate SSA operation descriptor '{descriptor.Id}'.", nameof(descriptors));
        }

        _descriptors = new ReadOnlyCollection<SsaOpDescriptor>(ordered);
    }

    public static SsaDescriptorSet Empty { get; } = new();

    public IReadOnlyList<SsaOpDescriptor> Values => _descriptors;

    public bool TryGet(SsaOpId id, out SsaOpDescriptor descriptor) => _byId.TryGetValue(id, out descriptor!);
}

public static class SsaCoreDescriptors
{
    public static SsaDescriptorSet ConstantMaterialization { get; } = new(
    [
        new SsaOpDescriptor(
            SsaOperations.ConstantInt32,
            resultTypes: [SsaTypes.Int32],
            requiredAttributes: [SsaAttributeKeys.ConstantValue],
            allowedAttributes: [SsaAttributeKeys.ConstantValue]),
        new SsaOpDescriptor(
            SsaOperations.ConstantBool,
            resultTypes: [SsaTypes.Bool],
            requiredAttributes: [SsaAttributeKeys.ConstantValue],
            allowedAttributes: [SsaAttributeKeys.ConstantValue]),
        new SsaOpDescriptor(
            SsaOperations.ConstantFloat64,
            resultTypes: [SsaTypes.Float64],
            requiredAttributes: [SsaAttributeKeys.ConstantValue],
            allowedAttributes: [SsaAttributeKeys.ConstantValue])
    ]);


    public static SsaDescriptorSet CoreOperations { get; } = new(
    [
        .. ConstantMaterialization.Values,
        new SsaOpDescriptor(
            SsaOperations.LoadExternalInt32,
            resultTypes: [SsaTypes.Int32],
            effects: new SsaEffectSummary([SsaEffectKind.ReadsMemory]),
            requiredAttributes: [SsaAttributeKeys.ExternalSlot],
            allowedAttributes: [SsaAttributeKeys.ExternalSlot]),
        new SsaOpDescriptor(
            SsaOperations.LoadExternalBool,
            resultTypes: [SsaTypes.Bool],
            effects: new SsaEffectSummary([SsaEffectKind.ReadsMemory]),
            requiredAttributes: [SsaAttributeKeys.ExternalSlot],
            allowedAttributes: [SsaAttributeKeys.ExternalSlot]),
        new SsaOpDescriptor(
            SsaOperations.LoadExternalFloat64,
            resultTypes: [SsaTypes.Float64],
            effects: new SsaEffectSummary([SsaEffectKind.ReadsMemory]),
            requiredAttributes: [SsaAttributeKeys.ExternalSlot],
            allowedAttributes: [SsaAttributeKeys.ExternalSlot])
    ]);
}

public static class SsaSemanticDescriptors
{
    public static SemanticDescriptorSet ArithmeticInt32 { get; } = new(
        types:
        [
            new SemanticTypeDescriptor(
                SsaSemanticTypes.Bool,
                SemanticTypeTraits.Predicate | SemanticTypeTraits.ValueObject | SemanticTypeTraits.Immutable,
                "alpha bool"),
            new SemanticTypeDescriptor(
                SsaSemanticTypes.Int32,
                SemanticTypeTraits.Numeric | SemanticTypeTraits.ValueObject | SemanticTypeTraits.Immutable,
                "alpha int32"),
            new SemanticTypeDescriptor(
                SsaSemanticTypes.Float64,
                SemanticTypeTraits.Numeric | SemanticTypeTraits.ValueObject | SemanticTypeTraits.Immutable,
                "alpha float64"),
            new SemanticTypeDescriptor(
                SsaSemanticTypes.Object,
                SemanticTypeTraits.Reference | SemanticTypeTraits.RuntimeManaged,
                "runtime managed object")
        ],
        callables:
        [
            new CallableDescriptor(
                SsaCallables.AddInt32Unchecked,
                new CallableSignature(
                    [SsaSemanticTypes.Int32, SsaSemanticTypes.Int32],
                    [SsaSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                algebraicTraits: AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "alpha unchecked int32 addition"),
            new CallableDescriptor(
                SsaCallables.SubtractInt32Unchecked,
                new CallableSignature(
                    [SsaSemanticTypes.Int32, SsaSemanticTypes.Int32],
                    [SsaSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "alpha unchecked int32 subtraction"),
            new CallableDescriptor(
                SsaCallables.MultiplyInt32Unchecked,
                new CallableSignature(
                    [SsaSemanticTypes.Int32, SsaSemanticTypes.Int32],
                    [SsaSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                algebraicTraits: AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "alpha unchecked int32 multiplication"),
            new CallableDescriptor(
                SsaCallables.EqualInt32,
                new CallableSignature(
                    [SsaSemanticTypes.Int32, SsaSemanticTypes.Int32],
                    [SsaSemanticTypes.Bool]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                algebraicTraits: AlgebraicTraits.Commutative | AlgebraicTraits.Comparison,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "alpha int32 equality")
        ]);
}
