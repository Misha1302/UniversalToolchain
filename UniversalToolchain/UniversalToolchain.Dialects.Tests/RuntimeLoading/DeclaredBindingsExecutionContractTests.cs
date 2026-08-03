using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Testing.Infrastructure;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class DeclaredBindingsExecutionContractTests
{
    private const string DeclaredBindingsDialectText = """
                                                       dialect DeclaredBindingsDialect
                                                       use Arithmetic,Identifier,Numbers,Scopes,Variables,Whitespaces
                                                       backend cil,interpreter
                                                       """;

    [Test]
    public void DeclaredBindings_DialectExecution_WorksForCompilerPath()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.Compile("left + right", CreateDeclaredBindings(), "cil");
        var result = host.Run(artifact, CreateArguments(new RealNumberImpl(7), new RealNumberImpl(5)));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void DeclaredBindings_DialectExecution_WorksForInterpreterPath()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.Compile("left + right", CreateDeclaredBindings(), "interpreter");
        var result = host.Run(artifact, CreateArguments(new RealNumberImpl(7), new RealNumberImpl(5)));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void DeclaredBindings_DialectExecution_PreservesDeclaredBindingOrder()
    {
        using var host = ComposeHost(DeclaredBindingsDialectText);

        var artifact = host.Compile("right - left", CreateDeclaredBindings(), "cil");

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
                                          backend cil,interpreter
                                          """;

        using var host = ComposeHost(invalidDialectText);

        var first = CaptureCompilerFailure(host);
        var second = CaptureCompilerFailure(host);

        Assert.Multiple(() =>
        {
            Assert.That(first.GetType(), Is.EqualTo(second.GetType()));
            Assert.That(first.Message, Is.EqualTo(second.Message));
            Assert.That(first.Message, Is.Not.Empty);
        });
    }

    private static WistDialectExecutionHost ComposeHost(string dialectText)
    {
        ServiceProvider? provider = CreateWorkflowProviderWithCilAndInterpreter();
        try
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(dialectText, "declared-bindings-inline");
            if (!composition.IsSuccess)
                throw new InvalidOperationException(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

            var owner = provider;
            provider = null;
            return workflow.CreateHost(composition, new WistRuntimeServiceOptions(), owner);
        }
        finally
        {
            provider?.Dispose();
        }
    }

    private static Exception CaptureCompilerFailure(WistDialectExecutionHost host)
    {
        try
        {
            _ = host.Compile("left + right", CreateDeclaredBindings(), "cil");
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new AssertionException("Expected compiler path to fail.");
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
