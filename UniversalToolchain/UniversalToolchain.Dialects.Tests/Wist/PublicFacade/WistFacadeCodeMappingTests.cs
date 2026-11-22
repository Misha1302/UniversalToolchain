using NUnit.Framework;
using UniversalToolchain.Ssa.Optimization;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

/// <summary>
///     Pins that each Wist facade code is produced by the stage it documents, not merely that the stage has a code.
/// </summary>
/// <remarks>
///     <para>
///         <c>WistDiagnosticCodeConsistencyTests</c> proves one stage cannot take two codes depending on the entry
///         point. That is a stability property: it stays green if every stage is remapped to the same wrong code.
///         These tests pin the mapping itself.
///     </para>
///     <para>
///         Five facade codes had no test at all: <c>COMPILE-001</c>, <c>EXEC-001</c>, <c>RESOLVE-002</c>,
///         <c>SSA-001</c> and <c>VALIDATE-001</c>. Four of them are reachable from the public surface through an
///         exception of the right shape; <c>SSA-001</c> needs an <c>SsaRouteException</c>, which the facade maps
///         before it looks at stages at all.
///     </para>
/// </remarks>
[TestFixture]
public sealed class WistFacadeCodeMappingTests
{
    [Test]
    public void BytecodeStage_IsACompilationFailure()
    {
        // The pin the stability test cannot provide: a stage must keep its documented meaning.
        AssertCode(new CommonExceptions.BytecodeGenerationException("bytecode"), WistDiagnosticCodes.CompilationFailure);
    }

    [Test]
    public void RuntimeStage_IsAnExecutionFailure()
    {
        AssertCode(new CommonExceptions.RuntimeExecutionException("runtime"), WistDiagnosticCodes.ExecutionFailure);
    }

    [Test]
    public void InternalCompilerStage_IsACompilationFailure()
    {
        AssertCode(new CommonExceptions.InternalCompilerException("internal"), WistDiagnosticCodes.CompilationFailure);
    }

    [Test]
    public void AmbiguousMatchException_IsAnAmbiguousResolutionFailure()
    {
        // Sits ahead of every stage branch in ResolveCode, so it wins even though the exception carries no stage.
        AssertCode(
            new System.Reflection.AmbiguousMatchException("ambiguous"),
            WistDiagnosticCodes.AmbiguousResolution);
    }

    [Test]
    public void TypeLoadException_IsAResolutionFailure()
    {
        AssertCode(new TypeLoadException("type load"), WistDiagnosticCodes.TypeResolutionFailure);
    }

    [Test]
    public void SsaRouteFailure_BecomesTheSsaRouteCodeAndCarriesTheRouteReport()
    {
        // The one code the factory handles before any stage lookup: an SSA route exception is rebuilt into a
        // diagnostic that keeps the route report's own stage and mention of its diagnostic code.
        var report = new SsaRouteReport(
            SsaRoutePolicy.Require,
            "test-profile",
            usedSsa: false,
            fellBackToInput: false,
            inputAirInstructionCount: 3,
            outputAirInstructionCount: 3,
            executedPasses: [],
            diagnostics: [new SsaRouteDiagnostic("ssa.test.failed", "route could not continue", "lowering")]);

        var diagnostics = WistDiagnosticFactory.FromException(
            new SsaRouteException(report),
            "Validation",
            WistDiagnosticExposure.Developer);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Has.Count.EqualTo(1));
            Assert.That(diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.SsaRouteFailure));
            Assert.That(diagnostics[0].Message, Does.Contain("ssa.test.failed"));
            Assert.That(diagnostics[0].Stage, Is.EqualTo("SSA Lowering"));
        });
    }

    [Test]
    public void UnrecognisedFailureWithNoStage_FallsBackToTheEntryPointCode()
    {
        // VALIDATE-001 is documented as "validation failure not classified above", so this is the branch that
        // gives it its meaning. Without this test the code is only ever produced by accident.
        var diagnostic = WistDiagnosticFactory
            .FromException(new InvalidOperationException("unclassified"), "Validation", WistDiagnosticExposure.Developer)
            .Single();

        Assert.That(diagnostic.Code, Is.EqualTo(WistDiagnosticCodes.ValidationFailure));
    }

    [Test]
    public void UnrecognisedFailureWithNoStage_StillFallsBackDuringCompilation()
    {
        // The control for the previous test: the fallback follows the entry point, and that is intended for a
        // failure no stage explains. Pinning only the validation branch would leave the mechanism itself unpinned.
        var diagnostic = WistDiagnosticFactory
            .FromException(new InvalidOperationException("unclassified"), "Compilation", WistDiagnosticExposure.Developer)
            .Single();

        Assert.That(diagnostic.Code, Is.EqualTo(WistDiagnosticCodes.CompilationFailure));
    }

    private static void AssertCode(Exception exception, string expectedCode)
    {
        var diagnostic = WistDiagnosticFactory
            .FromException(exception, "Validation", WistDiagnosticExposure.Developer)
            .Single();

        Assert.That(diagnostic.Code, Is.EqualTo(expectedCode));
    }
}
