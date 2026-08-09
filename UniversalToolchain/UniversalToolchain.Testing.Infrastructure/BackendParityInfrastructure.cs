using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Testing.Infrastructure;

/// <summary>
/// Unified parity infrastructure over one canonical Wist LanguagePlan and LanguageRuntime.
/// </summary>
public static class BackendParityInfrastructure
{
    private static readonly BackendId Cil = new("cil");
    private static readonly BackendId Interpreter = new("interpreter");

    public static (BackendExecutionResult CompilerResult, BackendExecutionResult InterpreterResult) RunBoth(
        string dialectText,
        string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dialectText);
        ArgumentNullException.ThrowIfNull(code);

        var package = new WistLanguageFeaturePackage();
        var definition = CreateDualBackendDefinition(dialectText);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();
        using var runtime = LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider()));

        var compilerResult = ExecuteSafely(() =>
            WistRuntimeValueAdapterActivation.Normalize(
                plan,
                runtime.Run(new LanguageExecutionRequest(code, Cil)).Value));
        var interpreterResult = ExecuteSafely(() =>
            WistRuntimeValueAdapterActivation.Normalize(
                plan,
                runtime.Run(new LanguageExecutionRequest(code, Interpreter)).Value));
        return (compilerResult, interpreterResult);
    }

    public static void AssertSemanticParity(
        BackendExecutionResult compilerResult,
        BackendExecutionResult interpreterResult)
    {
        Assert.That(compilerResult.IsSuccess, Is.EqualTo(interpreterResult.IsSuccess),
            "Backends must either both succeed or both fail.");

        if (compilerResult.IsSuccess)
        {
            BackendResultAssertions.AssertEquivalent(compilerResult.Value, interpreterResult.Value);
            return;
        }

        var compilerException = compilerResult.Exception
            ?? throw new InvalidOperationException("Compiler result must contain exception on failure.");
        var interpreterException = interpreterResult.Exception
            ?? throw new InvalidOperationException("Interpreter result must contain exception on failure.");
        Assert.That(compilerException.GetType().FullName, Is.EqualTo(interpreterException.GetType().FullName));
        Assert.That(compilerException.Message, Is.EqualTo(interpreterException.Message));
    }

    public static double AsNumber(object? value) => BackendResultAssertions.AsNumber(value);
    public static bool AsBool(object? value) => BackendResultAssertions.AsBool(value);

    public static BackendExecutionResult ExecuteSafely(Func<object?> action)
    {
        try
        {
            return BackendExecutionResult.Success(action());
        }
        catch (Exception exception)
        {
            return BackendExecutionResult.Failure(exception);
        }
    }

    private static LanguageDefinition CreateDualBackendDefinition(string dialectText)
    {
        const string sourceName = "backend-parity-inline";
        var cilDefinition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            dialectText,
            sourceName,
            Cil.Value,
            WistFacadeSsaPolicy.Disabled);
        var interpreterDefinition = WistFacadeLanguageDefinitionFactory.FromDialectText(
            dialectText,
            sourceName,
            Interpreter.Value,
            WistFacadeSsaPolicy.Disabled);

        if (!cilDefinition.SelectedFeatures.SequenceEqual(interpreterDefinition.SelectedFeatures) ||
            cilDefinition.RuntimePolicy != interpreterDefinition.RuntimePolicy ||
            !cilDefinition.ContributionOrderConstraints.SequenceEqual(interpreterDefinition.ContributionOrderConstraints) ||
            !cilDefinition.IntrinsicPolicy.SequenceEqual(interpreterDefinition.IntrinsicPolicy))
        {
            throw new InvalidOperationException(
                "Wist parity translation produced backend-dependent language semantics before canonical planning.");
        }

        return new LanguageDefinition(
            cilDefinition.Id,
            cilDefinition.Version,
            cilDefinition.ToolchainApiVersion,
            cilDefinition.SelectedFeatures,
            [Cil, Interpreter],
            cilDefinition.RuntimeProvider,
            cilDefinition.RuntimePolicy,
            cilDefinition.Metadata,
            cilDefinition.SlotOverrides,
            cilDefinition.CapabilityProviders,
            cilDefinition.ExcludedContributions,
            cilDefinition.EntryArtifact,
            cilDefinition.ContributionOrderConstraints,
            cilDefinition.IntrinsicPolicy);
    }
}

public sealed record BackendExecutionResult(bool IsSuccess, object? Value, Exception? Exception)
{
    public static BackendExecutionResult Success(object? value) => new(true, value, null);
    public static BackendExecutionResult Failure(Exception exception) => new(false, null, exception);
}
