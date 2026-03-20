namespace UniversalToolchain.Dialects.Abstractions;

/// <summary>
///     Defines selected security profile for a dialect.
/// </summary>
public sealed class SecurityPolicy
{
    public SecurityPolicy(SecurityProfile profile)
    {
        if (!Enum.IsDefined(profile))
            Thrower.Argument(nameof(profile), "Security profile is not defined.");

        Profile = profile;
    }

    public SecurityProfile Profile { get; }
}