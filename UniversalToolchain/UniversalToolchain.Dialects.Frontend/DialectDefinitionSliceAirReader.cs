using IntermediateRepresentationAbstractions;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDefinitionSliceAirReader
{
    public static DialectDefinitionSlice Read(IAbstractIR air)
    {
        if (air == null)
        {
            Thrower.ArgumentNull(nameof(air));
        }

        var aggregation = new DialectDefinitionAggregation();
        foreach (var annotation in air.Instructions.SelectMany(x => x.Metadata))
        {
            ReadAnnotation(aggregation, annotation);
        }

        return new DialectDefinitionSliceBuilder().Build(aggregation);
    }

    private static void ReadAnnotation(DialectDefinitionAggregation aggregation, object annotation)
    {
        switch (annotation)
        {
            case DialectNameAirAnnotation dialectName:
                aggregation.SetDialectName(dialectName.Name);
                break;
            case UseModulesAirAnnotation useModules:
                aggregation.AddUseModules(useModules.Modules);
                break;
            case ExcludeModulesAirAnnotation excludeModules:
                aggregation.AddExcludeModules(excludeModules.Modules);
                break;
            case RequiresModulesAirAnnotation requiresModules:
                aggregation.AddRequiresModules(requiresModules.Modules);
                break;
            case BeforeModulesAirAnnotation beforeModules:
                aggregation.AddBeforeModules(beforeModules.Modules);
                break;
            case AfterModulesAirAnnotation afterModules:
                aggregation.AddAfterModules(afterModules.Modules);
                break;
            case BackendAirAnnotation backend:
                aggregation.AddBackends(backend.Backends);
                break;
            case AllowIntrinsicAirAnnotation allowIntrinsic:
                aggregation.AddAllowedIntrinsic(allowIntrinsic.IntrinsicName);
                break;
            case ForbidIntrinsicAirAnnotation forbidIntrinsic:
                aggregation.AddForbiddenIntrinsic(forbidIntrinsic.IntrinsicName);
                break;
            case EnableIntrinsicAirAnnotation enableIntrinsic:
                aggregation.AddEnabledIntrinsic(enableIntrinsic.IntrinsicName);
                break;
            case DisableIntrinsicAirAnnotation disableIntrinsic:
                aggregation.AddDisabledIntrinsic(disableIntrinsic.IntrinsicName);
                break;
            case SecurityAirAnnotation security:
                aggregation.SetSecurityProfile(security.Profile);
                break;
            case CapabilityAirAnnotation capability:
                aggregation.AddCapabilities(capability.Capabilities);
                break;
            case null:
                Thrower.InvalidOpEx("Dialect AIR contained a null annotation entry.");
                break;
            default:
                Thrower.InvalidOpEx($"Dialect AIR contained unsupported annotation type '{annotation.GetType().FullName}'.");
                break;
        }
    }
}

public sealed class DialectDefinitionSliceBuilder
{
    public DialectDefinitionSlice Build(DialectDefinitionAggregation aggregation)
    {
        if (aggregation == null)
        {
            Thrower.ArgumentNull(nameof(aggregation));
        }

        if (string.IsNullOrWhiteSpace(aggregation.DialectName))
        {
            Thrower.InvalidOpEx("Dialect AIR is missing a DialectNameAirAnnotation.");
        }

        return new DialectDefinitionSlice(
            aggregation.DialectName,
            aggregation.UseModules,
            aggregation.ExcludeModules,
            BuildOrderDirectives(aggregation),
            BuildBackendDirectives(aggregation.Backends),
            BuildIntrinsicDirectives(aggregation.AllowedIntrinsics, aggregation.ForbiddenIntrinsics),
            BuildOptimizerDirectives(aggregation.EnabledIntrinsics, aggregation.DisabledIntrinsics),
            aggregation.SecurityProfile,
            BuildCapabilities(aggregation.Capabilities));
    }

    private static IReadOnlyList<DialectOrderDirective> BuildOrderDirectives(DialectOrderDirectiveKind kind, IReadOnlyList<string> modules)
    {
        var result = new List<DialectOrderDirective>();
        for (var i = 0; i + 1 < modules.Count; i++)
        {
            result.Add(new DialectOrderDirective(kind, modules[i], modules[i + 1]));
        }

        return result;
    }

