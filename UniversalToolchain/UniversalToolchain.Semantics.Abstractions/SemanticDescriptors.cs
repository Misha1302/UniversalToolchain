using System.Collections.ObjectModel;

namespace UniversalToolchain.Semantics.Abstractions;

public sealed class SemanticTypeDescriptor
{
    public SemanticTypeDescriptor(
        SemanticTypeId id,
        SemanticTypeTraits traits = SemanticTypeTraits.None,
        string? displayName = null)
    {
        Id = id;
        Traits = traits;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? id.Value : displayName.Trim();
    }

    public SemanticTypeId Id { get; }

    public SemanticTypeTraits Traits { get; }

    public string DisplayName { get; }

    public bool HasTrait(SemanticTypeTraits trait) => (Traits & trait) == trait;
}

public sealed class CallableSignature
{
    public CallableSignature(
        IEnumerable<SemanticTypeId>? parameterTypes = null,
        IEnumerable<SemanticTypeId>? resultTypes = null)
    {
        ParameterTypes = new ReadOnlyCollection<SemanticTypeId>((parameterTypes ?? []).ToList());
        ResultTypes = new ReadOnlyCollection<SemanticTypeId>((resultTypes ?? []).ToList());
    }

    public IReadOnlyList<SemanticTypeId> ParameterTypes { get; }

    public IReadOnlyList<SemanticTypeId> ResultTypes { get; }
}

public sealed class CallableDescriptor
{
    public CallableDescriptor(
        CallableId id,
        CallableSignature signature,
        SemanticEffectSummary? effects = null,
        Determinism determinism = Determinism.Unknown,
        AlgebraicTraits algebraicTraits = AlgebraicTraits.None,
        SemanticTrustLevel trustLevel = SemanticTrustLevel.ExternalUnknown,
        string? displayName = null,
        IEnumerable<SemanticAttributeKey>? requiredAttributes = null,
        IEnumerable<SemanticAttributeKey>? allowedAttributes = null)
    {
        Id = id;
        Signature = signature ?? throw new ArgumentNullException(nameof(signature));
        Effects = effects ?? SemanticEffectSummary.Pure;
        Determinism = determinism;
        AlgebraicTraits = algebraicTraits;
        TrustLevel = trustLevel;
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? id.Value : displayName.Trim();

        var required = (requiredAttributes ?? []).Distinct().Order().ToList();
        var allowed = (allowedAttributes ?? []).Distinct().Order().ToList();
        foreach (var requiredAttribute in required)
        {
            if (!allowed.Contains(requiredAttribute))
                allowed.Add(requiredAttribute);
        }

        allowed.Sort();
        RequiredAttributes = new ReadOnlyCollection<SemanticAttributeKey>(required);
        AllowedAttributes = new ReadOnlyCollection<SemanticAttributeKey>(allowed);
    }

    public CallableId Id { get; }

    public CallableSignature Signature { get; }

    public SemanticEffectSummary Effects { get; }

    public Determinism Determinism { get; }

    public AlgebraicTraits AlgebraicTraits { get; }

    public SemanticTrustLevel TrustLevel { get; }

    public string DisplayName { get; }

    public IReadOnlyList<SemanticAttributeKey> RequiredAttributes { get; }

    public IReadOnlyList<SemanticAttributeKey> AllowedAttributes { get; }

    public bool HasTrait(AlgebraicTraits trait) => (AlgebraicTraits & trait) == trait;
}

public sealed class SemanticDescriptorSet
{
    private readonly ReadOnlyCollection<SemanticTypeDescriptor> _types;
    private readonly ReadOnlyCollection<CallableDescriptor> _callables;
    private readonly Dictionary<SemanticTypeId, SemanticTypeDescriptor> _typesById;
    private readonly Dictionary<CallableId, CallableDescriptor> _callablesById;

    public SemanticDescriptorSet(
        IEnumerable<SemanticTypeDescriptor>? types = null,
        IEnumerable<CallableDescriptor>? callables = null)
    {
        var orderedTypes = (types ?? [])
            .OrderBy(static x => x.Id)
            .ToList();
        var orderedCallables = (callables ?? [])
            .OrderBy(static x => x.Id)
            .ToList();

        _typesById = new Dictionary<SemanticTypeId, SemanticTypeDescriptor>();
        foreach (var type in orderedTypes)
        {
            if (!_typesById.TryAdd(type.Id, type))
                throw new ArgumentException($"Duplicate semantic type descriptor '{type.Id}'.", nameof(types));
        }

        _callablesById = new Dictionary<CallableId, CallableDescriptor>();
        foreach (var callable in orderedCallables)
        {
            ValidateCallable(callable);
            if (!_callablesById.TryAdd(callable.Id, callable))
                throw new ArgumentException($"Duplicate callable descriptor '{callable.Id}'.", nameof(callables));
        }

        _types = new ReadOnlyCollection<SemanticTypeDescriptor>(orderedTypes);
        _callables = new ReadOnlyCollection<CallableDescriptor>(orderedCallables);
    }

    public static SemanticDescriptorSet Empty { get; } = new();

    public IReadOnlyList<SemanticTypeDescriptor> Types => _types;

    public IReadOnlyList<CallableDescriptor> Callables => _callables;

    public bool TryGetType(SemanticTypeId id, out SemanticTypeDescriptor descriptor) =>
        _typesById.TryGetValue(id, out descriptor!);

    public bool TryGetCallable(CallableId id, out CallableDescriptor descriptor) =>
        _callablesById.TryGetValue(id, out descriptor!);

    private static void ValidateCallable(CallableDescriptor callable)
    {
        if (callable.Effects.IsPure && callable.Effects.Effects.Count > 1)
        {
            throw new ArgumentException(
                $"Callable descriptor '{callable.Id}' mixes Pure with other effects.",
                nameof(callable));
        }

        if (callable.TrustLevel is SemanticTrustLevel.UserProvidedUnchecked or SemanticTrustLevel.ExternalUnknown &&
            callable.AlgebraicTraits != AlgebraicTraits.None)
        {
            throw new ArgumentException(
                $"Callable descriptor '{callable.Id}' cannot expose algebraic traits at trust level '{callable.TrustLevel}'.",
                nameof(callable));
        }
    }
}
