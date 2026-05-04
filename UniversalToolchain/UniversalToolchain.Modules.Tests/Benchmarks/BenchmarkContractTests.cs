using System.Reflection;
using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using UniversalToolchain.Benchmarks.ExternalExecutionBenchmarks;

namespace UniversalToolchain.Modules.Tests.Benchmarks;

[TestFixture]
public sealed class BenchmarkContractTests
{
    private static readonly Type[] HotExecutionBenchmarkTypes =
    [
        typeof(ExternalSimple3ExecutionBenchmarks),
        typeof(ExternalMedium8ExecutionBenchmarks),
        typeof(ExternalDeepChain6ExecutionBenchmarks),
        typeof(ExternalRepeatedSubexpressions5ExecutionBenchmarks),
        typeof(ExternalConstantsHeavy6ExecutionBenchmarks),
        typeof(ExternalWideExpression10ExecutionBenchmarks)
    ];

    [Test]
    public void HotExecutionBenchmarks_ShouldUseInnerCountAsOperationsPerInvoke()
    {
        foreach (var benchmarkType in HotExecutionBenchmarkTypes)
        {
            var innerCount = GetInnerCount(benchmarkType);

            foreach (var benchmarkMethod in GetBenchmarkMethods(benchmarkType))
            {
                var benchmark = benchmarkMethod.GetCustomAttribute<BenchmarkAttribute>()!;

                Assert.That(
                    benchmark.OperationsPerInvoke,
                    Is.EqualTo(innerCount),
                    $"{benchmarkType.Name}.{benchmarkMethod.Name} must report one logical expression evaluation as one operation.");
            }
        }
    }

    [Test]
    public void HotExecutionBenchmarks_ShouldUsePreparedArtifactNames()
    {
        foreach (var benchmarkType in HotExecutionBenchmarkTypes)
        {
            var names = GetBenchmarkMethods(benchmarkType)
                .Select(static method => method.Name)
                .ToArray();

            Assert.That(names, Does.Contain("CSharp_NoInliningMethod"));
            Assert.That(names, Does.Contain("DynamicExpresso_CompiledDelegate"));
            Assert.That(names, Does.Contain("NCalc_CompiledLambda"));
            Assert.That(names, Does.Contain("Wist_Cil_DynamicMethodFastInvoker"));
        }
    }

    [Test]
    public void HotExecutionBenchmarks_ShouldHaveExactlyOneBaseline()
    {
        foreach (var benchmarkType in HotExecutionBenchmarkTypes)
        {
            var baselines = GetBenchmarkMethods(benchmarkType)
                .Where(static method => method.GetCustomAttribute<BenchmarkAttribute>()?.Baseline == true)
                .ToArray();

            Assert.That(
                baselines,
                Has.Length.EqualTo(1),
                $"{benchmarkType.Name} must have exactly one baseline benchmark method.");
        }
    }

    [Test]
    public void ExperimentalUnrolledBenchmarks_ShouldNotBeCompiledIntoPublicAssembly()
    {
        var benchmarkAssembly = typeof(ExternalSimple3ExecutionBenchmarks).Assembly;
        var compiledUnrolledTypes = benchmarkAssembly
            .GetTypes()
            .Where(static type => type.FullName?.Contains(".Unrolled.", StringComparison.Ordinal) == true)
            .Where(static type => GetBenchmarkMethods(type).Length > 0)
            .Select(static type => type.FullName)
            .ToArray();

        Assert.That(
            compiledUnrolledTypes,
            Is.Empty,
            "Generated unrolled benchmarks must stay out of the public benchmark assembly until they have a separate experimental contract.");
    }

    [Test]
    public void CompilationAndEndToEndBenchmarks_ShouldNotBePartOfExecutionSpeedSuite()
    {
        var benchmarkAssembly = typeof(ExternalSimple3ExecutionBenchmarks).Assembly;
        var nonExecutionBenchmarkTypes = benchmarkAssembly
            .GetTypes()
            .Where(static type => type.Namespace is not null)
            .Where(static type => type.Namespace!.Contains(".Compilation", StringComparison.Ordinal) || type.Namespace!.Contains(".EndToEnd", StringComparison.Ordinal))
            .Where(static type => GetBenchmarkMethods(type).Length > 0)
            .Select(static type => type.FullName)
            .ToArray();

        Assert.That(
            nonExecutionBenchmarkTypes,
            Is.Empty,
            "The public benchmark suite must measure execution speed of precompiled artifacts only.");
    }

    [Test]
    public void DataSize_ShouldRemainPowerOfTwo()
    {
        var dataSizeField = typeof(ExternalArithmeticExecutionBenchmarkEnvironmentBase).GetField(
            "DataSize",
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

        Assert.That(dataSizeField, Is.Not.Null);

        var dataSize = (int)dataSizeField!.GetRawConstantValue()!;

        Assert.That(dataSize, Is.EqualTo(4096));
        Assert.That(IsPowerOfTwo(dataSize), Is.True);
    }

    private static MethodInfo[] GetBenchmarkMethods(Type benchmarkType)
    {
        return benchmarkType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(static method => method.GetCustomAttribute<BenchmarkAttribute>() is not null)
            .ToArray();
    }

    private static int GetInnerCount(Type benchmarkType)
    {
        var innerCountField = benchmarkType.GetField(
            "InnerCount",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(innerCountField, Is.Not.Null, $"{benchmarkType.Name} must declare InnerCount.");

        return (int)innerCountField!.GetRawConstantValue()!;
    }

    private static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & value - 1) == 0;
    }
}
