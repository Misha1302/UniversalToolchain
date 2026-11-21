using System.Text.Json;
using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

/// <summary>
/// Cross-backend semantic parity over a committed corpus (UT-A050).
/// <para>
/// Two backends exist and the article's claim is that a program means the same thing on both. Today that
/// claim rested on a handful of hand-written tests that each pinned <em>one</em> backend, so nothing would
/// notice if the two drifted apart: the CIL test would stay green, the interpreter test would stay green,
/// and the same source would quietly mean two things.
/// </para>
/// <para>
/// This fixture is the differential test that was missing. It compiles and evaluates every corpus row on
/// both shipped backends under every SSA policy and compares <b>the exact CLR type and value</b>, or the
/// <b>root cause</b> of the failure — never the wrapper exception, because the interpreter adds instruction
/// context and the CIL path adds a reflection frame, and the facade's documented compatibility key is
/// <c>FailureKind</c>, not the outer type.
/// </para>
/// <para>
/// The corpus is committed data, regenerated only by <see cref="ParityCorpusGenerator"/>. A corpus computed
/// at test time agrees with whatever the code currently does and therefore cannot fail.
/// </para>
/// </summary>
[TestFixture]
public sealed class ParityCorpusTests
{
    private static ParityCorpus? _corpus;

    private static ParityCorpus Corpus => _corpus ??= Load();

