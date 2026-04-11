using System.Collections.Specialized;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Core;

[TestFixture]
public class CompiledArtifactContractsTests
{
    [Test]
    public void Compile_And_PrepareToRun_ProduceEquivalentExecutionResult()
    {
        using var host = CreateHost();
        var compilerCore = GetCompilerCore(host);
        const string code = "(1 + 2) * 5";
        var declaredBindings = new OrderedDictionary<string, Type>();

        compilerCore.PrepareToRun(code, declaredBindings);
        var viaPrepared = compilerCore.RunPrepared();

        var artifact = compilerCore.Compile(code, declaredBindings);
        var viaCompiledSession = artifact.CreateSession().Run();

        Assert.That(viaCompiledSession, Is.EqualTo(viaPrepared));
    }

    [Test]
    public void Compile_And_GetExecutable_ProduceEquivalentCompilationOutput()
    {
        using var host = CreateHost();
        var compilerCore = GetCompilerCore(host);
        const string code = "41 + 1";

        var fromCompile = compilerCore.Compile(code).CompilationOutput;
        var fromGetExecutable = compilerCore.GetExecutable(code);

        var viaCompile = fromCompile.CreateDelegate<Func<int>>().Invoke();
        var viaGetExecutable = fromGetExecutable.CreateDelegate<Func<int>>().Invoke();

        Assert.That(viaGetExecutable, Is.EqualTo(viaCompile));
    }

    [Test]
    public void CompiledArtifact_Implements_Untyped_And_Typed_Interfaces()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");

