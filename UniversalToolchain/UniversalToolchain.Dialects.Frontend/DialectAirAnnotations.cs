using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public interface IDialectDefinitionSliceAnnotation
{
    void Apply(DialectDefinitionAggregation aggregation);
}

public sealed class DialectNameAirAnnotation(string name) : IDialectDefinitionSliceAnnotation
{
    public string Name { get; } = RequireValue(name, nameof(name), "Dialect name annotation must not be empty.");

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation = aggregation.ArgNotNull();

        aggregation.SetDialectName(Name);
    }

    private static string RequireValue(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(paramName, message);

        return value;
    }
}

public sealed class DialectVersionAirAnnotation(string version) : IDialectDefinitionSliceAnnotation
{
    public string Version { get; } = DialectAnnotationValueGuard.RequireValue(
        version,
        nameof(version),
        "Dialect version annotation must not be empty.");

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation = aggregation.ArgNotNull();

        aggregation.SetVersion(Version);
    }
}

public sealed class BaseDialectAirAnnotation(string baseDialectName) : IDialectDefinitionSliceAnnotation
{
    public string BaseDialectName { get; } = DialectAnnotationValueGuard.RequireValue(
        baseDialectName,
        nameof(baseDialectName),
        "Base dialect annotation must not be empty.");

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation = aggregation.ArgNotNull();

        aggregation.SetBaseDialectName(BaseDialectName);
    }
}

public sealed class UseModulesAirAnnotation(IReadOnlyList<string> modules) : IDialectDefinitionSliceAnnotation
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Use modules annotation must not contain empty values.");

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddUseModules(Modules);
    }
}

public sealed class ExcludeModulesAirAnnotation(IReadOnlyList<string> modules) : IDialectDefinitionSliceAnnotation
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Exclude modules annotation must not contain empty values.");

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddExcludeModules(Modules);
    }
}

public sealed class OrderAirAnnotation : IDialectDefinitionSliceAnnotation
{
    public OrderAirAnnotation(DialectOrderDirectiveKind kind, IReadOnlyList<string> modules)
    {
        Kind = kind;
        Modules = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Order annotation must not contain empty values.");
    }

    public DialectOrderDirectiveKind Kind { get; }

    public IReadOnlyList<string> Modules { get; }

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddOrderDirectives(BuildOrderDirectives(Kind, Modules));
    }

    private static IReadOnlyList<DialectOrderDirective> BuildOrderDirectives(DialectOrderDirectiveKind kind, IReadOnlyList<string> modules)
    {
        var result = new List<DialectOrderDirective>();
        for (var i = 0; i + 1 < modules.Count; i++)
            result.Add(new DialectOrderDirective(kind, modules[i], modules[i + 1]));

        return result;
    }
}

public sealed class BackendAirAnnotation : IDialectDefinitionSliceAnnotation
{
    public BackendAirAnnotation(IReadOnlyList<string> backends)
    {
        Backends = DialectAnnotationValueGuard.RequireList(backends, nameof(backends), "Backend annotation must not contain empty values.");
    }

    public IReadOnlyList<string> Backends { get; }

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddBackends(Backends.Select(x => new DialectBackendDirective(DialectAnnotationValueGuard.ParseBackend(x), true)).ToList());
    }
}

public sealed class IntrinsicAirAnnotation : IDialectDefinitionSliceAnnotation
{
    public IntrinsicAirAnnotation(string intrinsicName, bool allowed, DialectBackendSelector? target = null)
    {
        IntrinsicName = DialectAnnotationValueGuard.RequireValue(intrinsicName, nameof(intrinsicName), "Intrinsic annotation must not be empty.");
        Allowed = allowed;
        Target = target ?? DialectBackendSelector.Any;
    }

    public string IntrinsicName { get; }

    public bool Allowed { get; }

    public DialectBackendSelector Target { get; }

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddIntrinsicDirectives([new DialectIntrinsicDirective(IntrinsicName, Allowed, Target)]);
    }
}

public sealed class OptimizerAirAnnotation : IDialectDefinitionSliceAnnotation
{
    public OptimizerAirAnnotation(string optimizerName, bool enabled, DialectBackendSelector? target = null)
    {
        OptimizerName = DialectAnnotationValueGuard.RequireValue(optimizerName, nameof(optimizerName), "Optimizer annotation must not be empty.");
        Enabled = enabled;
        Target = target ?? DialectBackendSelector.Any;
    }

    public string OptimizerName { get; }

    public bool Enabled { get; }

    public DialectBackendSelector Target { get; }

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddOptimizerDirectives([new DialectOptimizerDirective(OptimizerName, Enabled, Target)]);
    }
}

public sealed class SecurityAirAnnotation(DialectSecurityProfile profile) : IDialectDefinitionSliceAnnotation
{
    public DialectSecurityProfile Profile { get; } = profile;

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.SetSecurityProfile(Profile);
    }
}

public sealed class CapabilityAirAnnotation(IReadOnlyList<string> capabilities) : IDialectDefinitionSliceAnnotation
{
    public IReadOnlyList<string> Capabilities { get; } = DialectAnnotationValueGuard.RequireList(capabilities, nameof(capabilities), "Capability annotation must not contain empty values.");

    public void Apply(DialectDefinitionAggregation aggregation)
    {
        aggregation.AddCapabilities(Capabilities.Select(x => new DialectCapabilityDirective(x, true)).ToList());
    }
}

internal static class DialectAnnotationValueGuard
{
    public static string RequireValue(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(paramName, message);

        return value;
    }

    public static IReadOnlyList<string> RequireList(IReadOnlyList<string> values, string paramName, string message)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        var result = new List<string>(values.Count);
        foreach (var value in values)
            result.Add(RequireValue(value, paramName, message));

        return result;
    }

    public static DialectBackendId ParseBackend(string value)
    {
        if (!DialectBackendSelectorText.TryParseId(value, out var backendId))
            Thrower.Argument(nameof(value), $"Backend '{value}' is not supported.");

        return backendId;
    }

    public static DialectSecurityProfile ParseSecurityProfile(string value)
    {
        if (string.Equals(value, "trusted", StringComparison.Ordinal))
            return DialectSecurityProfile.Trusted;

        if (string.Equals(value, "restricted", StringComparison.Ordinal))
            return DialectSecurityProfile.Restricted;

        Thrower.Argument(nameof(value), $"Security profile '{value}' is not supported. Expected 'trusted' or 'restricted'.");
        return DialectSecurityProfile.Trusted;
    }
}