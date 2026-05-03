using System.Reflection.Emit;
using DynamicMethodCalling.Core;

namespace Tests.Backends;

[TestFixture]
public sealed class CilBackendPureSignatureTests
{
    [Test]
    public void Compile_PureExternalArithmeticIr_ShouldNotRequireExecutionEnvironmentArgument()
    {
        var ir = BuildIr(
            new Instruction(UOpCode.Intrinsic, ["load_external", 0, typeof(double)]),
            new Instruction(UOpCode.Intrinsic, ["load_external", 1, typeof(double)]),
            new Instruction(UOpCode.Intrinsic, ["add_f64"]));
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
        var parameterTypes = compiled.GetParameters()
            .Select(static parameter => parameter.ParameterType)
            .ToArray();
        var invoker = new DynamicMethodInvoker<double, double, double>(compiled);
        var result = invoker.Invoke(19.0, 23.0);

        Assert.Multiple(() =>
        {
            Assert.That(parameterTypes, Is.EqualTo(new[] { typeof(double), typeof(double) }));
            Assert.That(parameterTypes, Does.Not.Contain(typeof(IExecutionEnvironment)));
            Assert.That(result, Is.EqualTo(42.0));
        });
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var ir = new AbstractIR();
        ir.AppendInstructions(instructions);
        return ir;
    }
}
