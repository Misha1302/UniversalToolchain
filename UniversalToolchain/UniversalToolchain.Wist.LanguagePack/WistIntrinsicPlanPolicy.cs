using BasicCore.Capabilities;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Core;
using IntermediateRepresentationAbstractions;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal sealed class WistIntrinsicPlanPolicy
{
    private readonly HashSet<string> _allowed;
    private readonly HashSet<string> _forbidden;

    private WistIntrinsicPlanPolicy(
        IEnumerable<string> allowed,
        IEnumerable<string> forbidden,
        bool hasExplicitAllowList)
    {
        _allowed = allowed.ToHashSet(StringComparer.Ordinal);
        _forbidden = forbidden.ToHashSet(StringComparer.Ordinal);
        HasExplicitAllowList = hasExplicitAllowList;
    }

    public bool HasExplicitAllowList { get; }

    public static WistIntrinsicPlanPolicy Create(LanguagePlan plan, BackendId backend)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var matching = plan.Definition.IntrinsicPolicy
            .Where(directive => directive.Backend == null || directive.Backend == backend)
            .ToArray();
        return new WistIntrinsicPlanPolicy(
            matching.Where(static directive => directive.Allowed).Select(static directive => directive.Intrinsic.Value),
            matching.Where(static directive => !directive.Allowed).Select(static directive => directive.Intrinsic.Value),
            matching.Any(static directive => directive.Allowed));
    }

    public void Validate(IAbstractIR air)
    {
        ArgumentNullException.ThrowIfNull(air);
        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic)
                continue;
            if (!instruction.TryGetTypedIntrinsicInvocation(out var invocation))
            {
                throw new InvalidOperationException(
                    $"Intrinsic payload must contain a typed IntrinsicInvocation: {instruction}");
            }

            var name = IntrinsicCapabilityNameEncoder.EncodeOrThrow(invocation);
            EnsureAllowed(name);
        }
    }

    public IOptimizerIntrinsicCapabilityContext ApplyTo(IOptimizerIntrinsicCapabilityContext inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new PolicyCapabilityContext(this, inner);
    }

    private void EnsureAllowed(string intrinsicName)
    {
        if (_forbidden.Contains(intrinsicName))
            throw new InvalidOperationException($"Intrinsic '{intrinsicName}' is forbidden by the selected language plan.");
        if (HasExplicitAllowList && !_allowed.Contains(intrinsicName))
            throw new InvalidOperationException($"Intrinsic '{intrinsicName}' is not allowed by the selected language plan.");
    }

    private bool Allows(string intrinsicName) =>
        !_forbidden.Contains(intrinsicName) &&
        (!HasExplicitAllowList || _allowed.Contains(intrinsicName));

    private sealed class PolicyCapabilityContext(
        WistIntrinsicPlanPolicy policy,
        IOptimizerIntrinsicCapabilityContext inner) : IOptimizerIntrinsicCapabilityContext
    {
        public bool Supports(IntrinsicSymbol symbol, params Type[] typeArguments) =>
            Supports(symbol, (IReadOnlyList<Type>)typeArguments);

        public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<Type> typeArguments)
        {
            ArgumentNullException.ThrowIfNull(typeArguments);
            var invocation = new IntrinsicInvocation(
                symbol,
                typeArguments.Select(IntrinsicTypeArgument.From).ToArray(),
                []);
            if (!IntrinsicCapabilityNameEncoder.TryEncode(invocation, out var name) || !policy.Allows(name))
                return false;
            return inner.Supports(symbol, typeArguments);
        }
    }
}
