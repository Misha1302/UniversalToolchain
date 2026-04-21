using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Wist.Facade;

/// <summary>
///     Builds Wist runtime facades without exposing service wiring to first-contact users.
/// </summary>
public sealed class WistRuntimeFacadeBuilder
{
    private const string DefaultDialectText = """
                                              dialect FullDefault
                                              use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                              backend cil,interpreter
                                              enable BooleanOptimization
                                              enable ComparisonIntrinsicOptimization
                                              enable LocalVariablesOptimization
                                              security trusted
                                              capability unsafe-interop
                                              """;

    private string? _dialectFilePath;

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
            ? workflow.ComposeText(DefaultDialectText, "wist-facade-default")
            : workflow.ComposeFile(_dialectFilePath);

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return new WistRuntimeFacade(workflow.CreateHost(composition));
    }
}
