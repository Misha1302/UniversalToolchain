using ParametersSetterModule;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class ParametersSetterExportContractsTests
{
    private const string ExpectedUnsupportedReason =
        "ParametersSetter is not exported yet: parser contracts and runtime binding semantics are not finalized.";

    [Test]
    public void ParametersSetter_IsNotExported_ToDialectRuntimeComposition()
    {
        var resolver = CreateResolverFromStandardCatalog();
        var plan = BuildPlan(["ParametersSetter"]);

        var selected = resolver.Resolve(plan);

        Assert.Multiple(() =>
        {
            Assert.That(selected.OrderedModules, Is.Empty);
            Assert.That(selected.Diagnostics.Any(static x => x.Code == "R001" && x.Message.Contains("ParametersSetter", StringComparison.Ordinal)), Is.True);
        });
    }

    [Test]
    public void ParametersSetter_PlaceholderUnsupportedReason_IsStable()
    {
        Assert.That(ParametersSetterModuleImpl.UnsupportedReason, Is.EqualTo(ExpectedUnsupportedReason));
    }

    [Test]
    public void ParametersSetter_DoesNotAccidentallyAppearInResolvedRuntimeCatalog()
    {
        var catalog = CreateStandardCatalog();

        var foundByAlias = catalog.TryResolveModule("ParametersSetter", out _);
        var appearsInDeterministicOrder = catalog
            .GetModulesInDeterministicOrder()
            .Any(static x => string.Equals(x.CanonicalAlias, "ParametersSetter", StringComparison.Ordinal)
                             || x.Aliases.Contains("ParametersSetter", StringComparer.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(foundByAlias, Is.False);
            Assert.That(appearsInDeterministicOrder, Is.False);
        });
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
}
