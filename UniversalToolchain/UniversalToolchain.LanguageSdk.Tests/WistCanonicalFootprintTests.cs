using UniversalToolchain.FeatureSdk;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistCanonicalFootprintTests
{
    private static readonly string[] UnselectedMinimalAssemblies =
    [
        "CommentsModule",
        "CSharpInteropModule",
        "LabelsModule",
        "LoopsModule",
        "ParametersSetterModule",
        "SafeMathFunctionsModule"
    ];

    [Test]
    public void MinimalArithmeticRuntime_DoesNotLoadUnselectedFeatureAssemblies()
    {
        var before = SnapshotAssemblyPresence(UnselectedMinimalAssemblies);
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();

        using var runtime = LanguageRuntime.Create(
            plan,
            new LanguageRuntimeProviderRegistry().AddProvider(new WistLanguageRuntimeProvider()));
        var value = runtime.Run(new LanguageExecutionRequest("2 + 3", new("interpreter"))).Value;
        var after = SnapshotAssemblyPresence(UnselectedMinimalAssemblies);

        Assert.Multiple(() =>
        {
            Assert.That(value?.ToString(), Is.EqualTo("5"));
            foreach (var assemblyName in UnselectedMinimalAssemblies)
            {
                Assert.That(
                    after[assemblyName],
                    Is.EqualTo(before[assemblyName]),
                    $"Minimal arithmetic runtime unexpectedly loaded unselected assembly '{assemblyName}'.");
            }
        });
    }

    [Test]
    public void MinimalArithmeticPlan_ResolvesImplementationTypesOnlyForSelectedModules()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();

        var selectedAssemblies = WistRuntimeComponentCatalog.GetSelectedImplementationTypes(plan)
            .Select(static type => type.Assembly.GetName().Name)
            .Where(static name => name != null)
            .Select(static name => name!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.That(
            selectedAssemblies,
            Is.EquivalentTo(new[]
            {
                "ArithmeticModule",
                "NumbersModule",
                "ScopesModule",
                "WhitespacesModule"
            }));
    }

    private static IReadOnlyDictionary<string, bool> SnapshotAssemblyPresence(IEnumerable<string> assemblyNames)
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Select(static assembly => assembly.GetName().Name)
            .Where(static name => name != null)
            .Select(static name => name!)
            .ToHashSet(StringComparer.Ordinal);

        return assemblyNames.ToDictionary(
            static name => name,
            loaded.Contains,
            StringComparer.Ordinal);
    }
}
