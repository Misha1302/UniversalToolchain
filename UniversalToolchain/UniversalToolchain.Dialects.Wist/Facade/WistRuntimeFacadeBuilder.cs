using System.Reflection.Emit;
using BasicCore.Compilation;
using IntermediateRepresentationAbstractions;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist.Presets;
using UniversalToolchain.Dialects.Wist.Rules;

namespace UniversalToolchain.Dialects.Wist.Facade;

/// <summary>
///     Builds Wist runtime facades without exposing service wiring to first-contact users.
/// </summary>
public sealed class WistRuntimeFacadeBuilder
{
    private string? _dialectFilePath;
    private WistShippedDialectPreset _preset = WistShippedDialectPresets.Default;

    private WistRuntimeFacadeBuilder()
    {
    }

    /// <summary>
    ///     Creates a builder for the default Wist facade profile.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateDefault() => new();

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
    ///     Uses a shipped Wist dialect preset when no explicit dialect file is configured.
    /// </summary>
    public WistRuntimeFacadeBuilder WithShippedDialectPreset(string presetId)
        => WithShippedDialectPreset(WistShippedDialectPresets.GetRequired(presetId));

    /// <summary>
    ///     Uses a shipped Wist dialect preset when no explicit dialect file is configured.
    /// </summary>
    public WistRuntimeFacadeBuilder WithShippedDialectPreset(WistShippedDialectPreset preset)
    {
        _preset = preset.ArgNotNull();
        return this;
    }

    /// <summary>
    ///     Builds a facade over a composed Wist dialect runtime host.
    /// </summary>
    public WistRuntimeFacade Build()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialectFilePath = _dialectFilePath ?? new WistShippedDialectFileResolver().Resolve(_preset);
        var composition = workflow.ComposeFile(dialectFilePath);

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        var host = workflow.CreateHost(composition);
        var ruleSetCompiler = new WistRuleSetCompiler((source, declaredBindings, mode) =>
        {
            if (!host.Configuration.TryResolveKnownBackendId(mode, out var backendId))
                Thrower.InvalidOpEx($"Unknown execution mode '{mode}'.");

            if (backendId == WistDialectBackendIds.Interpreter)
                return host.GetArtifactCompiler<IAbstractIR>(mode).Compile(source, declaredBindings);

            if (backendId == WistDialectBackendIds.Cil)
                return host.GetArtifactCompiler<DynamicMethod>(mode).Compile(source, declaredBindings);

            return Thrower.InvalidOpEx<ICompiledArtifact>($"Unsupported Wist facade backend '{mode}'.");
        });

        return new WistRuntimeFacade(host, composition, ruleSetCompiler);
    }
}
