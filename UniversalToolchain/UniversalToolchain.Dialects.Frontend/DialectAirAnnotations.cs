using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectNameAirAnnotation(string name)
{
    public string Name { get; } = RequireValue(name, nameof(name), "Dialect name annotation must not be empty.");

    private static string RequireValue(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Thrower.Argument(paramName, message);
        }

        return value;
    }
}

public sealed class UseModulesAirAnnotation(IReadOnlyList<string> modules)
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Use modules annotation must not contain empty values.");
}

public sealed class ExcludeModulesAirAnnotation(IReadOnlyList<string> modules)
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Exclude modules annotation must not contain empty values.");
}

public sealed class RequiresModulesAirAnnotation(IReadOnlyList<string> modules)
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Requires modules annotation must not contain empty values.");
}

public sealed class BeforeModulesAirAnnotation(IReadOnlyList<string> modules)
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "Before modules annotation must not contain empty values.");
}

public sealed class AfterModulesAirAnnotation(IReadOnlyList<string> modules)
{
    public IReadOnlyList<string> Modules { get; } = DialectAnnotationValueGuard.RequireList(modules, nameof(modules), "After modules annotation must not contain empty values.");
}

public sealed class BackendAirAnnotation(IReadOnlyList<string> backends)
{
    public IReadOnlyList<string> Backends { get; } = DialectAnnotationValueGuard.RequireList(backends, nameof(backends), "Backend annotation must not contain empty values.");
}

public sealed class AllowIntrinsicAirAnnotation(string intrinsicName)
{
    public string IntrinsicName { get; } = DialectAnnotationValueGuard.RequireValue(intrinsicName, nameof(intrinsicName), "Allow intrinsic annotation must not be empty.");
}

public sealed class ForbidIntrinsicAirAnnotation(string intrinsicName)
{
    public string IntrinsicName { get; } = DialectAnnotationValueGuard.RequireValue(intrinsicName, nameof(intrinsicName), "Forbid intrinsic annotation must not be empty.");
}

public sealed class EnableIntrinsicAirAnnotation(string intrinsicName)
{
    public string IntrinsicName { get; } = DialectAnnotationValueGuard.RequireValue(intrinsicName, nameof(intrinsicName), "Enable intrinsic annotation must not be empty.");
}

public sealed class DisableIntrinsicAirAnnotation(string intrinsicName)
{
    public string IntrinsicName { get; } = DialectAnnotationValueGuard.RequireValue(intrinsicName, nameof(intrinsicName), "Disable intrinsic annotation must not be empty.");
}

public sealed class SecurityAirAnnotation(DialectSecurityProfile profile)
{
    public DialectSecurityProfile Profile { get; } = profile;
}

public sealed class CapabilityAirAnnotation(IReadOnlyList<string> capabilities)
{
    public IReadOnlyList<string> Capabilities { get; } = DialectAnnotationValueGuard.RequireList(capabilities, nameof(capabilities), "Capability annotation must not contain empty values.");
}

internal static class DialectAnnotationValueGuard
{
    public static string RequireValue(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Thrower.Argument(paramName, message);
        }

        return value;
    }

    public static IReadOnlyList<string> RequireList(IReadOnlyList<string> values, string paramName, string message)
    {
        if (values == null)
        {
            Thrower.ArgumentNull(paramName);
        }

        var result = new List<string>(values.Count);
        foreach (var value in values)
        {
            result.Add(RequireValue(value, paramName, message));
        }

        return result;
    }

    public static DialectBackendTarget ParseBackend(string value)
    {
        if (!DialectBackendTargetText.TryParse(value, allowAny: false, out var target))
        {
            Thrower.Argument(nameof(value), $"Backend '{value}' is not supported. Expected one of: interpreter, cil.");
        }

        return target;
    }

    public static DialectSecurityProfile ParseSecurityProfile(string value)
    {
        if (string.Equals(value, "trusted", StringComparison.Ordinal))
        {
            return DialectSecurityProfile.Trusted;
        }

        if (string.Equals(value, "restricted", StringComparison.Ordinal))
        {
            return DialectSecurityProfile.Restricted;
        }

        Thrower.Argument(nameof(value), $"Security profile '{value}' is not supported. Expected 'trusted' or 'restricted'.");
        return DialectSecurityProfile.Trusted;
    }
}
