using System.Reflection;
using NUnit.Framework;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

/// <summary>
///     Pins that a Wist facade code describes the failure, not the API the caller happened to use.
/// </summary>
/// <remarks>
///     <para>
///         <c>BindingException</c> was the one stage <c>ResolveCode</c> did not map. Every other stage had a
///         branch, so binding failures fell through to the entry-point fallback and the code became a
///         function of <c>operationStage</c>: the same unknown identifier reported <c>UTC-WIST-VALIDATE-001</c>
///         through <c>Validate</c> and <c>UTC-WIST-COMPILE-001</c> through <c>TryCompile</c>.
///     </para>
///     <para>
///         Measured before the fix:
///     </para>
///     <code>
///     Validate("unknownName + 1")   -&gt; UTC-WIST-VALIDATE-001
///     TryCompile("unknownName + 1") -&gt; UTC-WIST-COMPILE-001
///     both exceptions: InvalidOperationException wrapping BindingException
///     </code>
///     <para>
///         A caller switching on the code to decide "the user wrote a bad name" versus "the backend could not
///         compile" got different answers for one failure. The tests below compare the code across entry points
///         rather than asserting a fixed string twice, because the invariant is agreement and not a particular
///         value - that is what a future remapping could silently break.
///     </para>
/// </remarks>
[TestFixture]
public sealed class WistDiagnosticCodeConsistencyTests
{
    [Test]
    public void UnknownIdentifier_ReportsTheSameCodeThroughValidateAndTryCompile()
    {
        using var engine = WistEngine.CreateRestrictedArithmetic();

        var validated = engine.Validate("missing + 1.0");
        var compiled = engine.TryCompile<Func<double>>("missing + 1.0");

        Assert.Multiple(() =>
        {
            Assert.That(validated.Diagnostics, Is.Not.Empty);
            Assert.That(compiled.Diagnostics, Is.Not.Empty);
            Assert.That(
                compiled.Diagnostics[0].Code,
                Is.EqualTo(validated.Diagnostics[0].Code),
                "one failure must not be reported as two different codes depending on the entry point");
        });
    }

