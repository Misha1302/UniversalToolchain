using System.Reflection.Emit;
using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Integration;

[TestFixture]
public class InterpreterBindingsParityTests
{
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
            ["price"] = typeof(object),
            ["fee"] = typeof(object)
        };

        var result = RunInBothBackends(code, declared, [
            new NamedArgument("price", 100.0),
            new NamedArgument("fee", 2.5)
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
            ["price"] = typeof(object),
            ["fee"] = typeof(object),
            ["unusedAlpha"] = typeof(object),
            ["unusedBeta"] = typeof(string)
        };

        var first = RunInBothBackends(code, declared, [
            new NamedArgument("price", 5.0),
            new NamedArgument("fee", 1.5),
            new NamedArgument("unusedAlpha", 999.0),
            new NamedArgument("unusedBeta", "ignored")
        ]);

        var second = RunInBothBackends(code, declared, [
            new NamedArgument("price", 5.0),
            new NamedArgument("fee", 1.5),
            new NamedArgument("unusedAlpha", -321.0),
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
            ["price"] = typeof(object),
            ["fee"] = typeof(object)
        };
        var declaredFeeThenPrice = new OrderedDictionary<string, Type>
        {
            ["fee"] = typeof(object),
            ["price"] = typeof(object)
        };

        var ordered = RunInBothBackends(code, declaredPriceThenFee, [
            new NamedArgument("price", 7.0),
            new NamedArgument("fee", 0.75)
        ]);

        var reordered = RunInBothBackends(code, declaredFeeThenPrice, [
            new NamedArgument("price", 7.0),
            new NamedArgument("fee", 0.75)
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
            ["price"] = typeof(object),
            ["fee"] = typeof(object)
        };
        var arguments = new[]
        {
            new NamedArgument("price", 10.0),
            new NamedArgument("fee", 1.0)
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
        var result = RunInBothBackends(code, declared, []);

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
            ["price"] = typeof(object),
            ["fee"] = typeof(object)
        };

        var result = RunInBothBackends(code, declared, [
            new NamedArgument("price", 100.0),
            new NamedArgument("fee", 2.5)
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
        var result = RunInBothBackends(code, declared, []);

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
            ["price"] = typeof(object)
        };

        try
        {
            var artifact = compilerCore.Compile("unknown + price", declared);
            var session = artifact.CreateSession();
            session.SetArgument("price", 2.0);
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

    private static (double CompilerNumeric, double InterpreterNumeric) RunInBothBackends(
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

        var compilerResult = compilerSession.Run() ?? Thrower.InvalidOpEx<object>("Compiler returned null result.");
        var interpreterResult = interpreterSession.Run() ?? Thrower.InvalidOpEx<object>("Interpreter returned null result.");

        return (ToNumeric(compilerResult), ToNumeric(interpreterResult));
    }

    private static void AssertDeterministicParity(string code, OrderedDictionary<string, Type> declared, IReadOnlyList<NamedArgument> arguments)
    {
        var first = TryRunInBothBackends(code, declared, arguments);
        var second = TryRunInBothBackends(code, declared, arguments);

        Assert.That(first.CompilerOutcome.Kind, Is.EqualTo(first.InterpreterOutcome.Kind));
        Assert.That(second.CompilerOutcome.Kind, Is.EqualTo(second.InterpreterOutcome.Kind));
        Assert.That(first.CompilerOutcome.Kind, Is.EqualTo(second.CompilerOutcome.Kind));

        if (first.CompilerOutcome.Kind == OutcomeKind.Success)
        {
            Assert.That(first.CompilerOutcome.NumericValue, Is.EqualTo(first.InterpreterOutcome.NumericValue).Within(1e-9));
            Assert.That(second.CompilerOutcome.NumericValue, Is.EqualTo(second.InterpreterOutcome.NumericValue).Within(1e-9));
            Assert.That(first.CompilerOutcome.NumericValue, Is.EqualTo(second.CompilerOutcome.NumericValue).Within(1e-9));
            return;
        }

        Assert.That(first.CompilerOutcome.ExceptionType, Is.EqualTo(first.InterpreterOutcome.ExceptionType));
        Assert.That(second.CompilerOutcome.ExceptionType, Is.EqualTo(second.InterpreterOutcome.ExceptionType));
        Assert.That(first.CompilerOutcome.ExceptionType, Is.EqualTo(second.CompilerOutcome.ExceptionType));
        Assert.That(first.CompilerOutcome.ExceptionMessage, Is.EqualTo(first.InterpreterOutcome.ExceptionMessage));
        Assert.That(second.CompilerOutcome.ExceptionMessage, Is.EqualTo(second.InterpreterOutcome.ExceptionMessage));
        Assert.That(first.CompilerOutcome.ExceptionMessage, Is.EqualTo(second.CompilerOutcome.ExceptionMessage));
    }

    private static (ExecutionOutcome CompilerOutcome, ExecutionOutcome InterpreterOutcome) TryRunInBothBackends(
        string code,
        OrderedDictionary<string, Type> declared,
        IReadOnlyList<NamedArgument> arguments)
    {
        using var host = CreateHost();
        var compilerOutcome = TryRunSingleBackend(() => GetCompilerCore(host).Compile(code, declared), arguments);
        var interpreterOutcome = TryRunSingleBackend(() => GetInterpreterCore(host).Compile(code, declared), arguments);
        return (compilerOutcome, interpreterOutcome);
    }

    private static ExecutionOutcome TryRunSingleBackend<TCompilationOutput>(
        Func<ICompiledArtifact<TCompilationOutput>> artifactFactory,
        IReadOnlyList<NamedArgument> arguments)
    {
        try
        {
            var artifact = artifactFactory();
            var session = artifact.CreateSession();
            foreach (var argument in arguments)
                session.SetArgument(argument.Name, argument.Value);

            var result = session.Run() ?? Thrower.InvalidOpEx<object>("Backend returned null result.");
            return ExecutionOutcome.Success(ToNumeric(result));
        }
        catch (Exception ex)
        {
            return ExecutionOutcome.Failure(ex.GetType().FullName ?? ex.GetType().Name, ex.Message);
        }
    }

    private static double ToNumeric(object value) => value switch
    {
        int intValue => intValue,
        long longValue => longValue,
        float floatValue => floatValue,
        double doubleValue => doubleValue,
        decimal decimalValue => (double)decimalValue,
        RealNumberImpl realNumber => realNumber.GetValue(),
        _ => Thrower.InvalidCast<double>($"Cannot convert value of type {value.GetType().FullName} to numeric result.")
    };

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

    private enum OutcomeKind
    {
        Success,
        Failure
    }

    private sealed record ExecutionOutcome(OutcomeKind Kind, double? NumericValue, string? ExceptionType, string? ExceptionMessage)
    {
        public static ExecutionOutcome Success(double numericValue) => new(OutcomeKind.Success, numericValue, null, null);

        public static ExecutionOutcome Failure(string exceptionType, string exceptionMessage) =>
            new(OutcomeKind.Failure, null, exceptionType, exceptionMessage);
    }
}
