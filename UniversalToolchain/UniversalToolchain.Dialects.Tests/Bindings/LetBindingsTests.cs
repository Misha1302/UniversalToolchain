using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Features;
using UniversalToolchain.Features.Abstractions;
using UniversalToolchain.Features.Core;
using UniversalToolchain.Modules.Tests;

namespace UniversalToolchain.Dialects.Tests.Bindings;

[TestFixture]
public sealed class LetBindingsTests
{
    private const string LetBindingsDialect = """
                                              dialect LetBindings
                                              use Arithmetic,Identifier,NativeTypes,SafeMathFunctions,Scopes,Variables,Whitespaces
                                              backend compiler,interpreter
                                              """;

    [Test]
    public void LetBinding_CanReferencePreviousBinding()
    {
        var (compilerResult, interpreterResult) = ExecuteWithDeclaredBindings(
            """
            let base = price * quantity
            let result = base + fee
            result
            """);

        Assert.Multiple(() =>
        {
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult), Is.EqualTo(32d).Within(1e-9));
            Assert.That(BackendParityInfrastructure.AsNumber(interpreterResult), Is.EqualTo(32d).Within(1e-9));
        });
    }

    [Test]
    public void LetBinding_CanChainBindings()
    {
        var (compilerResult, interpreterResult) = ExecuteWithDeclaredBindings(
            """
            let base = price * quantity
            let discountValue = clamp(base * discount, 0.0, maxDiscount)
            let result = base - discountValue
            result
            """);

        Assert.Multiple(() =>
        {
            Assert.That(BackendParityInfrastructure.AsNumber(compilerResult), Is.EqualTo(25d).Within(1e-9));
            Assert.That(BackendParityInfrastructure.AsNumber(interpreterResult), Is.EqualTo(25d).Within(1e-9));
        });
    }

    [Test]
    public void LetBinding_CannotReferenceFutureBinding()
    {
        var (compilerResult, interpreterResult) = TryCompileInBothBackends(
            """
            result
            let result = price + fee
            """);

        AssertBindingFailure(
            compilerResult,
            interpreterResult,
            "WST-BIND-001",
            "used before its declaration");
    }

    [Test]
    public void LetBinding_CannotShadowDeclaredBinding()
    {
        var (compilerResult, interpreterResult) = TryCompileInBothBackends(
            """
            let price = fee
            price + fee
            """);

        AssertBindingFailure(
            compilerResult,
            interpreterResult,
            "WST-BIND-002",
            "cannot shadow a declared external binding");
    }

    [Test]
    public void LetBinding_DuplicateLocalName_ReturnsDiagnostic()
    {
        var (compilerResult, interpreterResult) = TryCompileInBothBackends(
            """
            let result = price
            let result = fee
            result
            """);

        AssertBindingFailure(
            compilerResult,
            interpreterResult,
            "WST-BIND-002",
            "already declared");
    }

    [Test]
    public void LetBinding_CompilerAndInterpreterParity()
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(LetBindingsDialect);

        const string code = """
                            let base = price * quantity
                            let result = base + fee
                            result
                            """;

        var compiler = host.GetArtifactCompiler<System.Reflection.Emit.DynamicMethod>("compiler").Compile(code, CreateDeclaredBindings()).CreateSession();
        var interpreter = host.GetArtifactCompiler<IntermediateRepresentationAbstractions.IAbstractIR>("interpreter").Compile(code, CreateDeclaredBindings()).CreateSession();

        SetArguments(compiler);
        SetArguments(interpreter);

        var compilerResult = BackendExecutionResult.Success(compiler.Run());
        var interpreterResult = BackendExecutionResult.Success(interpreter.Run());

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);

        Assert.That(BackendParityInfrastructure.AsNumber(compilerResult.Value), Is.EqualTo(32d).Within(1e-9));
    }

    [Test]
    public void FeatureProjection_ReportsLetBindingsWhenVariablesAndScopesSelected()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(LetBindingsDialect, "let-bindings-inline");

        Assert.That(composition.IsSuccess, Is.True, DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        ILanguageFeatureCatalog catalog = new WistLanguageFeatureCatalog();
        var explanation = new DialectFeatureExplanationProjector(catalog).Project(composition);

        Assert.Multiple(() =>
        {
            Assert.That(explanation.AvailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Contain(WistLanguageFeatureIds.LetBindings.Value));
            Assert.That(explanation.UnavailableFeatures.Select(static x => x.Descriptor.FeatureId.Value), Does.Not.Contain(WistLanguageFeatureIds.LetBindings.Value));
        });
    }

    private static void AssertBindingFailure(
        BackendExecutionResult compilerResult,
        BackendExecutionResult interpreterResult,
        string diagnosticCode,
        string messageFragment)
    {
        Assert.Multiple(() =>
        {
            Assert.That(compilerResult.IsSuccess, Is.False);
            Assert.That(interpreterResult.IsSuccess, Is.False);
            Assert.That(compilerResult.Exception!.Message, Does.Contain(diagnosticCode).And.Contain(messageFragment).IgnoreCase);
            Assert.That(interpreterResult.Exception!.Message, Does.Contain(diagnosticCode).And.Contain(messageFragment).IgnoreCase);
        });
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings()
    {
        return new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["quantity"] = typeof(double),
            ["fee"] = typeof(double),
            ["discount"] = typeof(double),
            ["maxDiscount"] = typeof(double)
        };
    }

    private static void SetArguments(BasicCore.Execution.ICompiledArtifactSession session)
    {
        session.SetArgument("price", 10d);
        session.SetArgument("quantity", 3d);
        session.SetArgument("fee", 2d);
        session.SetArgument("discount", 0.5d);
        session.SetArgument("maxDiscount", 5d);
    }

    private static (object? CompilerResult, object? InterpreterResult) ExecuteWithDeclaredBindings(string code)
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(LetBindingsDialect);

        var compiler = host.GetArtifactCompiler<System.Reflection.Emit.DynamicMethod>("compiler").Compile(code, CreateDeclaredBindings()).CreateSession();
        var interpreter = host.GetArtifactCompiler<IntermediateRepresentationAbstractions.IAbstractIR>("interpreter").Compile(code, CreateDeclaredBindings()).CreateSession();

        SetArguments(compiler);
        SetArguments(interpreter);

        var compilerResult = compiler.Run();
        var interpreterResult = interpreter.Run();

        BackendParityInfrastructure.AssertSemanticParity(
            BackendExecutionResult.Success(compilerResult),
            BackendExecutionResult.Success(interpreterResult));
        return (compilerResult, interpreterResult);
    }

    private static (BackendExecutionResult CompilerResult, BackendExecutionResult InterpreterResult) TryCompileInBothBackends(string code)
    {
        using var host = DialectTestHostInfrastructure.CreateHostFromDialectText(LetBindingsDialect);

        var compilerResult = BackendParityInfrastructure.ExecuteSafely(() =>
            host.GetArtifactCompiler<System.Reflection.Emit.DynamicMethod>("compiler").Compile(code, CreateDeclaredBindings()));
        var interpreterResult = BackendParityInfrastructure.ExecuteSafely(() =>
            host.GetArtifactCompiler<IntermediateRepresentationAbstractions.IAbstractIR>("interpreter").Compile(code, CreateDeclaredBindings()));

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return (compilerResult, interpreterResult);
    }
}
