using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds Wist runtime facades without exposing service wiring to first-contact users.
/// </summary>
public sealed class WistRuntimeFacadeBuilder
{
    private readonly string _builtInDialectText;
    private readonly string _builtInSyntheticSourceName;
    private string? _dialectFilePath;

    private WistRuntimeFacadeBuilder(
        string builtInDialectText,
        string builtInSyntheticSourceName)
    {
        _builtInDialectText = builtInDialectText;
        _builtInSyntheticSourceName = builtInSyntheticSourceName;
    }

    /// <summary>
    ///     Creates a builder for the safe default Wist facade profile.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateDefault() => new(
        BuiltInFacadeDialectProfiles.SafeDefaultText,
        BuiltInFacadeDialectProfiles.SafeDefaultSyntheticSourceName);

    /// <summary>
    ///     Creates a builder for the trusted Wist facade profile with unsafe interop enabled.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateTrustedDefault() => new(
        BuiltInFacadeDialectProfiles.TrustedDefaultText,
        BuiltInFacadeDialectProfiles.TrustedDefaultSyntheticSourceName);

    /// <summary>
    ///     Uses a Wist dialect file instead of the default facade profile.
    /// </summary>
    public WistRuntimeFacadeBuilder WithDialectFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            Thrower.Argument(nameof(filePath), "Dialect file path must not be empty.");

        _dialectFilePath = filePath;
        return this;
    }

    /// <summary>
    ///     Builds a facade over a composed Wist dialect runtime host.
    /// </summary>
    public WistRuntimeFacade Build()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = _dialectFilePath == null
            ? workflow.ComposeText(_builtInDialectText, _builtInSyntheticSourceName)
            : workflow.ComposeFile(_dialectFilePath);

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return new WistRuntimeFacade(workflow.CreateHost(composition));
    }
}
