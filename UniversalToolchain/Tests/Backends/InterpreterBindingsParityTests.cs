using BasicCore.Binding;
using Tests.Infrastructure;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Testing.Infrastructure;

namespace Tests.Backends;

[TestFixture]
public class InterpreterBindingsParityTests
{
    [Test]
    public void ExternalBindings_ReadsMustWorkWithoutLocalContainerStorage()
    {
        const string code = """
                            price + fee
                            """;

        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

        var result = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", (10.0)),
            new NamedArgument("fee", (2.0))
        ]);

        Assert.That(result.CompilerNumeric, Is.EqualTo(result.InterpreterNumeric).Within(1e-9));
        Assert.That(result.CompilerNumeric, Is.EqualTo(12.0).Within(1e-9));
    }

    [Test]
    public void Reproducer_WithPriceFeeAndLocalLoopVariable_ShouldMatchCompilerInterpreterAndExpected()
    {
        const string code = """
                            let i = 0
                            i = i + 1
                            i = i + 1
                            i = i + 1
                            price + fee * i
                            """;
        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

        var result = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", (100.0)),
            new NamedArgument("fee", (2.5))
        ]);

        Assert.That(result.CompilerNumeric, Is.EqualTo(result.InterpreterNumeric).Within(1e-9));
        Assert.That(result.CompilerNumeric, Is.EqualTo(107.5).Within(1e-9));
    }

    [Test]
    public void ExtraUnusedDeclaredBindings_ShouldKeepStableParityAndResult()
    {
        const string code = """
                            let i = 0
                            i = i + 1
                            i = i + 1
                            price + fee * i
                            """;
        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double),
            ["unusedAlpha"] = typeof(double),
            ["unusedBeta"] = typeof(string)
        };

        var first = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", (5.0)),
            new NamedArgument("fee", (1.5)),
            new NamedArgument("unusedAlpha", (999.0)),
            new NamedArgument("unusedBeta", "ignored")
        ]);

        var second = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", (5.0)),
            new NamedArgument("fee", (1.5)),
            new NamedArgument("unusedAlpha", (-321.0)),
            new NamedArgument("unusedBeta", "still-ignored")
        ]);

        Assert.That(first.CompilerNumeric, Is.EqualTo(first.InterpreterNumeric).Within(1e-9));
        Assert.That(second.CompilerNumeric, Is.EqualTo(second.InterpreterNumeric).Within(1e-9));
        Assert.That(first.CompilerNumeric, Is.EqualTo(second.CompilerNumeric).Within(1e-9));
        Assert.That(first.CompilerNumeric, Is.EqualTo(8.0).Within(1e-9));
    }

    [Test]
    public void ReorderedDeclaredBindings_WithNamedSetArgument_ShouldRemainStable()
    {
        const string code = """
                            let i = 0
                            i = i + 1
                            i = i + 1
                            i = i + 1
                            i = i + 1
                            price + fee * i
                            """;

        var declaredPriceThenFee = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };
        var declaredFeeThenPrice = new OrderedDictionary<string, Type>
        {
            ["fee"] = typeof(double),
            ["price"] = typeof(double)
        };

        var ordered = RunWithBindingsInBothBackends(code, declaredPriceThenFee, [
            new NamedArgument("price", (7.0)),
            new NamedArgument("fee", (0.75))
        ]);

        var reordered = RunWithBindingsInBothBackends(code, declaredFeeThenPrice, [
            new NamedArgument("price", (7.0)),
            new NamedArgument("fee", (0.75))
        ]);

        Assert.That(ordered.CompilerNumeric, Is.EqualTo(ordered.InterpreterNumeric).Within(1e-9));
        Assert.That(reordered.CompilerNumeric, Is.EqualTo(reordered.InterpreterNumeric).Within(1e-9));
        Assert.That(ordered.CompilerNumeric, Is.EqualTo(reordered.CompilerNumeric).Within(1e-9));
        Assert.That(ordered.CompilerNumeric, Is.EqualTo(10.0).Within(1e-9));
    }

    [Test]
    public void ShadowingAndNestedScope_WithLocalNamesOverlappingExternals_ShouldBeDeterministicAndParityStable()
    {
        const string shadowingCode = """
                                     let price = fee
                                     price + fee
                                     """;
        const string nestedScopeCode = """
                                       let total = price
                                       if price == price (
                                           let fee = price
                                           total = total + fee
                                       )
                                       total + fee
                                       """;
        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };
        var arguments = new[]
        {
            new NamedArgument("price", (10.0)),
            new NamedArgument("fee", (1.0))
        };

        AssertDeterministicParity(shadowingCode, declared, arguments);
        AssertDeterministicParity(nestedScopeCode, declared, arguments);
    }


    [Test]
    public void LocalVariable_TypeMustStayStableAcrossRepeatedReadWrite()
    {
        const string code = """
                            let i = 0
                            i = i + 1
                            i = i + 1
                            i
                            """;

        var declared = new OrderedDictionary<string, Type>();
        var result = RunWithBindingsInBothBackends(code, declared, []);

        Assert.That(result.CompilerNumeric, Is.EqualTo(result.InterpreterNumeric).Within(1e-9));
        Assert.That(result.CompilerNumeric, Is.EqualTo(2.0).Within(1e-9));
    }

    [Test]
    public void LocalVariable_WithExternalArithmetic_MustNotSwitchStorageContainer()
    {
        const string code = """
                            let i = 0
                            i = i + 1
                            price + fee * i
                            """;

        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double),
            ["fee"] = typeof(double)
        };

        var result = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", (100.0)),
            new NamedArgument("fee", (2.5))
        ]);

        Assert.That(result.CompilerNumeric, Is.EqualTo(result.InterpreterNumeric).Within(1e-9));
        Assert.That(result.CompilerNumeric, Is.EqualTo(102.5).Within(1e-9));
    }

    [Test]
    public void LocalShadowing_MustUseIndependentStorageKeys()
    {
        const string code = """
                            let fee = 1
                            let total = fee
                            total + fee
                            """;

        var declared = new OrderedDictionary<string, Type>();
        var result = RunWithBindingsInBothBackends(code, declared, []);

        Assert.That(result.CompilerNumeric, Is.EqualTo(result.InterpreterNumeric).Within(1e-9));
        Assert.That(result.CompilerNumeric, Is.EqualTo(2.0).Within(1e-9));
    }

    [Test]
    public void UnknownVariableAccess_FailsClosedWithDeterministicIdentifierDiagnostic()
    {
        using var host = CreateHost();
        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(double)
        };
        var arguments = new List<KeyValuePair<string, object>>
        {
            new("price", (2.0))
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ParityBackendExecutionAdapter.RunCompiled(host, "cil", "unknown + price", declared, arguments));

        Assert.That(exception!.ToString(), Does.Contain("Unknown identifier 'unknown'"));
    }

    [Test]
    public void LocalVariableStorageKeys_AreStableAcrossIndependentBindingContexts()
    {
        var first = new BindingContext([]);
        var second = new BindingContext([]);

        var firstKeys = new[]
        {
            first.DeclareLocal("x", typeof(int)).StorageKey,
            first.DeclareLocal("y", typeof(double)).StorageKey
        };
        var secondKeys = new[]
        {
            second.DeclareLocal("x", typeof(int)).StorageKey,
            second.DeclareLocal("y", typeof(double)).StorageKey
        };

        Assert.Multiple(() =>
        {
            Assert.That(firstKeys, Is.EqualTo(secondKeys));
            Assert.That(firstKeys, Is.EqualTo(new[]
            {
                "local:00000000:x",
                "local:00000001:y"
            }));
            Assert.That(firstKeys, Has.None.Contains("-"));
        });
    }

    private static (double CompilerNumeric, double InterpreterNumeric) RunWithBindingsInBothBackends(
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<NamedArgument> arguments)
    {
        var mappedArguments = MapArguments(arguments);
        var compilerResult = BackendExecutionResult.Success(RunCompiled("cil", code, declared, mappedArguments));
        var interpreterResult = BackendExecutionResult.Success(RunCompiled("interpreter", code, declared, mappedArguments));

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return (BackendValueNormalizer.ConvertTo<double>(compilerResult.Value), BackendValueNormalizer.ConvertTo<double>(interpreterResult.Value));
    }

    private static void AssertDeterministicParity(string code, OrderedDictionary<string, Type> declared, IReadOnlyList<NamedArgument> arguments)
    {
        var first = TryRunWithBindingsInBothBackends(code, declared, arguments);
        var second = TryRunWithBindingsInBothBackends(code, declared, arguments);

        BackendParityInfrastructure.AssertSemanticParity(first.CompilerOutcome, first.InterpreterOutcome);
        BackendParityInfrastructure.AssertSemanticParity(second.CompilerOutcome, second.InterpreterOutcome);

        Assert.That(first.CompilerOutcome.IsSuccess, Is.EqualTo(second.CompilerOutcome.IsSuccess));

        if (first.CompilerOutcome.IsSuccess)
        {
            Assert.That(BackendValueNormalizer.ConvertTo<double>(first.CompilerOutcome.Value),
                Is.EqualTo(BackendValueNormalizer.ConvertTo<double>(second.CompilerOutcome.Value)).Within(1e-9));
            return;
        }

        Assert.That(first.CompilerOutcome.Exception?.GetType().FullName,
            Is.EqualTo(second.CompilerOutcome.Exception?.GetType().FullName));
        Assert.That(first.CompilerOutcome.Exception?.Message, Is.EqualTo(second.CompilerOutcome.Exception?.Message));
    }

    private static (BackendExecutionResult CompilerOutcome, BackendExecutionResult InterpreterOutcome) TryRunWithBindingsInBothBackends(
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<NamedArgument> arguments)
    {
        var mappedArguments = MapArguments(arguments);
        var compilerOutcome = TryRunSingleBackend(() => RunCompiled("cil", code, declared, mappedArguments));
        var interpreterOutcome = TryRunSingleBackend(() => RunCompiled("interpreter", code, declared, mappedArguments));
        return (compilerOutcome, interpreterOutcome);
    }

    private static object RunCompiled(
        string backend,
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<KeyValuePair<string, object>> arguments)
    {
        using var host = CreateHost();
        return ParityBackendExecutionAdapter.RunCompiled(host, backend, code, declared, arguments);
    }

    private static KeyValuePair<string, object>[] MapArguments(IReadOnlyList<NamedArgument> arguments)
        => arguments
            .Select(static argument => new KeyValuePair<string, object>(argument.Name, argument.Value))
            .ToArray();

    private static BackendExecutionResult TryRunSingleBackend(Func<object> backendRunner) => BackendParityInfrastructure.ExecuteSafely(backendRunner);

    private static WistDialectExecutionHost CreateHost() => RuntimeCompiledArtifactTestFactory.CreateHost();

    private sealed record NamedArgument(string Name, object Value);
}