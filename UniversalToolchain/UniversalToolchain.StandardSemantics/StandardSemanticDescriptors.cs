using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.StandardSemantics;

public static class StandardSemanticDescriptors
{
    public static SemanticDescriptorSet ScalarInt32 { get; } = new(
        types:
        [
            new SemanticTypeDescriptor(
                StandardSemanticTypes.Bool,
                SemanticTypeTraits.Predicate | SemanticTypeTraits.ValueObject | SemanticTypeTraits.Immutable,
                "bool"),
            new SemanticTypeDescriptor(
                StandardSemanticTypes.Int32,
                SemanticTypeTraits.Numeric | SemanticTypeTraits.ValueObject | SemanticTypeTraits.Immutable,
                "int32"),
            new SemanticTypeDescriptor(
                StandardSemanticTypes.Float64,
                SemanticTypeTraits.Numeric | SemanticTypeTraits.ValueObject | SemanticTypeTraits.Immutable,
                "float64")
        ],
        callables:
        [
            new CallableDescriptor(
                StandardCallables.AddInt32Unchecked,
                new CallableSignature(
                    [StandardSemanticTypes.Int32, StandardSemanticTypes.Int32],
                    [StandardSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                algebraicTraits: AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "unchecked int32 addition"),
            new CallableDescriptor(
                StandardCallables.SubtractInt32Unchecked,
                new CallableSignature(
                    [StandardSemanticTypes.Int32, StandardSemanticTypes.Int32],
                    [StandardSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "unchecked int32 subtraction"),
            new CallableDescriptor(
                StandardCallables.MultiplyInt32Unchecked,
                new CallableSignature(
                    [StandardSemanticTypes.Int32, StandardSemanticTypes.Int32],
                    [StandardSemanticTypes.Int32]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                algebraicTraits: AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "unchecked int32 multiplication"),
            new CallableDescriptor(
                StandardCallables.EqualInt32,
                new CallableSignature(
                    [StandardSemanticTypes.Int32, StandardSemanticTypes.Int32],
                    [StandardSemanticTypes.Bool]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                algebraicTraits: AlgebraicTraits.Commutative | AlgebraicTraits.Comparison,
                trustLevel: SemanticTrustLevel.BuiltInTrusted,
                displayName: "int32 equality")
        ]);
}
