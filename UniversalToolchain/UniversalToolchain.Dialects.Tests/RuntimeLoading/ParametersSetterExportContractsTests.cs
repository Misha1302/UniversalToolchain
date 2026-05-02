using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests.RuntimeLoading;

public class ParametersSetterExportContractsTests
{
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