    [Test]
    public void UnknownIdentifier_IsReportedAsResolutionFailure()
    {
        using var engine = WistEngine.CreateRestrictedArithmetic();

        var result = engine.Validate("missing + 1.0");

        Assert.That(result.Diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.TypeResolutionFailure));
    }

    [Test]
    public void UnknownIdentifier_HasTheBindingStageNotTheEntryPointStage()
    {
        using var engine = WistEngine.CreateRestrictedArithmetic();

        var validated = engine.Validate("missing + 1.0");
        var compiled = engine.TryCompile<Func<double>>("missing + 1.0");

        Assert.Multiple(() =>
        {
            Assert.That(validated.Diagnostics[0].Stage, Is.EqualTo("Binding"));
            Assert.That(compiled.Diagnostics[0].Stage, Is.EqualTo("Binding"));
        });
    }

    [Test]
    public void SyntaxFailures_KeepTheirOwnCodesRatherThanCollapsingIntoResolution()
    {
        // The control: mapping Binding must not turn every failure into a resolution failure.
        using var engine = WistEngine.CreateRestrictedArithmetic();

        var lexer = engine.Validate("#");
        var parser = engine.Validate("1 +");

        Assert.Multiple(() =>
        {
            Assert.That(lexer.Diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.LexerFailure));
            Assert.That(parser.Diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.ParserFailure));
        });
    }

    [Test]
    public void EveryDeclaredStage_ResolvesToAStageCodeRatherThanAnEntryPointFallback()
    {
        // The invariant behind the bug: every stage a WistException can carry must have a mapping, so no failure
        // can take its identity from operationStage. The stage list is discovered from the exception types, not
        // written by hand - a hand-written list is exactly what missed Binding while the factory missed it too.
        var stages = DeclaredStages();

        Assert.Multiple(() =>
        {
            Assert.That(stages, Does.Contain("Binding"), "the fixture must still see the stage the factory omitted");
            Assert.That(stages, Does.Contain("Dialect"), "the fixture must see stages declared outside CommonExceptions");
            Assert.That(stages, Does.Contain("Policy"), "the fixture must see stages behind a non-obvious constructor");
        });

        foreach (var stage in stages)
        {
            var exception = CreateExceptionFor(stage);

            var throughValidation = WistDiagnosticFactory
                .FromException(exception, "Validation", WistDiagnosticExposure.Developer)
                .Single();
            var throughCompilation = WistDiagnosticFactory
                .FromException(exception, "Compilation", WistDiagnosticExposure.Developer)
                .Single();

            Assert.Multiple(() =>
            {
                // Bytecode and InternalCompiler legitimately resolve to the compilation code, so the test cannot
                // simply forbid it. What it forbids is the entry-point identity: one stage must produce one code
                // whichever API observed it, and no stage may reach the unmatched fallback.
                Assert.That(throughValidation.Code, Is.EqualTo(throughCompilation.Code),
                    $"stage '{stage}' takes its code from operationStage instead of from the exception");
                Assert.That(throughValidation.Code, Is.Not.EqualTo(WistDiagnosticCodes.UnexpectedFailure),
                    $"stage '{stage}' is unmapped and falls through to the unexpected-failure code");
                Assert.That(throughValidation.Stage, Is.EqualTo(stage));
            });
        }
    }

    [Test]
    public void WrappedResourceLimit_KeepsItsOwnCodeAndHint()
    {
        // Code, stage and hints must come from the same typed exception. Measured before the fix: wrapping the
        // limit in a plain exception turned UTC-WIST-001 into UTC-WIST-VALIDATE-001 and dropped the hint, while
        // the stage stayed "Policy" - one diagnostic disagreeing with itself.
        var wrapped = new InvalidOperationException(
            "wrapper", new WistResourceLimitException("UTC-WIST-001", "too long"));

        var diagnostic = WistDiagnosticFactory
            .FromException(wrapped, "Validation", WistDiagnosticExposure.Developer)
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo("UTC-WIST-001"));
            Assert.That(diagnostic.Stage, Is.EqualTo("Policy"));
            Assert.That(diagnostic.Hints, Is.Not.Empty);
        });
    }

    [Test]
    public void WrappedDialectFeatureFailure_KeepsItsHint()
    {
        var wrapped = new InvalidOperationException(
            "wrapper",
            new WistDialectFeatureException("dialect", new InvalidOperationException("inner")));

        var diagnostic = WistDiagnosticFactory
            .FromException(wrapped, "Validation", WistDiagnosticExposure.Developer)
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(diagnostic.Code, Is.EqualTo(WistDiagnosticCodes.DialectFailure));
            Assert.That(diagnostic.Hints, Is.Not.Empty);
        });
    }

    /// <summary>
    ///     Every stage a concrete <see cref="CommonExceptions.WistException" /> subtype declares, discovered by
    ///     constructing each subtype rather than by listing stages by hand.
    /// </summary>
    /// <remarks>
    ///     Two assemblies are scanned because the stage-bearing types are split across them:
    ///     <c>CommonExceptions</c> holds the pipeline stages, and <c>UniversalToolchain.Wist</c> adds
    ///     <see cref="WistDialectFeatureException" /> with <c>Dialect</c>. Referencing both types forces both
    ///     assemblies to load; scanning only the assembly of the base class silently missed the dialect stage.
    /// </remarks>
    private static IReadOnlyList<string> DeclaredStages()
    {
        var stages = new SortedSet<string>(StringComparer.Ordinal);
        var assemblies = new[] { typeof(CommonExceptions.WistException).Assembly, typeof(WistDialectFeatureException).Assembly };

        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(CommonExceptions.WistException).IsAssignableFrom(type))
                    continue;

                var instance = Construct(type);

                // The base WistException declares no stage; it is the "no typed stage" case, not a stage.
                if (instance?.Stage is not { Length: > 0 } stage)
                    continue;

                stages.Add(stage);
            }
        }

        return stages.ToArray();
    }

    /// <summary>
    ///     Instantiates a stage-bearing exception type using whichever constructor shape it exposes.
    /// </summary>
    /// <remarks>
    ///     Three shapes exist and all three are needed: pipeline stages take a message, the dialect failure takes
    ///     message plus inner, and the resource limit takes a diagnostic code plus a message. Recognising only the
    ///     first skipped the dialect stage, and ignoring the third skipped the policy stage - both silently, which
    ///     is the failure mode this fixture exists to avoid.
    /// </remarks>
    private static CommonExceptions.WistException? Construct(Type type)
    {
        // Non-public constructors count too: WistResourceLimitException's is internal, and
        // GetConstructor(Type[]) only finds public ones - which silently dropped the policy stage.
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        if (type.GetConstructor(flags, null, [typeof(string)], null) is not null)
            return (CommonExceptions.WistException?)Activator.CreateInstance(
                type, flags, null, ["probe"], null);
        if (type.GetConstructor(flags, null, [typeof(string), typeof(Exception)], null) is not null)
            return (CommonExceptions.WistException?)Activator.CreateInstance(
                type, flags, null, ["probe", new InvalidOperationException("inner")], null);
        if (type.GetConstructor(flags, null, [typeof(string), typeof(string)], null) is not null)
            return (CommonExceptions.WistException?)Activator.CreateInstance(
                type, flags, null, ["UTC-WIST-001", "probe"], null);
        return null;
    }

    private static CommonExceptions.WistException CreateExceptionFor(string stage) =>
        stage switch
        {
            "Lexer" => new CommonExceptions.LexerException("lexer", new InvalidOperationException("inner")),
            "Parser" => new CommonExceptions.ParserException("parser"),
            "Binding" => new CommonExceptions.BindingException("binding"),
            "Bytecode" => new CommonExceptions.BytecodeGenerationException("bytecode"),
            "Runtime" => new CommonExceptions.RuntimeExecutionException("runtime"),
            "TypeSystem" => new CommonExceptions.TypeSystemException("type system"),
            "Import" => new CommonExceptions.ImportException("import"),
            "InternalCompiler" => new CommonExceptions.InternalCompilerException("internal"),
            "Dialect" => new WistDialectFeatureException("dialect", new InvalidOperationException("inner")),
            "Policy" => new WistResourceLimitException("UTC-WIST-001", "probe"),
            _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "unknown stage")
        };
}
