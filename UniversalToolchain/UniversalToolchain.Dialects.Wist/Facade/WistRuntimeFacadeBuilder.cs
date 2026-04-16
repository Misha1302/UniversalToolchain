using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Builds Wist runtime facades without exposing service wiring to first-contact users.
/// </summary>
public sealed class WistRuntimeFacadeBuilder
{
    private const string SafeDefaultDialectText = """
                                                  dialect SafeDefault
                                                  use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                                  backend cil,interpreter
                                                  enable BooleanOptimization
                                                  enable ComparisonIntrinsicOptimization
                                                  enable LocalVariablesOptimization
                                                  security restricted
                                                  """;

    private const string TrustedDefaultDialectText = """
                                                     dialect TrustedDefault
                                                     use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,Numbers,Scopes,SemicolonAsNewLine,Variables,Whitespaces
                                                     backend cil,interpreter
                                                     enable BooleanOptimization
                                                     enable ComparisonIntrinsicOptimization
                                                     enable LocalVariablesOptimization
                                                     security trusted
                                                     capability unsafe-interop
                                                     """;

    private string? _dialectFilePath;
    private string _builtInDialectText = SafeDefaultDialectText;

    private WistRuntimeFacadeBuilder()
    {
    }

    /// <summary>
    ///     Creates a builder for the safe first-contact Wist facade profile with no C# interop, restricted security,
    ///     and a runtime surface intended for formulas and rules onboarding.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateDefault()
    {
        return new WistRuntimeFacadeBuilder
        {
            _builtInDialectText = SafeDefaultDialectText
        };
    }

    /// <summary>
    ///     Creates a builder for trusted Wist composition with C# interop enabled as an explicit opt-in profile that
    ///     must not be used for untrusted input.
    /// </summary>
    public static WistRuntimeFacadeBuilder CreateTrustedDefault()
    {
        return new WistRuntimeFacadeBuilder
        {
            _builtInDialectText = TrustedDefaultDialectText
        };
    }

    /// <summary>
    ///     Uses a Wist dialect file instead of the selected built-in facade profile.
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
            ? workflow.ComposeText(_builtInDialectText, "wist-facade-default")
            : workflow.ComposeFile(_dialectFilePath);

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return new WistRuntimeFacade(workflow.CreateHost(composition));
    }
}
