using BenchmarkDotNet.Attributes;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Benchmarks;

/// <summary>
/// Migration-only boundary benchmarks. Keep these separate from hot invocation benchmarks:
/// they measure architecture boundaries that must not accidentally move onto the per-call path.
/// </summary>
[MemoryDiagnoser]
public class MigrationArchitectureBoundaryBenchmarks
{
    private LanguageCompiler? _compiler;
    private LanguageDefinition? _definition;

    [GlobalSetup]
    public void Setup()
    {
        var package = new WistLanguageFeaturePackage();
        _compiler = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package));
        _definition = LanguageDefinitionBuilder.Create("Wist.Migration.Benchmark", "1")
            .UseFeature(WistFeatureIds.Arithmetic)
            .EnableBackend(new BackendId("cil"))
            .UseRuntimeProvider(WistLanguageFeaturePackage.RuntimeProviderId, WistLanguageFeaturePackage.PackageVersion)
            .Build();
    }

    [Benchmark]
    public int WistEngine_CreateAndDispose()
    {
        using var engine = WistEngine.CreateRestrictedArithmetic();
        return 1;
    }

    [Benchmark]
    public LanguagePlan LanguagePlan_Compile() =>
        Compiler.Compile(Definition).GetRequiredPlan();

    private LanguageCompiler Compiler => _compiler ?? throw new InvalidOperationException("Compiler is not initialized.");

    private LanguageDefinition Definition => _definition ?? throw new InvalidOperationException("Definition is not initialized.");
}
