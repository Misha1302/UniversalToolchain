using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaRoundtripRouteTests
{
    [Test]
    public void Run_WhenPolicyIsOff_ReturnsInputWithoutUsingSsa()
    {
        var source = new AbstractIR();
        source.Push(42);

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Off);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Run_WhenPolicyIsPreferAndRouteIsUnsupported_FallsBackWithDiagnostics()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var result = new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Prefer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Program, Is.SameAs(source));
            Assert.That(result.UsedSsa, Is.False);
            Assert.That(result.FellBackToInput, Is.True);
            Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
        });
    }

    [Test]
    public void Run_WhenPolicyIsRequireAndRouteIsUnsupported_ThrowsDiagnosticException()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var exception = Assert.Throws<SsaRouteException>(() =>
            new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Require));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
    }

    [Test]
    public void Run_WhenPolicyIsDebugAndRouteIsUnsupported_ThrowsDiagnosticException()
    {
        var source = new AbstractIR();
        source.Intrinsic("custom.intrinsic");

        var exception = Assert.Throws<SsaRouteException>(() =>
            new SsaRoundtripRoute().Run(source, SsaRoutePolicy.Debug));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("air.stack.invalid"));
    }

    [Test]
    public void Run_WhenPolicyIsDebugAndRouteIsSupported_ReturnsOptimizedRoundtrippedAir()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = SsaRouteFactory
            .CreateRoundtripRoute(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug))
            .Run(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Program.Instructions.Select(static x => x.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push
            }));
            Assert.That(result.Program.Instructions[1].Operands.Single(), Is.EqualTo(5));
        });
    }

    [Test]
    public void Run_WhenUsingRawConvertersWithoutProfile_DoesNotRunOptimizationPasses()
    {
        var source = new AbstractIR();
        source.Push(2);
        source.Push(3);
        source.Intrinsic(AirIntrinsicIds.AddInt32Unchecked);

        var result = new SsaRoundtripRoute(
                SsaRouteFactory.CreateLowerer(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug)),
                SsaRouteFactory.CreateEmitter(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Debug)))
            .Run(source, SsaRoutePolicy.Debug);

        Assert.Multiple(() =>
        {
            Assert.That(result.UsedSsa, Is.True);
            Assert.That(result.FellBackToInput, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Program.Instructions.Select(static x => x.UOpCode), Is.EqualTo(new[]
            {
                UOpCode.Label,
                UOpCode.Push,
                UOpCode.Push,
                UOpCode.Intrinsic
            }));
        });
    }
}