    [Test]
    public void Corpus_CoversEveryDualBackendPresetAndBothBackends()
    {
        // A differential test over a corpus that exercises one preset, or one backend, proves nothing about
        // parity: it compares a backend to itself. This is the coverage assertion that keeps the loops honest.
        var expectedPresets = ParityCorpusGenerator.DualBackendPresets();
        var actualPresets = Corpus.Rows.Select(static row => row.Preset).Distinct(StringComparer.Ordinal).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actualPresets, Is.EquivalentTo(expectedPresets),
                "every preset that declares two backends must appear, and only those presets can");
            Assert.That(Corpus.Rows, Has.Count.EqualTo(expectedPresets.Length * ParityCorpusGenerator.Sources.Length),
                "the corpus must have exactly one row per (preset, source)");
            Assert.That(Corpus.Rows.Select(static row => ParityCorpusGenerator.RowKey(row.Preset, row.Source)),
                Is.Unique, "a duplicated row would let one measurement stand in for two");
            Assert.That(expectedPresets, Has.Length.GreaterThanOrEqualTo(5),
                "the dual-backend preset set shrank; parity is now measured on a narrower surface");
        });
    }

    [Test]
    public void EveryCorpusRow_HasIdenticalValueAndExactTypeOnBothBackends()
    {
        var failures = new List<string>();

        foreach (var row in Corpus.Rows)
        foreach (var policy in new[] { WistSsaPolicy.Disabled, WistSsaPolicy.Prefer })
        {
            var cil = ParityCorpusGenerator.Probe(row.Preset, "cil", policy, row.Source);
            var interpreter = ParityCorpusGenerator.Probe(row.Preset, "interpreter", policy, row.Source);

            if (!cil.Equals(interpreter))
            {
                failures.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}/{policy}: " +
                    $"cil={cil} interpreter={interpreter}");
            }
        }

        Assert.That(failures, Is.Empty,
            "the same source must mean the same thing on both backends:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void EveryCorpusRow_MatchesItsRecordedExpectation()
    {
        // Parity can also be achieved by breaking both backends the same way. This assertion pins the
        // recorded outcome independently of the other backend, so a coordinated regression fails here.
        var failures = new List<string>();

        foreach (var row in Corpus.Rows)
        {
            var actual = ParityCorpusGenerator.Probe(row.Preset, "cil", WistSsaPolicy.Disabled, row.Source);
            if (!actual.Equals(row.Expected))
            {
                failures.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}: " +
                    $"recorded={row.Expected} actual={actual}");
            }
        }

        Assert.That(failures, Is.Empty,
            "a measured outcome changed without the corpus being regenerated:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void PreferPolicy_NeverChangesTheAnswer()
    {
        // Prefer means "use SSA when it applies, otherwise fall back to AIR". Falling back must not change
        // the value; if it did, the policy would be a second, undeclared semantics.
        var failures = new List<string>();

        foreach (var row in Corpus.Rows)
        foreach (var backend in ParityCorpusGenerator.Backends)
        {
            var disabled = ParityCorpusGenerator.Probe(row.Preset, backend, WistSsaPolicy.Disabled, row.Source);
            var prefer = ParityCorpusGenerator.Probe(row.Preset, backend, WistSsaPolicy.Prefer, row.Source);

            if (!disabled.Equals(prefer))
            {
                failures.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}/{backend}: " +
                    $"Disabled={disabled} Prefer={prefer}");
            }
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void RequirePolicy_DivergencesAreExactlyTheOnesOnRecord()
    {
        // Require is a capability axis, not a semantic one: it refuses to compile when the AIR carry an
        // intrinsic the SSA pack does not model, and the two backends emit different AIR. That is a real
        // asymmetry, and the honest options are to fix it or to pin its exact size.
        //
        // This test pins it. It fails both when a new divergence appears (a capability regression on one
        // backend) and when one disappears (the asymmetry was fixed and the record is now stale). Neither
        // may pass silently, because both change what "the first version of SSA" means.
        var divergences = Corpus.Rows
            .Where(static row => row.RequireOutcomes.Values.Distinct().Count() > 1)
            .Select(row => ParityCorpusGenerator.RowKey(row.Preset, row.Source))
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        var measured = Corpus.Rows
            .Where(row => ParityCorpusGenerator
                .Probe(row.Preset, "cil", WistSsaPolicy.Require, row.Source)
                .Equals(ParityCorpusGenerator.Probe(row.Preset, "interpreter", WistSsaPolicy.Require, row.Source)) == false)
            .Select(row => ParityCorpusGenerator.RowKey(row.Preset, row.Source))
            .OrderBy(static key => key, StringComparer.Ordinal)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(measured, Is.EqualTo(divergences),
                "the set of sources where Require behaves differently per backend changed. " +
                "If this is a fix, regenerate the corpus so the record shrinks; if it is a regression, " +
                "fix the SSA pack.");
            Assert.That(divergences, Has.Length.EqualTo(Corpus.DeclaredRequireDivergences),
                "the corpus' own declared divergence count disagrees with its rows");
        });
    }

    [Test]
    public void RequirePolicy_DivergenceIsConfinedToTheSSAPack_NotToTheLanguage()
    {
        // The divergence must be explainable, not mysterious. Every diverging source, under Require, has one
        // side whose AIR contain an intrinsic the SSA pack does not model. Measured, that side - and that
        // intrinsic - depends on the source:
        //   division on CIL            -> 'div_i32' is not modelled
        //   comparison on interpreter  -> the AIR carry 'call C#' (the interpreter's comparison path), which
        //                                 the pack does not model
        // Both `==` and `<` fail on the interpreter for that reason, and `<` fails on CIL with 'cmp_lt_i32',
        // so the two sides coincidentally agree on failure for `<`. What must hold is that a divergence is
        // always one side refusing with the documented SSA conversion failure, never the two backends
        // silently disagreeing on a value. The assertion below is therefore symmetric in the two sides.
        var unexplained = new List<string>();

        foreach (var row in Corpus.Rows)
        {
            var cil = ParityCorpusGenerator.Probe(row.Preset, "cil", WistSsaPolicy.Require, row.Source);
            var interpreter = ParityCorpusGenerator.Probe(row.Preset, "interpreter", WistSsaPolicy.Require, row.Source);
            if (cil.Equals(interpreter))
                continue;

            // Exactly one side must refuse with the documented SSA conversion failure.
            var cilRefused = cil.Failure == ParityCorpusGenerator.SsaConversionFailure;
            var interpreterRefused = interpreter.Failure == ParityCorpusGenerator.SsaConversionFailure;
            if (cilRefused == interpreterRefused)
            {
                unexplained.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}: {cil} vs {interpreter} - " +
                    $"expected exactly one SSA-capability refusal, got cil-refused={cilRefused}, " +
                    $"interpreter-refused={interpreterRefused}");
                continue;
            }

            // And the side that did compile must reproduce the recorded outcome. Without this, a divergence
            // could hide a real semantic change on the surviving backend.
            var survivor = cilRefused ? interpreter : cil;
            if (!survivor.Equals(row.Expected))
            {
                unexplained.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}: the backend that compiled " +
                    $"({(cilRefused ? "interpreter" : "cil")}) returned {survivor}, recorded {row.Expected}");
            }
        }

        Assert.That(unexplained, Is.Empty,
            "a Require divergence that is not an SSA-capability refusal means the semantic backends " +
            "genuinely disagree, which is worse:" + Environment.NewLine + string.Join(Environment.NewLine, unexplained));
    }

    [Test]
    public void CompilePath_AgreesWithEvaluatePathAndWithTheCorpus()
    {
        // Evaluate and Compile are two independent implementations of the same AIR: Evaluate uses the
        // registered artifact executor, Compile materializes a durable program. The mutant that returns
        // Int32+1 from the interpreter's executor passed every other test in this fixture, which is exactly
        // how a real drift between the two paths would look. This is the test that catches it.
        var failures = new List<string>();

        foreach (var row in Corpus.Rows)
        foreach (var backend in ParityCorpusGenerator.Backends)
        {
            var evaluated = ParityCorpusGenerator.Probe(row.Preset, backend, WistSsaPolicy.Disabled, row.Source);
            var compiled = ParityCorpusGenerator.ProbeCompiled(row.Preset, backend, WistSsaPolicy.Disabled, row.Source);

            if (!evaluated.Equals(compiled))
            {
                failures.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}/{backend}: " +
                    $"Evaluate={evaluated} Compile={compiled}");
            }

            if (!compiled.Equals(row.Expected))
            {
                failures.Add(
                    $"{ParityCorpusGenerator.RowKey(row.Preset, row.Source)}/{backend}: " +
                    $"Compile={compiled} recorded={row.Expected}");
            }
        }

        Assert.That(failures, Is.Empty,
            "the durable-program path and the executor path must agree, on both backends:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void Harness_DetectsInjectedDivergence()
    {
        // The self-test. A comparison that cannot report a difference leaves every green result above
        // meaningless: it would pass for two identical values and for two different ones alike.
        var value = ParityObservation.ForValue(5);
        var otherValue = ParityObservation.ForValue(6);
        var differentType = ParityObservation.ForValue(5.0);
        var failure = ParityObservation.ForFailure("DivideByZeroException");

        Assert.Multiple(() =>
        {
            Assert.That(value.Equals(otherValue), Is.False, "a value difference must be detected");
            Assert.That(value.Equals(differentType), Is.False,
                "Int32 5 and Double 5 must not compare equal: the corpus pins the CLR type");
            Assert.That(value.Equals(failure), Is.False, "a value and a failure must not compare equal");
            Assert.That(value.Equals(ParityObservation.ForValue(5)), Is.True, "equal observations must compare equal");
        });
    }

    [Test]
    public void Harness_DetectsInjectedDivergenceThroughTheRealProbe()
    {
        // The self-test above checks the comparison in isolation. This one injects a genuine cross-backend
        // divergence through the real measurement path, so the whole pipeline - engine creation, evaluation,
        // root-cause normalization - is proven able to report a mismatch.
        //
        // The injected divergence is a source the corpus does NOT contain: an int division that is an error
        // on one preset's int type and a value on another. Comparing across presets is exactly the shape a
        // drift between backends would take, and it must be visible.
        var intDivision = "7 / 2";
        var native = ParityCorpusGenerator.Probe(
            WistLanguageDefinitions.FullDefaultNativeId, "cil", WistSsaPolicy.Disabled, intDivision);
        var floating = ParityCorpusGenerator.Probe(
            WistLanguageDefinitions.FullDefaultId, "cil", WistSsaPolicy.Disabled, intDivision);

        Assert.Multiple(() =>
        {
            Assert.That(native.Type, Is.EqualTo("Int32"), "the native preset must yield an int division");
            Assert.That(floating.Type, Is.EqualTo("Double"), "the default preset must yield a double division");
            Assert.That(native.Equals(floating), Is.False,
                "the harness reported no difference between Int32:3 and Double:3.5, so every parity result " +
                "in this fixture would be meaningless");
        });
    }

    private static ParityCorpus Load()
    {
        var path = ParityCorpusGenerator.CorpusPath();
        Assert.That(File.Exists(path), Is.True,
            $"the committed corpus is missing at {path}. Run ParityCorpusGenerator deliberately, " +
            "review the diff, and commit the result.");
        var corpus = JsonSerializer.Deserialize<ParityCorpus>(File.ReadAllText(path), ParityCorpusGenerator.JsonOptions);
        Assert.That(corpus, Is.Not.Null, "the committed corpus could not be deserialized");
        return corpus!;
    }
}
