using System.Collections.ObjectModel;
using System.Reflection;
using UniversalToolchain.Semantics.Abstractions;

namespace UniversalToolchain.Ssa.Abstractions;

/// <summary>
/// Preserves the exact managed member selected during AIR lowering for one SSA execution.
/// </summary>
public sealed class SsaManagedCallableBinding
{
    public SsaManagedCallableBinding(
        CallableId callable,
        CallableDescriptor descriptor,
        MethodBase member)
    {
        Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Member = member ?? throw new ArgumentNullException(nameof(member));
        if (Descriptor.Id != callable)
            throw new ArgumentException("Managed callable binding descriptor id must match the callable id.", nameof(descriptor));

        Callable = callable;
    }

    public CallableId Callable { get; }

    public CallableDescriptor Descriptor { get; }

    public MethodBase Member { get; }

    /// <summary>
    /// Compares two execution bindings by member identity and by the complete
    /// semantic callable contract. Descriptor instances are deliberately not
    /// compared by reference because repeated lowering of the same managed
    /// member may materialize equivalent immutable descriptor objects.
    /// </summary>
    public bool IsEquivalentTo(SsaManagedCallableBinding? other) =>
        other is not null &&
        Callable == other.Callable &&
        Equals(Member, other.Member) &&
        DescriptorsAreEquivalent(Descriptor, other.Descriptor);

    public static bool DescriptorsAreEquivalent(
        CallableDescriptor left,
        CallableDescriptor right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        return left.Id == right.Id &&
               left.Signature.ParameterTypes.SequenceEqual(right.Signature.ParameterTypes) &&
               left.Signature.ResultTypes.SequenceEqual(right.Signature.ResultTypes) &&
               left.Effects.Effects.SequenceEqual(right.Effects.Effects) &&
               left.Determinism == right.Determinism &&
               left.AlgebraicTraits == right.AlgebraicTraits &&
               left.TrustLevel == right.TrustLevel &&
               string.Equals(left.DisplayName, right.DisplayName, StringComparison.Ordinal) &&
               left.RequiredAttributes.SequenceEqual(right.RequiredAttributes) &&
               left.AllowedAttributes.SequenceEqual(right.AllowedAttributes);
    }
}

/// <summary>
/// Immutable execution-scoped managed callable binding snapshot.
/// </summary>
public sealed class SsaManagedCallableBindingSet
{
    private readonly ReadOnlyCollection<SsaManagedCallableBinding> _bindings;
    private readonly Dictionary<CallableId, SsaManagedCallableBinding> _byCallable;

    public SsaManagedCallableBindingSet(IEnumerable<SsaManagedCallableBinding>? bindings = null)
    {
        _byCallable = new Dictionary<CallableId, SsaManagedCallableBinding>();
        foreach (var binding in bindings ?? [])
        {
            ArgumentNullException.ThrowIfNull(binding);
            if (_byCallable.TryGetValue(binding.Callable, out var existing))
            {
                if (!existing.IsEquivalentTo(binding))
                {
                    throw new ArgumentException(
                        $"Managed callable '{binding.Callable}' is bound to multiple incompatible members or descriptors.",
                        nameof(bindings));
                }

                continue;
            }

            _byCallable.Add(binding.Callable, binding);
        }

        _bindings = new ReadOnlyCollection<SsaManagedCallableBinding>(
            _byCallable.Values.OrderBy(static binding => binding.Callable.Value, StringComparer.Ordinal).ToArray());
    }

    public static SsaManagedCallableBindingSet Empty { get; } = new();

    public IReadOnlyList<SsaManagedCallableBinding> Values => _bindings;

    public bool TryGet(CallableId callable, out SsaManagedCallableBinding binding) =>
        _byCallable.TryGetValue(callable, out binding!);
}
