using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds Wist runtime facades without exposing service wiring to first-contact users.
/// </summary>
public sealed class WistRuntimeFacadeBuilder
{
    private const string TrustedDefaultDialectText = """
                                                    dialect FullDefault
                                                    use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                                    backend cil,interpreter
                                                    enable BooleanOptimization
                                                    enable ComparisonIntrinsicOptimization
                                                    enable LocalVariablesOptimization
                                                    security trusted
                                                    capability unsafe-interop
                                                    """;

    private readonly string _builtInDialectText;
    private readonly string _syntheticSourceName;
    private string? _dialectFilePath;

    private WistRuntimeFacadeBuilder(string builtInDialectText, string syntheticSourceName)
    {
        _builtInDialectText = builtInDialectText;
        _syntheticSourceName = syntheticSourceName;
    }

    /// <summary>
    ///     Creates a builder for the current default built-in Wist facade profile, which is trusted and interop-enabled.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateDefault() => CreateTrustedDefault();

    /// <summary>
    ///     Creates a builder for the explicit trusted built-in Wist facade profile.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateTrustedDefault()
        => new(TrustedDefaultDialectText, "wist-facade-trusted-default");

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
            ? workflow.ComposeText(_builtInDialectText, _syntheticSourceName)
            : workflow.ComposeFile(_dialectFilePath);

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return new WistRuntimeFacade(workflow.CreateHost(composition));
    }
}
