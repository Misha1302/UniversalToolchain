namespace UniversalToolchain.Semantics.Abstractions;

public sealed record ConstantValue(SemanticTypeId Type, string CanonicalValue);

public interface IConstantEvaluator
{
    bool TryEvaluate(
        CallableDescriptor descriptor,
        IReadOnlyList<ConstantValue> arguments,
        out ConstantValue result);
}
