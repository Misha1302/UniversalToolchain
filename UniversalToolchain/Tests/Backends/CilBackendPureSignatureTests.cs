using System.Reflection.Emit;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Presets;

namespace Tests.Backends;

[TestFixture]
public sealed class CilBackendPureSignatureTests
{
    [Test]
    public void Compile_PureExternalArithmeticIr_ShouldNotRequireExecutionEnvironmentArgument()
    {
        var ir = BuildIr(
            IntrinsicInstructionFactory.CreateForCapability("load_external", 0, typeof(double)),
            IntrinsicInstructionFactory.CreateForCapability("load_external", 1, typeof(double)),
            IntrinsicInstructionFactory.CreateForCapability("add_f64"));
        var input = new CompilationInput
        {
            SourceText = string.Empty,
            ExternalBindings =
            [
                new ExternalBinding { Name = "left", Type = typeof(double), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "right", Type = typeof(double), Kind = ExternalBindingKind.Variable }
            ]
        };

        var compiled = new AbstractMethodsCompilerImpl().Compile(ir, input);
        var parameterTypes = compiled.Method.GetParameters()
            .Select(static parameter => parameter.ParameterType)
            .ToArray();
        var invoker = new DynamicMethodInvoker<double, double, double>(compiled.Method);
        var result = invoker.Invoke(19.0, 23.0);

        Assert.Multiple(() =>
        {
            Assert.That(parameterTypes, Is.EqualTo(new[] { typeof(double), typeof(double) }));
            Assert.That(parameterTypes, Does.Not.Contain(typeof(IExecutionEnvironment)));
            Assert.That(result, Is.EqualTo(42.0));
        });
    }


    [Test]
    public void Compile_ExternalConstantsHeavyFormula_ShouldProduceSixParameterDynamicMethodWithoutExecutionEnvironment()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var dialectPath = new WistShippedDialectFileResolver().Resolve(WistShippedDialectPresets.FullDefaultNative);
        var composition = workflow.ComposeFile(dialectPath);
        Assert.That(composition.IsSuccess, Is.True);

        using var host = workflow.CreateHost(composition);
        var compiler = host.GetBackendSpecificArtifactCompiler<CilCompilationOutput>("compiler");
        var compiled = compiler.Compile(
            "(A * 1.5 + B * 2.0 - C * 3.0 + D / 4.0 + E / 5.0) * 0.75 + F",
            new OrderedDictionary<string, Type>
            {
                ["A"] = typeof(double), ["B"] = typeof(double), ["C"] = typeof(double),
                ["D"] = typeof(double), ["E"] = typeof(double), ["F"] = typeof(double)
            });

        var parameterTypes = compiled.CompilationOutput.Method.GetParameters().Select(static x => x.ParameterType).ToArray();
        var invoker = new DynamicMethodInvoker<double, double, double, double, double, double, double>(compiled.CompilationOutput.Method);
        var result = invoker.Invoke(10.0, 20.0, 3.0, 8.0, 5.0, 1.5);
        var expected = (10.0 * 1.5 + 20.0 * 2.0 - 3.0 * 3.0 + 8.0 / 4.0 + 5.0 / 5.0) * 0.75 + 1.5;

        Assert.Multiple(() =>
        {
            Assert.That(parameterTypes, Is.EqualTo(new[]
            {
                typeof(double), typeof(double), typeof(double), typeof(double), typeof(double), typeof(double)
            }));
            Assert.That(parameterTypes, Does.Not.Contain(typeof(IExecutionEnvironment)));
            Assert.That(result, Is.EqualTo(expected).Within(1e-9));
        });
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }
}
