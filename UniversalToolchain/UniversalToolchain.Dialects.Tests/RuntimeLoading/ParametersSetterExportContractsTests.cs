using System.Collections.Specialized;
using System.Reflection.Emit;
using BasicCore.Compilation;
using BasicCore.Execution;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class ParametersSetterExportContractsTests
{
    private const string ParametersDialectText = """
                                                 dialect ParametersDialect
                                                 use Arithmetic,Identifier,Variables,Numbers,ParametersSetter,Whitespaces
                                                 backend compiler,interpreter
                                                 """;

    [Test]
    public void ParametersSetter_IsExportedThroughDialectComposition()
    {
        var resolver = CreateResolverFromStandardCatalog();
        var plan = BuildPlan(["ParametersSetter"]);

        var selected = resolver.Resolve(plan);

        Assert.Multiple(() =>
        {
            Assert.That(selected.IsResolved, Is.True, string.Join(Environment.NewLine, selected.Diagnostics.Select(x => x.Message)));
            Assert.That(selected.OrderedModules.Select(x => x.CanonicalAlias), Is.EqualTo(new[] { "ParametersSetter" }));
        });
    }

    [Test]
    public void ParametersSetter_DialectExport_WorksForCompilerPath()
    {
        using var host = ComposeHost(ParametersDialectText);

        var artifact = host.GetArtifactCompiler<DynamicMethod>("compiler").Compile("left + right", CreateDeclaredBindings());
        var result = artifact.CreateSession().InvokeNamed<object>(CreateArguments(left: 7, right: 5));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void ParametersSetter_DialectExport_WorksForInterpreterPath()
    {
        using var host = ComposeHost(ParametersDialectText);

        var artifact = host.GetArtifactCompiler<IAbstractIR>("interpreter").Compile("left + right", CreateDeclaredBindings());
        var result = artifact.CreateSession().InvokeNamed<object>(CreateArguments(left: 7, right: 5));

        Assert.That(BackendParityInfrastructure.AsNumber(result), Is.EqualTo(12d).Within(1e-9));
    }

    [Test]
    public void ParametersSetter_DialectExport_PreservesDeclaredBindingOrder()
    {
        using var host = ComposeHost(ParametersDialectText);

        var artifact = host.GetArtifactCompiler<DynamicMethod>("compiler").Compile("right - left", CreateDeclaredBindings());

        Assert.Multiple(() =>
        {
            Assert.That(artifact.DeclaredBindings.Select(x => x.Name), Is.EqualTo(new[] { "left", "right" }));
            Assert.That(artifact.SlotsByName["left"], Is.EqualTo(0));
            Assert.That(artifact.SlotsByName["right"], Is.EqualTo(1));
        });
    }

    [Test]
    public void ParametersSetter_DialectExport_InvalidConfiguration_FailsDeterministically()
    {
        const string invalidDialectText = """
                                          dialect InvalidParametersDialect
                                          use Arithmetic,Numbers,ParametersSetter,Whitespaces
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

    [Test]
    public void ParametersSetter_AppearsInResolvedRuntimeCatalog()
    {
        var catalog = CreateStandardCatalog();

        var foundByAlias = catalog.TryResolveModule("ParametersSetter", out var entry);
        var appearsInDeterministicOrder = catalog
            .GetModulesInDeterministicOrder()
            .Any(static x => string.Equals(x.CanonicalAlias, "ParametersSetter", StringComparison.Ordinal)
                             || x.Aliases.Contains("ParametersSetter", StringComparer.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(foundByAlias, Is.True);
            Assert.That(entry, Is.Not.Null);
            Assert.That(entry!.CanonicalAlias, Is.EqualTo("ParametersSetter"));
            Assert.That(appearsInDeterministicOrder, Is.True);
        });
    }

    private static WistDialectExecutionHost ComposeHost(string dialectText)
    {
        var provider = CreateWorkflowProviderWithCilAndInterpreter();
        try
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(dialectText, "parameters-inline");
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
        var exception = Assert.Catch(action);
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

    private static DialectBuildPlan BuildPlan(IReadOnlyList<string> modules) =>
        new(
            "ContractDialect",
            null,
            modules,
            [],
            [],
            [],
            [],
            null,
            [],
            new DialectValidationResult([]));

    private static SelectedRuntimePlanResolver CreateResolverFromStandardCatalog() => new(CreateStandardCatalog());

    private static IRuntimeComponentCatalog CreateStandardCatalog() =>
        new FileBasedRuntimeComponentCatalog(
            new DefaultRuntimeManifestFileLocator(new RuntimeArtifactLocatorOptions()),
            new RuntimeManifestJsonSerializer());

    private static ServiceProvider CreateWorkflowProviderWithCilAndInterpreter()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }
}