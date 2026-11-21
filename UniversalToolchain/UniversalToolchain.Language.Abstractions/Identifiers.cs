namespace UniversalToolchain.Language.Abstractions;

internal static class LanguageValueValidation
{
    public static string Required(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value must not be empty.", paramName);
        return value.Trim();
    }
}

public readonly record struct LanguageId
{
    public LanguageId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageVersion
{
    public LanguageVersion(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageFeatureId
{
    public LanguageFeatureId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguagePackageId
{
    public LanguagePackageId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct BackendId
{
    public BackendId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageContributionId
{
    public LanguageContributionId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageCapabilityId
{
    public LanguageCapabilityId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageSlotId
{
    public LanguageSlotId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

/// <summary>
/// Descriptive identity of a grouping of contributions that belong to one mechanism.
/// <para>
/// A module is <em>not</em> a planner concept: the planner selects features and contributions, and a
/// module never adds, removes or reorders anything. The identity exists so that a package can state
/// which mechanism owns which phase of which contribution, which is what lowering dispatch and
/// observability need. It carries no selection authority.
/// </para>
/// </summary>
public readonly record struct LanguageModuleId
{
    public LanguageModuleId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageArtifactKindId
{
    public LanguageArtifactKindId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct LanguageRuntimeProviderId
{
    public LanguageRuntimeProviderId(string value) => Value = LanguageValueValidation.Required(value, nameof(value));
    public string Value { get; }
    public override string ToString() => Value;
}

public readonly record struct ToolchainApiVersion
{
    public ToolchainApiVersion(int major)
    {
        if (major <= 0)
            throw new ArgumentOutOfRangeException(nameof(major), "Toolchain API major version must be positive.");
        Major = major;
    }
    public int Major { get; }
    public override string ToString() => Major.ToString(System.Globalization.CultureInfo.InvariantCulture);
}

public static class ToolchainApi
{
    public static ToolchainApiVersion Current { get; } = new(1);
}

public static class LanguageCapabilities
{
    public static LanguageCapabilityId Backend(BackendId backend) => new($"backend:{backend.Value}");
    public static LanguageCapabilityId RuntimeProvider { get; } = new("runtime.provider");
}

public static class LanguageSlots
{
    public static LanguageSlotId FrontendSyntax { get; } = new("frontend.syntax");
    public static LanguageSlotId FrontendParser { get; } = new("frontend.parser");
    public static LanguageSlotId SemanticsBinding { get; } = new("semantics.binding");
    public static LanguageSlotId SemanticsTypes { get; } = new("semantics.types");
    public static LanguageSlotId Lowering { get; } = new("lowering");
    public static LanguageSlotId Operations { get; } = new("operations");
    public static LanguageSlotId Optimizers { get; } = new("optimizers");
    public static LanguageSlotId Backends { get; } = new("backends");
    public static LanguageSlotId RuntimeProvider { get; } = new("runtime.provider");
    public static LanguageSlotId Tooling { get; } = new("tooling");
}

public static class LanguageArtifacts
{
    public static LanguageArtifactKindId SourceText { get; } = new("source.text");
    public static LanguageArtifactKindId Air { get; } = new("ir.air");
    public static LanguageArtifactKindId Ssa { get; } = new("ir.ssa");
}