        Assert.That(artifact, Is.InstanceOf<ICompiledArtifact>());
        Assert.That(artifact, Is.InstanceOf<ICompiledArtifact<string>>());
    }

    [Test]
    public void CommonCode_CanUseICompiledArtifact_WithoutCompilationOutputType()
    {
        var artifact = CreateTwoArgumentArtifact("compiled", 4, "seed");
        ICompiledArtifact untypedArtifact = artifact;

        var session = untypedArtifact.CreateSession();
        session.SetArgument("value", 7);
        session.SetArgument("text", "shared");

        Assert.That(untypedArtifact.SourceText, Is.EqualTo("value + text"));
        Assert.That(session.Run(), Is.EqualTo("compiled:7:shared"));
    }

    [Test]
    public void TypedCode_CanReadCompilationOutput_FromTypedInterface()
    {
        var artifact = CreateTwoArgumentArtifact("typed-output");
        ICompiledArtifact<string> typedArtifact = artifact;

        Assert.That(typedArtifact.CompilationOutput, Is.EqualTo("typed-output"));
    }

    [Test]
    public void ICompiledArtifactCollection_CanStore_DifferentCompiledArtifactTypes()
    {
        var stringArtifact = CreateTwoArgumentArtifact("string-output", 5, "x");
        var intArtifact = new CompiledArtifact<int>(
            "int-compiled",
            [new ExternalBinding { Name = "value", Type = typeof(int), Value = 2, Kind = ExternalBindingKind.Variable }],
            123,
            new IntIdentityExecutor());

        IReadOnlyList<ICompiledArtifact> artifacts = [stringArtifact, intArtifact];

        Assert.That(artifacts, Has.Count.EqualTo(2));
        Assert.That(artifacts[0].CreateSession().Run(), Is.EqualTo("string-output:5:x"));
        Assert.That(artifacts[1].CreateSession().Run(), Is.EqualTo(123));
    }

    [Test]
    public void CompiledArtifact_CreateSession_ReturnsIndependentSessions()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");

        var first = artifact.CreateSession();
        var second = artifact.CreateSession();

        first.SetArgument(0, 5);
        first.SetArgument(1, "left");

        second.SetArgument(0, 9);
        second.SetArgument(1, "right");

        Assert.That(first.Run(), Is.EqualTo("compiled:5:left"));
        Assert.That(second.Run(), Is.EqualTo("compiled:9:right"));
    }

    [Test]
    public void CompiledArtifact_CreateSession_PreservesDeclaredBindingOrder()
    {
        var artifact = new CompiledArtifact<string>(
            "order",
            [
                new ExternalBinding { Name = "beta", Type = typeof(int), Value = 1, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "alpha", Type = typeof(int), Value = 2, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "gamma", Type = typeof(int), Value = 3, Kind = ExternalBindingKind.Variable }
            ],
            "ordered",
            new OrderedSlotsExecutor());

        var session = artifact.CreateSession();

        Assert.That(session.Run(), Is.EqualTo("ordered:1|2|3"));
    }

    [Test]
    public void CompiledArtifact_CreateSession_PreservesDeclaredDefaultValues()
    {
        var artifact = CreateTwoArgumentArtifact("compiled", 7, "seed");
        var session = artifact.CreateSession();

        var result = session.Run();

        Assert.That(result, Is.EqualTo("compiled:7:seed"));
    }

    [Test]
    public void CompiledArtifactSession_SetArgument_ByName_UsesArtifactSlots()
    {
        var artifact = CreateTwoArgumentArtifact("compiled", 0, "init");
        var session = artifact.CreateSession();

        session.SetArgument("text", "name-slot");
        session.SetArgument("value", 42);

        Assert.That(session.Run(), Is.EqualTo("compiled:42:name-slot"));
    }

    [Test]
    public void CompiledArtifactSession_SetArgument_BySlot_UsesDeclaredBindingTypeValidation()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");
        var session = artifact.CreateSession();

        Assert.Throws<ArgumentException>(() => session.SetArgument(0, "wrong-type"));
    }

    [Test]
    public void CompiledArtifactSession_SetArgument_RejectsConstantBindingBySlot()
    {
        var artifact = CreateArtifactWithConstantBinding("compiled");
        var session = artifact.CreateSession();

        var ex = Assert.Throws<InvalidOperationException>(() => session.SetArgument(0, 99));

        Assert.That(ex!.Message, Is.EqualTo("Binding 'value' at slot 0 is constant and cannot be reassigned."));
    }

    [Test]
    public void CompiledArtifactSession_SetArgument_RejectsConstantBindingByName()
    {
        var artifact = CreateArtifactWithConstantBinding("compiled");
        var session = artifact.CreateSession();

        var ex = Assert.Throws<InvalidOperationException>(() => session.SetArgument("value", 99));

        Assert.That(ex!.Message, Is.EqualTo("Binding 'value' at slot 0 is constant and cannot be reassigned."));
    }

    [Test]
    public void CompiledArtifactSession_SetArgument_AllowsVariableBindingMutation()
    {
        var artifact = CreateTwoArgumentArtifact("compiled", 1, "seed");
        var session = artifact.CreateSession();

        session.SetArgument(0, 10);

        Assert.That(session.Run(), Is.EqualTo("compiled:10:seed"));
    }

    [Test]
    public void CompiledArtifactSession_Run_UsesArtifactCompilationOutput()
    {
        var artifact = CreateTwoArgumentArtifact("expected-output", 3, "x");
        var session = artifact.CreateSession();

        var result = session.Run();

        Assert.That(result, Is.EqualTo("expected-output:3:x"));
    }

    [Test]
    public void CompiledArtifactSession_Run_CanBeRepeatedWithDifferentArguments()
    {
        var artifact = CreateTwoArgumentArtifact("compiled", 0, string.Empty);
        var session = artifact.CreateSession();

        var first = session.Invoke<string, string>(1, "a");
        var second = session.Invoke<string, string>(8, "b");

        Assert.That(first, Is.EqualTo("compiled:1:a"));
        Assert.That(second, Is.EqualTo("compiled:8:b"));
    }

    [Test]
    public void CompiledArtifactSession_Run_CanBeInvokedViaInterface()
    {
        var artifact = CreateTwoArgumentArtifact("compiled", 0, string.Empty);
        ICompiledArtifactSession session = artifact.CreateSession();

        var first = session.Invoke<string>(1, "a");
        var second = session.InvokeNamed<string>(new Dictionary<string, object?>
        {
            ["value"] = 8,
            ["text"] = "b"
        });

        Assert.That(first, Is.EqualTo("compiled:1:a"));
        Assert.That(second, Is.EqualTo("compiled:8:b"));
    }

    [Test]
    public void CompiledArtifactSession_Run_RejectsNullForNonNullableValueType()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");
        var session = artifact.CreateSession();

        Assert.Throws<ArgumentException>(() => session.Invoke<string, string>(null, "value"));
    }

    [Test]
    public void CompiledArtifactSession_Run_RejectsUnknownArgumentName()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");
        var session = artifact.CreateSession();

        Assert.Throws<ArgumentException>(() => session.InvokeNamed<string, string>(new Dictionary<string, object?>
        {
            ["value"] = 1,
            ["unknown"] = "x"
        }));
    }

    [Test]
    public void CompiledArtifactSession_Run_RejectsWrongArgumentCount()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");
        var session = artifact.CreateSession();

        Assert.Throws<ArgumentException>(() => session.Invoke<string, string>(1));
    }

    [Test]
    public void CompiledArtifactSession_Run_RejectsWrongArgumentType()
    {
        var artifact = CreateTwoArgumentArtifact("compiled");
        var session = artifact.CreateSession();

        Assert.Throws<ArgumentException>(() => session.Invoke<string, string>("bad", "ok"));
    }

    private static CompiledArtifact<string> CreateTwoArgumentArtifact(string compilationOutput, int defaultValue = 0, string defaultText = "")
    {
        return new CompiledArtifact<string>(
            "value + text",
            [
                new ExternalBinding { Name = "value", Type = typeof(int), Value = defaultValue, Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "text", Type = typeof(string), Value = defaultText, Kind = ExternalBindingKind.Variable }
            ],
            compilationOutput,
            new PairFormattingExecutor());
    }

    private static CompiledArtifact<string> CreateArtifactWithConstantBinding(string compilationOutput)
    {
        return new CompiledArtifact<string>(
            "value + text",
            [
                new ExternalBinding { Name = "value", Type = typeof(int), Value = 1, Kind = ExternalBindingKind.Constant },
                new ExternalBinding { Name = "text", Type = typeof(string), Value = "seed", Kind = ExternalBindingKind.Variable }
            ],
            compilationOutput,
            new PairFormattingExecutor());
    }

    private sealed class PairFormattingExecutor : IExecutor<string>
    {
        public object? Execute(string compilation, IExecutionEnvironment environment)
        {
            return $"{compilation}:{environment.GetExternalValue(0)}:{environment.GetExternalValue(1)}";
        }
    }

    private sealed class OrderedSlotsExecutor : IExecutor<string>
    {
        public object? Execute(string compilation, IExecutionEnvironment environment)
        {
            return $"{compilation}:{environment.GetExternalValue(0)}|{environment.GetExternalValue(1)}|{environment.GetExternalValue(2)}";
        }
    }

    private sealed class IntIdentityExecutor : IExecutor<int>
    {
        public object? Execute(int compilation, IExecutionEnvironment environment)
        {
            return compilation;
        }
    }

    private static WistDialectExecutionHost CreateHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect CompiledArtifactContracts
            use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
            backend compiler
            """,
            "compiled-artifact-contracts-inline");

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    private static BasicCoreImpl<DynamicMethod> GetCompilerCore(WistDialectExecutionHost host)
    {
        return host.GetCore("compiler") as BasicCoreImpl<DynamicMethod>
               ?? Thrower.InvalidOpEx<BasicCoreImpl<DynamicMethod>>("Compiler core must be BasicCoreImpl<DynamicMethod>.");
    }
}
