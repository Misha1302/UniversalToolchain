using System.Reflection.Emit;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;
using Tests.Infrastructure;

namespace Tests.Integration;

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
            ["price"] = typeof(RealNumberImpl),
            ["fee"] = typeof(RealNumberImpl)
        };

        var result = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", new RealNumberImpl(10.0)),
            new NamedArgument("fee", new RealNumberImpl(2.0))
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
            ["price"] = typeof(RealNumberImpl),
            ["fee"] = typeof(RealNumberImpl)
        };

        var result = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", new RealNumberImpl(100.0)),
            new NamedArgument("fee", new RealNumberImpl(2.5))
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
            ["price"] = typeof(RealNumberImpl),
            ["fee"] = typeof(RealNumberImpl),
            ["unusedAlpha"] = typeof(RealNumberImpl),
            ["unusedBeta"] = typeof(string)
        };

        var first = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", new RealNumberImpl(5.0)),
            new NamedArgument("fee", new RealNumberImpl(1.5)),
            new NamedArgument("unusedAlpha", new RealNumberImpl(999.0)),
            new NamedArgument("unusedBeta", "ignored")
        ]);

        var second = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", new RealNumberImpl(5.0)),
            new NamedArgument("fee", new RealNumberImpl(1.5)),
            new NamedArgument("unusedAlpha", new RealNumberImpl(-321.0)),
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
            ["price"] = typeof(RealNumberImpl),
            ["fee"] = typeof(RealNumberImpl)
        };
        var declaredFeeThenPrice = new OrderedDictionary<string, Type>
        {
            ["fee"] = typeof(RealNumberImpl),
            ["price"] = typeof(RealNumberImpl)
        };

        var ordered = RunWithBindingsInBothBackends(code, declaredPriceThenFee, [
            new NamedArgument("price", new RealNumberImpl(7.0)),
            new NamedArgument("fee", new RealNumberImpl(0.75))
        ]);

        var reordered = RunWithBindingsInBothBackends(code, declaredFeeThenPrice, [
            new NamedArgument("price", new RealNumberImpl(7.0)),
            new NamedArgument("fee", new RealNumberImpl(0.75))
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
            ["price"] = typeof(RealNumberImpl),
            ["fee"] = typeof(RealNumberImpl)
        };
        var arguments = new[]
        {
            new NamedArgument("price", new RealNumberImpl(10.0)),
            new NamedArgument("fee", new RealNumberImpl(1.0))
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
            ["price"] = typeof(RealNumberImpl),
            ["fee"] = typeof(RealNumberImpl)
        };

        var result = RunWithBindingsInBothBackends(code, declared, [
            new NamedArgument("price", new RealNumberImpl(100.0)),
            new NamedArgument("fee", new RealNumberImpl(2.5))
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
    public void UnknownVariableAccess_WhenStrictFailureExists_ShouldExposeMeaningfulError()
    {
        using var host = CreateHost();
        var compilerCore = GetCompilerCore(host);
        var declared = new OrderedDictionary<string, Type>
        {
            ["price"] = typeof(RealNumberImpl)
        };

        try
        {
            var artifact = compilerCore.Compile("unknown + price", declared);
            var session = artifact.CreateSession();
            session.SetArgument("price", new RealNumberImpl(2.0));
            _ = session.Run();
            Assert.Pass("Current runtime allows the scenario without strict unknown-variable failure.");
        }
        catch (Exception ex)
        {
            var message = ex.ToString();
            Assert.That(message, Does.Contain("unknown").IgnoreCase,
                "Strict unknown-variable failure must mention the unresolved variable name.");
        }
    }

    private static (double CompilerNumeric, double InterpreterNumeric) RunWithBindingsInBothBackends(
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<NamedArgument> arguments)
    {
        using var host = CreateHost();
        var compilerArtifact = GetCompilerCore(host).Compile(code, declared);
        var interpreterArtifact = GetInterpreterCore(host).Compile(code, declared);

        var compilerSession = compilerArtifact.CreateSession();
        var interpreterSession = interpreterArtifact.CreateSession();

        foreach (var argument in arguments)
        {
            compilerSession.SetArgument(argument.Name, argument.Value);
            interpreterSession.SetArgument(argument.Name, argument.Value);
        }

        var compilerResult = BackendExecutionResult.Success(compilerSession.Run() ?? Thrower.InvalidOpEx<object>("Compiler returned null result."));
        var interpreterResult = BackendExecutionResult.Success(interpreterSession.Run() ?? Thrower.InvalidOpEx<object>("Interpreter returned null result."));

        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return (BackendParityInfrastructure.AsNumber(compilerResult.Value), BackendParityInfrastructure.AsNumber(interpreterResult.Value));
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
            Assert.That(BackendParityInfrastructure.AsNumber(first.CompilerOutcome.Value),
                Is.EqualTo(BackendParityInfrastructure.AsNumber(second.CompilerOutcome.Value)).Within(1e-9));
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
        using var host = CreateHost();
        var compilerOutcome = TryRunSingleBackend(() => GetCompilerCore(host).Compile(code, declared), arguments);
        var interpreterOutcome = TryRunSingleBackend(() => GetInterpreterCore(host).Compile(code, declared), arguments);
        return (compilerOutcome, interpreterOutcome);
    }

    private static BackendExecutionResult TryRunSingleBackend<TCompilationOutput>(
        Func<ICompiledArtifact<TCompilationOutput>> artifactFactory,
        IReadOnlyList<NamedArgument> arguments)
    {
        return BackendParityInfrastructure.ExecuteSafely(() =>
        {
            var artifact = artifactFactory();
            var session = artifact.CreateSession();
            foreach (var argument in arguments)
                session.SetArgument(argument.Name, argument.Value);

            return session.Run() ?? Thrower.InvalidOpEx<object>("Backend returned null result.");
        });
    }

    private static WistDialectExecutionHost CreateHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect InterpreterBindingsParity
            use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
            backend compiler,interpreter
            """,
            "interpreter-bindings-parity-inline");

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    private static BasicCoreImpl<DynamicMethod> GetCompilerCore(WistDialectExecutionHost host) =>
        host.GetCore("compiler") as BasicCoreImpl<DynamicMethod>
        ?? Thrower.InvalidOpEx<BasicCoreImpl<DynamicMethod>>("Compiler core must be BasicCoreImpl<DynamicMethod>.");

    private static BasicCoreImpl<IAbstractIR> GetInterpreterCore(WistDialectExecutionHost host) =>
        host.GetCore("interpreter") as BasicCoreImpl<IAbstractIR>
        ?? Thrower.InvalidOpEx<BasicCoreImpl<IAbstractIR>>("Interpreter core must be BasicCoreImpl<IAbstractIR>.");

    private sealed record NamedArgument(string Name, object Value);
}
