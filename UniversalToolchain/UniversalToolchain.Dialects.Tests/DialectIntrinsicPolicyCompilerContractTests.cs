using BasicCore.Compilation;
using BasicCore.Contracts;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

[TestFixture]
public sealed class DialectIntrinsicPolicyCompilerContractTests
{
    [Test]
    public void Compile_WhenIntrinsicIsForbidden_ShouldRejectBeforeCallingInnerCompiler()
    {
        var inner = new TrackingCompiler();
        var compiler = new DialectIntrinsicPolicyCompiler<IAbstractIR>(
            inner,
            allowedIntrinsics: [],
            forbiddenIntrinsics: ["boolean_not"]);
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["boolean_not"]));
        var input = new CompilationInput { SourceText = "test" };

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(ir, input));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("forbidden by the selected dialect"));
            Assert.That(inner.WasCalled, Is.False);
        });
    }

    [Test]
    public void Compile_WhenExplicitAllowListIsEnabled_ShouldRejectUnlistedIntrinsicBeforeCallingInnerCompiler()
    {
        var inner = new TrackingCompiler();
        var compiler = new DialectIntrinsicPolicyCompiler<IAbstractIR>(
            inner,
            allowedIntrinsics: ["call C#"],
            forbiddenIntrinsics: [],
            hasExplicitAllowList: true);
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["boolean_not"]));
        var input = new CompilationInput { SourceText = "test" };

        var exception = Assert.Throws<InvalidOperationException>(() => compiler.Compile(ir, input));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("not allowed by the selected dialect"));
            Assert.That(inner.WasCalled, Is.False);
        });
    }

    [Test]
    public void Compile_WhenIntrinsicPolicyAllowsAir_ShouldDelegateToInnerCompiler()
    {
        var inner = new TrackingCompiler();
        var compiler = new DialectIntrinsicPolicyCompiler<IAbstractIR>(
            inner,
            allowedIntrinsics: ["call C#"],
            forbiddenIntrinsics: [],
            hasExplicitAllowList: true);
        var ir = BuildIr(new Instruction(UOpCode.Intrinsic, ["call C#", typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!]));
        var input = new CompilationInput { SourceText = "test" };

        var result = compiler.Compile(ir, input);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(ir));
            Assert.That(inner.WasCalled, Is.True);
        });
    }

    [Test]
    public void SupportedIntrinsics_ShouldExcludeForbiddenIntrinsicsAndStayDeterministic()
    {
        var compiler = new DialectIntrinsicPolicyCompiler<IAbstractIR>(
            new TrackingCompiler(["zeta", "boolean_not", "alpha"]),
            allowedIntrinsics: [],
            forbiddenIntrinsics: ["boolean_not"]);

        Assert.That(compiler.SupportedIntrinsics, Is.EqualTo(new[] { "alpha", "zeta" }));
    }

    [Test]
    public void SupportedIntrinsics_WhenExplicitAllowListIsEnabled_ShouldExposeOnlyAllowedSupportedIntrinsics()
    {
        var compiler = new DialectIntrinsicPolicyCompiler<IAbstractIR>(
            new TrackingCompiler(["zeta", "boolean_not", "alpha"]),
            allowedIntrinsics: ["zeta"],
            forbiddenIntrinsics: [],
            hasExplicitAllowList: true);

        Assert.That(compiler.SupportedIntrinsics, Is.EqualTo(new[] { "zeta" }));
    }

    private static IAbstractIR BuildIr(params Instruction[] instructions)
    {
        var air = new AbstractIR();
        air.AppendInstructions(instructions);
        return air;
    }

    private sealed class TrackingCompiler : IAbstractIrCompiler<IAbstractIR>
    {
        public TrackingCompiler(IReadOnlyList<string>? supportedIntrinsics = null)
        {
            SupportedIntrinsics = supportedIntrinsics ?? ["call C#", "boolean_not"];
        }

        public bool WasCalled { get; private set; }

        public IReadOnlyList<string> SupportedIntrinsics { get; }

        public IAbstractIR Compile(IAbstractIR air, CompilationInput input)
        {
            WasCalled = true;
            return air;
        }
    }
}
