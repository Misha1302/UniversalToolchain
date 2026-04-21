using System.Reflection.Emit;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Modules.Tests;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class DeclaredBindingsExecutionContractTests
{
    private const string DeclaredBindingsDialectText = """
                                                       dialect DeclaredBindingsDialect
                                                       use Arithmetic,Identifier,Numbers,Variables,Whitespaces
                                                       backend compiler,interpreter
                                                       """;

    [Test]
    public void DeclaredBindings_DialectExecution_WorksForCompilerPath()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.GetArtifactCompiler<DynamicMethod>("compiler").Compile("left + right", CreateDeclaredBindings());
        var result = artifact.CreateSession().InvokeNamed<object>(CreateArguments(new RealNumberImpl(7), new RealNumberImpl(5)));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void DeclaredBindings_DialectExecution_WorksForInterpreterPath()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.GetArtifactCompiler<IAbstractIR>("interpreter").Compile("left + right", CreateDeclaredBindings());
        var result = artifact.CreateSession().InvokeNamed<object>(CreateArguments(new RealNumberImpl(7), new RealNumberImpl(5)));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void DeclaredBindings_DialectExecution_PreservesDeclaredBindingOrder()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.GetArtifactCompiler<DynamicMethod>("compiler").Compile("right - left", CreateDeclaredBindings());

        Assert.Multiple(() =>
        {
            Assert.That(artifact.DeclaredBindings.Select(x => x.Name), Is.EqualTo(new[] { "left", "right" }));
            Assert.That(artifact.SlotsByName["left"], Is.EqualTo(0));
            Assert.That(artifact.SlotsByName["right"], Is.EqualTo(1));
        });
    }

    [Test]
    public void DeclaredBindingsDialect_MissingIdentifierAndVariables_FailsDeterministically()
    {
        const string invalidDialectText = """
                                          dialect InvalidDeclaredBindingsDialect
                                          use Arithmetic,Numbers,Whitespaces
                                          backend compiler,interpreter
                                          """;

        using var host = ComposeHost(invalidDialectText);

        var first = CaptureFailure(() => host.GetArtifactCompiler<DynamicMethod>("compiler").Compile("left + right", CreateDeclaredBindings()));
        var second = CaptureFailure(() => host.GetArtifactCompiler<DynamicMethod>("compiler").Compile("left + right", CreateDeclaredBindings()));

        Assert.Multiple(() =>
        {
            Assert.That(first.GetType(), Is.EqualTo(second.GetType()));
            Assert.That(first.Message, Is.EqualTo(second.Message));
            Assert.That(first.Message, Is.Not.Empty);
        });
    }

    private static WistDialectExecutionHost ComposeHost(string dialectText)
    {
        using var provider = CreateWorkflowProviderWithCilAndInterpreter();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialectText, "declared-bindings-inline");
        if (!composition.IsSuccess)
            throw new InvalidOperationException(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
    }

    private static Exception CaptureFailure(TestDelegate action)
    {
        var exception = Assert.Catch(action);
        Assert.That(exception, Is.Not.Null);
        return exception!;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings()
    {
        var bindings = new OrderedDictionary<string, Type>
        {
            ["left"] = typeof(RealNumberImpl),
            ["right"] = typeof(RealNumberImpl)
        };
        return bindings;
    }

    private static IReadOnlyDictionary<string, object?> CreateArguments(RealNumberImpl left, RealNumberImpl right) =>
        new Dictionary<string, object?>
        {
            ["left"] = left,
            ["right"] = right
        };

    private static ServiceProvider CreateWorkflowProviderWithCilAndInterpreter()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }
}