using System.Collections.Specialized;
using System.Reflection.Emit;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using UniversalToolchain.Dialects.Wist;

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
        var result = artifact.CreateSession().InvokeNamed<object>(CreateArguments(left: 7, right: 5));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void DeclaredBindings_DialectExecution_WorksForInterpreterPath()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.GetArtifactCompiler<IAbstractIR>("interpreter").Compile("left + right", CreateDeclaredBindings());
        var result = artifact.CreateSession().InvokeNamed<object>(CreateArguments(left: 7, right: 5));

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
        var provider = CreateWorkflowProviderWithCilAndInterpreter();
        try
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(dialectText, "declared-bindings-inline");
            if (!composition.IsSuccess)
                throw new InvalidOperationException(composition.ToDeterministicText());

            return workflow.CreateHost(composition);
        }
        finally
        {
            provider.Dispose();
        }
    }

    private static Exception CaptureFailure(TestDelegate action)
    {
        var exception = Assert.Throws<Exception>(action);
        Assert.That(exception, Is.Not.Null);
        return exception!;
    }

    private static OrderedDictionary<string, Type> CreateDeclaredBindings()
    {
        var bindings = new OrderedDictionary<string, Type>
        {
            ["left"] = typeof(double),
            ["right"] = typeof(double)
        };
        return bindings;
    }

    private static IReadOnlyDictionary<string, object?> CreateArguments(double left, double right)
    {
        return new Dictionary<string, object?>
        {
            ["left"] = left,
            ["right"] = right
        };
    }

    private static ServiceProvider CreateWorkflowProviderWithCilAndInterpreter()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }
}
