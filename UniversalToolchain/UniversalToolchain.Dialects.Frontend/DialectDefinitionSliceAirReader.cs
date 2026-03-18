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
            if (annotation == null)
            {
                Thrower.InvalidOpEx("Dialect AIR contained a null annotation entry.");
            }

            if (annotation is IDialectDefinitionSliceAnnotation dialectAnnotation)
            {
                dialectAnnotation.Apply(aggregation);
            }
        }

        return new DialectDefinitionSliceBuilder().Build(aggregation);
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
            aggregation.OrderDirectives,
            aggregation.Backends,
            aggregation.IntrinsicDirectives,
            aggregation.OptimizerDirectives,
            aggregation.SecurityProfile,
            aggregation.Capabilities);
    }
}

public sealed class DialectDefinitionAggregation
{
    private readonly List<string> _useModules = [];
    private readonly List<string> _excludeModules = [];
    private readonly List<DialectOrderDirective> _orderDirectives = [];
    private readonly List<DialectBackendDirective> _backends = [];
    private readonly List<DialectIntrinsicDirective> _intrinsicDirectives = [];
    private readonly List<DialectOptimizerDirective> _optimizerDirectives = [];
    private readonly List<DialectCapabilityDirective> _capabilities = [];
    private readonly HashSet<string> _seenUseModules = new(StringComparer.Ordinal);
    private readonly HashSet<string> _seenExcludeModules = new(StringComparer.Ordinal);
    private readonly HashSet<(DialectOrderDirectiveKind Kind, string Source, string Target)> _seenOrderDirectives = [];
    private readonly HashSet<DialectBackendTarget> _seenBackends = [];
    private readonly HashSet<(string Name, DialectBackendTarget Target)> _seenIntrinsics = [];
    private readonly HashSet<(string Name, DialectBackendTarget Target)> _seenOptimizers = [];
    private readonly HashSet<string> _seenCapabilities = new(StringComparer.Ordinal);

    public string? DialectName { get; private set; }

    public DialectSecurityProfile? SecurityProfile { get; private set; }

    public IReadOnlyList<string> UseModules => _useModules;

    public IReadOnlyList<string> ExcludeModules => _excludeModules;

    public IReadOnlyList<DialectOrderDirective> OrderDirectives => _orderDirectives;

    public IReadOnlyList<DialectBackendDirective> Backends => _backends;

    public IReadOnlyList<DialectIntrinsicDirective> IntrinsicDirectives => _intrinsicDirectives;

    public IReadOnlyList<DialectOptimizerDirective> OptimizerDirectives => _optimizerDirectives;

    public IReadOnlyList<DialectCapabilityDirective> Capabilities => _capabilities;

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

    public void AddOrderDirectives(IReadOnlyList<DialectOrderDirective> directives)
    {
        foreach (var directive in directives)
        {
            var key = (directive.Kind, directive.SourceModule, directive.TargetModule);
            if (!_seenOrderDirectives.Add(key))
            {
                Thrower.InvalidOpEx($"Duplicate order annotation value '{directive.Directive}:{directive.SourceModule}->{directive.TargetModule}'.");
            }

            _orderDirectives.Add(directive);
        }
    }

    public void AddBackends(IReadOnlyList<DialectBackendDirective> values)
    {
        foreach (var value in values)
        {
            if (!_seenBackends.Add(value.Backend))
            {
                Thrower.InvalidOpEx($"Duplicate backend annotation value '{DialectBackendTargetText.ToText(value.Backend)}'.");
            }

            _backends.Add(value);
        }
    }

    public void AddIntrinsicDirectives(IReadOnlyList<DialectIntrinsicDirective> values)
    {
        foreach (var value in values)
        {
            var key = (value.Name, value.Target);
            if (!_seenIntrinsics.Add(key))
            {
                Thrower.InvalidOpEx($"Duplicate intrinsic annotation value '{value.Name}' for '{DialectBackendTargetText.ToText(value.Target)}'.");
            }

            _intrinsicDirectives.Add(value);
        }
    }

    public void AddOptimizerDirectives(IReadOnlyList<DialectOptimizerDirective> values)
    {
        foreach (var value in values)
        {
            var key = (value.Name, value.Target);
            if (!_seenOptimizers.Add(key))
            {
                Thrower.InvalidOpEx($"Duplicate optimizer annotation value '{value.Name}' for '{DialectBackendTargetText.ToText(value.Target)}'.");
            }

            _optimizerDirectives.Add(value);
        }
    }

    public void AddCapabilities(IReadOnlyList<DialectCapabilityDirective> values)
    {
        foreach (var value in values)
        {
            if (!_seenCapabilities.Add(value.Name))
            {
                Thrower.InvalidOpEx($"Duplicate capability annotation value '{value.Name}'.");
            }

            _capabilities.Add(value);
        }
    }

    private static void AddMany(IReadOnlyList<string> values, List<string> target, HashSet<string> seen, string duplicateMessage)
    {
        foreach (var value in values)
        {
            var normalized = DialectAnnotationValueGuard.RequireValue(value, nameof(values), "Dialect annotation value must not be empty.");
            if (!seen.Add(normalized))
            {
                Thrower.InvalidOpEx(string.Format(duplicateMessage, normalized));
            }

            target.Add(normalized);
        }
    }
}