    private static IReadOnlyList<DialectOrderDirective> BuildOrderDirectives(DialectDefinitionAggregation aggregation)
    {
        var result = new List<DialectOrderDirective>();
        result.AddRange(BuildOrderDirectives(DialectOrderDirectiveKind.Requires, aggregation.RequiresModules));
        result.AddRange(BuildOrderDirectives(DialectOrderDirectiveKind.Before, aggregation.BeforeModules));
        result.AddRange(BuildOrderDirectives(DialectOrderDirectiveKind.After, aggregation.AfterModules));
        return result;
    }

    private static IReadOnlyList<DialectBackendDirective> BuildBackendDirectives(IReadOnlyList<string> backends)
    {
        return backends.Select(x => new DialectBackendDirective(DialectAnnotationValueGuard.ParseBackend(x), true)).ToList();
    }

    private static IReadOnlyList<DialectIntrinsicDirective> BuildIntrinsicDirectives(IReadOnlyList<string> allowed, IReadOnlyList<string> forbidden)
    {
        var result = new List<DialectIntrinsicDirective>();
        result.AddRange(allowed.Select(x => new DialectIntrinsicDirective(x, true, DialectBackendTarget.Any)));
        result.AddRange(forbidden.Select(x => new DialectIntrinsicDirective(x, false, DialectBackendTarget.Any)));
        return result;
    }

    private static IReadOnlyList<DialectOptimizerDirective> BuildOptimizerDirectives(IReadOnlyList<string> enabled, IReadOnlyList<string> disabled)
    {
        var result = new List<DialectOptimizerDirective>();
        result.AddRange(enabled.Select(x => new DialectOptimizerDirective(x, true, DialectBackendTarget.Any)));
        result.AddRange(disabled.Select(x => new DialectOptimizerDirective(x, false, DialectBackendTarget.Any)));
        return result;
    }

    private static IReadOnlyList<DialectCapabilityDirective> BuildCapabilities(IReadOnlyList<string> capabilities)
    {
        return capabilities.Select(x => new DialectCapabilityDirective(x, true)).ToList();
    }
}

public sealed class DialectDefinitionAggregation
{
    private readonly List<string> _useModules = [];
    private readonly List<string> _excludeModules = [];
    private readonly List<string> _requiresModules = [];
    private readonly List<string> _beforeModules = [];
    private readonly List<string> _afterModules = [];
    private readonly List<string> _backends = [];
    private readonly List<string> _allowedIntrinsics = [];
    private readonly List<string> _forbiddenIntrinsics = [];
    private readonly List<string> _enabledIntrinsics = [];
    private readonly List<string> _disabledIntrinsics = [];
    private readonly List<string> _capabilities = [];
    private readonly HashSet<string> _seenUseModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenExcludeModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenRequiresModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenBeforeModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenAfterModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenBackends = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenCapabilities = new(StringComparer.Ordinal);
    private readonly HashSet<string> _allowedIntrinsicSet = new(StringComparer.Ordinal);
    private readonly HashSet<string> _forbiddenIntrinsicSet = new(StringComparer.Ordinal);
    private readonly HashSet<string> _enabledIntrinsicSet = new(StringComparer.Ordinal);
    private readonly HashSet<string> _disabledIntrinsicSet = new(StringComparer.Ordinal);

    public string? DialectName { get; private set; }

    public DialectSecurityProfile? SecurityProfile { get; private set; }

    public IReadOnlyList<string> UseModules => _useModules;

    public IReadOnlyList<string> ExcludeModules => _excludeModules;

    public IReadOnlyList<string> RequiresModules => _requiresModules;

    public IReadOnlyList<string> BeforeModules => _beforeModules;

    public IReadOnlyList<string> AfterModules => _afterModules;

    public IReadOnlyList<string> Backends => _backends;

    public IReadOnlyList<string> AllowedIntrinsics => _allowedIntrinsics;

    public IReadOnlyList<string> ForbiddenIntrinsics => _forbiddenIntrinsics;

    public IReadOnlyList<string> EnabledIntrinsics => _enabledIntrinsics;

    public IReadOnlyList<string> DisabledIntrinsics => _disabledIntrinsics;

