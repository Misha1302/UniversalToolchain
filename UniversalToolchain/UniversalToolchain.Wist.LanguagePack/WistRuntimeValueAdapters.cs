using NumbersModule.Core;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistLanguageSlots
{
    public static LanguageSlotId RuntimeValueAdapters { get; } = new("wist.runtime.value-adapters");
}

internal sealed class WistRuntimeValueAdapterRegistration(
    LanguageContributionId contributionId,
    Type runtimeValueType,
    Func<object, object?> adapt)
{
    private readonly Func<object, object?> _adapt = adapt ?? throw new ArgumentNullException(nameof(adapt));

    public LanguageContributionId ContributionId { get; } = contributionId;
    public Type RuntimeValueType { get; } = runtimeValueType ?? throw new ArgumentNullException(nameof(runtimeValueType));

    public object? Adapt(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.GetType() != RuntimeValueType)
        {
            throw new InvalidOperationException(
                $"Wist value adapter '{ContributionId.Value}' expects exact runtime type '{RuntimeValueType.FullName}', " +
                $"but received '{value.GetType().FullName}'.");
        }
        return _adapt(value);
    }
}

internal static class WistRuntimeValueAdapterCatalog
{
    public static IReadOnlyList<WistRuntimeValueAdapterRegistration> BuiltIn { get; } =
    [
        new(
            WistContributionIds.RealNumberValueAdapter,
            typeof(RealNumberImpl),
            static value => ((RealNumberImpl)value).GetValue())
    ];
}

internal static class WistRuntimeValueAdapterActivation
{
    public static object? Normalize(LanguagePlan plan, object? value) =>
        Normalize(plan, WistRuntimeValueAdapterCatalog.BuiltIn, value);

    internal static object? Normalize(
        LanguagePlan plan,
        IEnumerable<WistRuntimeValueAdapterRegistration> registrations,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(registrations);
        if (value == null)
            return null;

        var registrationSnapshot = registrations.ToArray();
        var selectedIds = plan.Contributions
            .Where(static contribution => contribution.Contribution.Slot == WistLanguageSlots.RuntimeValueAdapters)
            .Select(static contribution => contribution.Contribution.Id)
            .ToArray();
        var selected = new List<WistRuntimeValueAdapterRegistration>(selectedIds.Length);
        foreach (var selectedId in selectedIds)
        {
            var matches = registrationSnapshot
                .Where(registration => registration.ContributionId == selectedId)
                .ToArray();
            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Wist LanguagePlan selects value adapter '{selectedId.Value}', but exactly one exact registration is required; found {matches.Length}.");
            }
            selected.Add(matches[0]);
        }

        var exactMatches = selected.Where(registration => registration.RuntimeValueType == value.GetType()).ToArray();
        if (exactMatches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Wist runtime value type '{value.GetType().FullName}' has multiple selected exact value adapters.");
        }
        if (exactMatches.Length == 1)
            return exactMatches[0].Adapt(value);

        if (value is RealNumberImpl)
        {
            throw new InvalidOperationException(
                $"Runtime value '{typeof(RealNumberImpl).FullName}' reached the Wist public boundary without the planned '{WistContributionIds.RealNumberValueAdapter.Value}' adapter.");
        }

        return value;
    }
}
