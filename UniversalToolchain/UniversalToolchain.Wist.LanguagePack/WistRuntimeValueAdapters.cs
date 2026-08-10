using System.Globalization;
using NumbersModule.Core;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;

namespace UniversalToolchain.Wist.LanguagePack;

internal static class WistLanguageSlots
{
    public static LanguageSlotId RuntimeValueAdapters { get; } = new("wist.runtime.value-adapters");
}

internal sealed class WistRuntimeValueAdapterRegistration
{
    private readonly Func<object, object?> _adaptOutput;
    private readonly Func<Type, bool> _acceptsPublicType;
    private readonly Func<object, object?> _adaptInput;

    public WistRuntimeValueAdapterRegistration(
        LanguageContributionId contributionId,
        Type runtimeValueType,
        Func<object, object?> adaptOutput)
        : this(
            contributionId,
            runtimeValueType,
            adaptOutput,
            static _ => false,
            static value => value)
    {
    }

    public WistRuntimeValueAdapterRegistration(
        LanguageContributionId contributionId,
        Type runtimeValueType,
        Func<object, object?> adaptOutput,
        Func<Type, bool> acceptsPublicType,
        Func<object, object?> adaptInput)
    {
        ContributionId = contributionId;
        RuntimeValueType = runtimeValueType ?? throw new ArgumentNullException(nameof(runtimeValueType));
        _adaptOutput = adaptOutput ?? throw new ArgumentNullException(nameof(adaptOutput));
        _acceptsPublicType = acceptsPublicType ?? throw new ArgumentNullException(nameof(acceptsPublicType));
        _adaptInput = adaptInput ?? throw new ArgumentNullException(nameof(adaptInput));
    }

    public LanguageContributionId ContributionId { get; }
    public Type RuntimeValueType { get; }

    public bool AcceptsPublicType(Type publicType)
    {
        ArgumentNullException.ThrowIfNull(publicType);
        return _acceptsPublicType(publicType);
    }

    public object? AdaptOutput(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value.GetType() != RuntimeValueType)
        {
            throw new InvalidOperationException(
                $"Wist value adapter '{ContributionId.Value}' expects exact runtime type '{RuntimeValueType.FullName}', " +
                $"but received '{value.GetType().FullName}'.");
        }
        return _adaptOutput(value);
    }

    public object? AdaptInput(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!AcceptsPublicType(value.GetType()))
        {
            throw new InvalidOperationException(
                $"Wist value adapter '{ContributionId.Value}' does not accept public input type '{value.GetType().FullName}'.");
        }
        return _adaptInput(value);
    }
}

internal static class WistRuntimeValueAdapterCatalog
{
    public static IReadOnlyList<WistRuntimeValueAdapterRegistration> BuiltIn { get; } =
    [
        new(
            WistContributionIds.RealNumberValueAdapter,
            typeof(RealNumberImpl),
            static value => ((RealNumberImpl)value).GetValue(),
            static type => IsClrNumericType(type),
            static value => RealNumberImpl.Create(Convert.ToDouble(value, CultureInfo.InvariantCulture)))
    ];

    private static bool IsClrNumericType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return Type.GetTypeCode(type) is
            TypeCode.SByte or TypeCode.Byte or
            TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or
            TypeCode.Int64 or TypeCode.UInt64 or
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal;
    }
}

internal static class WistRuntimeValueAdapterActivation
{
    public static object? Normalize(LanguagePlan plan, object? value) =>
        Normalize(plan, WistRuntimeValueAdapterCatalog.BuiltIn, value);

    public static object? NormalizeInput(LanguagePlan plan, object? value) =>
        NormalizeInput(plan, WistRuntimeValueAdapterCatalog.BuiltIn, value);

    public static Type NormalizeDeclaredType(LanguagePlan plan, Type publicType) =>
        NormalizeDeclaredType(plan, WistRuntimeValueAdapterCatalog.BuiltIn, publicType);

    internal static object? Normalize(
        LanguagePlan plan,
        IEnumerable<WistRuntimeValueAdapterRegistration> registrations,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(registrations);
        if (value == null)
            return null;

        var selected = Select(plan, registrations);
        var exactMatches = selected.Where(registration => registration.RuntimeValueType == value.GetType()).ToArray();
        if (exactMatches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Wist runtime value type '{value.GetType().FullName}' has multiple selected exact value adapters.");
        }
        if (exactMatches.Length == 1)
            return exactMatches[0].AdaptOutput(value);

        if (value is RealNumberImpl)
        {
            throw new InvalidOperationException(
                $"Runtime value '{typeof(RealNumberImpl).FullName}' reached the Wist public boundary without the planned '{WistContributionIds.RealNumberValueAdapter.Value}' adapter.");
        }

        return value;
    }

    internal static object? NormalizeInput(
        LanguagePlan plan,
        IEnumerable<WistRuntimeValueAdapterRegistration> registrations,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(registrations);
        if (value == null)
            return null;

        var selected = Select(plan, registrations);
        var matches = selected.Where(registration => registration.AcceptsPublicType(value.GetType())).ToArray();
        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Wist public input type '{value.GetType().FullName}' has multiple selected exact value adapters.");
        }
        return matches.Length == 1 ? matches[0].AdaptInput(value) : value;
    }

    internal static Type NormalizeDeclaredType(
        LanguagePlan plan,
        IEnumerable<WistRuntimeValueAdapterRegistration> registrations,
        Type publicType)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(publicType);

        var selected = Select(plan, registrations);
        var matches = selected.Where(registration => registration.AcceptsPublicType(publicType)).ToArray();
        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"Wist public declared type '{publicType.FullName}' has multiple selected exact value adapters.");
        }
        return matches.Length == 1 ? matches[0].RuntimeValueType : publicType;
    }

    private static IReadOnlyList<WistRuntimeValueAdapterRegistration> Select(
        LanguagePlan plan,
        IEnumerable<WistRuntimeValueAdapterRegistration> registrations)
    {
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
        return selected;
    }
}