    public IReadOnlyList<string> Capabilities => _capabilities;

    public void SetDialectName(string name)
    {
        if (DialectName != null)
        {
            Thrower.InvalidOpEx($"Dialect AIR contained duplicate DialectNameAirAnnotation values '{DialectName}' and '{name}'.");
        }

        DialectName = DialectAnnotationValueGuard.RequireValue(name, nameof(name), "Dialect name must not be empty.");
    }

    public void SetSecurityProfile(DialectSecurityProfile profile)
    {
        if (SecurityProfile != null)
        {
            Thrower.InvalidOpEx($"Dialect AIR contained duplicate singleton annotation '{nameof(SecurityAirAnnotation)}'.");
        }

        SecurityProfile = profile;
    }

    public void AddUseModules(IReadOnlyList<string> values) => AddMany(values, _useModules, _seenUseModules, "Duplicate use module annotation value '{0}'.");

    public void AddExcludeModules(IReadOnlyList<string> values) => AddMany(values, _excludeModules, _seenExcludeModules, "Duplicate exclude module annotation value '{0}'.");

    public void AddRequiresModules(IReadOnlyList<string> values) => AddMany(values, _requiresModules, _seenRequiresModules, "Duplicate requires module annotation value '{0}'.");

    public void AddBeforeModules(IReadOnlyList<string> values) => AddMany(values, _beforeModules, _seenBeforeModules, "Duplicate before module annotation value '{0}'.");

    public void AddAfterModules(IReadOnlyList<string> values) => AddMany(values, _afterModules, _seenAfterModules, "Duplicate after module annotation value '{0}'.");

    public void AddBackends(IReadOnlyList<string> values) => AddMany(values, _backends, _seenBackends, "Duplicate backend annotation value '{0}'.");

    public void AddCapabilities(IReadOnlyList<string> values) => AddMany(values, _capabilities, _seenCapabilities, "Duplicate capability annotation value '{0}'.");

    public void AddAllowedIntrinsic(string value)
    {
        AddOne(value, _allowedIntrinsics, _allowedIntrinsicSet, "Duplicate allow intrinsic annotation value '{0}'.");
        if (_forbiddenIntrinsicSet.Contains(value))
        {
            Thrower.InvalidOpEx($"Intrinsic '{value}' cannot be both allowed and forbidden in AIR annotations.");
        }
    }

    public void AddForbiddenIntrinsic(string value)
    {
        AddOne(value, _forbiddenIntrinsics, _forbiddenIntrinsicSet, "Duplicate forbid intrinsic annotation value '{0}'.");
        if (_allowedIntrinsicSet.Contains(value))
        {
            Thrower.InvalidOpEx($"Intrinsic '{value}' cannot be both allowed and forbidden in AIR annotations.");
        }
    }

    public void AddEnabledIntrinsic(string value)
    {
        AddOne(value, _enabledIntrinsics, _enabledIntrinsicSet, "Duplicate enable intrinsic annotation value '{0}'.");
        if (_disabledIntrinsicSet.Contains(value))
        {
            Thrower.InvalidOpEx($"Intrinsic '{value}' cannot be both enabled and disabled in AIR annotations.");
        }
    }

    public void AddDisabledIntrinsic(string value)
    {
        AddOne(value, _disabledIntrinsics, _disabledIntrinsicSet, "Duplicate disable intrinsic annotation value '{0}'.");
        if (_enabledIntrinsicSet.Contains(value))
        {
            Thrower.InvalidOpEx($"Intrinsic '{value}' cannot be both enabled and disabled in AIR annotations.");
        }
    }

    private static void AddMany(IReadOnlyList<string> values, List<string> target, HashSet<string> seen, string duplicateMessage)
    {
        foreach (var value in values)
        {
            AddOne(value, target, seen, duplicateMessage);
        }
    }

    private static void AddOne(string value, List<string> target, HashSet<string> seen, string duplicateMessage)
    {
        var normalized = DialectAnnotationValueGuard.RequireValue(value, nameof(value), "Dialect annotation value must not be empty.");
        if (!seen.Add(normalized))
        {
            Thrower.InvalidOpEx(string.Format(duplicateMessage, normalized));
        }

        target.Add(normalized);
    }
}
