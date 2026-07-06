using ExceptionsManager;

namespace UniversalToolchain.Dialects.Integration;

public sealed class RuntimeProfileSelection
{
    public RuntimeProfileSelection(
        string profileName,
        RuntimeProfileOverridePolicy overridePolicy = RuntimeProfileOverridePolicy.ExplicitSourceWins)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            Thrower.Argument(nameof(profileName), "Runtime profile name must not be empty.");

        if (!Enum.IsDefined(overridePolicy))
            Thrower.Argument(nameof(overridePolicy), "Runtime profile override policy is not defined.");

        ProfileName = profileName;
        OverridePolicy = overridePolicy;
    }

    public string ProfileName { get; }

    public RuntimeProfileOverridePolicy OverridePolicy { get; }
}
