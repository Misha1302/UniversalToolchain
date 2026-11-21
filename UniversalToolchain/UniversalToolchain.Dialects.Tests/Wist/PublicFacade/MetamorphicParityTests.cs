using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

/// <summary>
/// Metamorphic parity (UT-A050): transformations that must not change a program's meaning.
/// <para>
/// The article's claim 4 is that <c>;</c> means the same thing as a newline. Stated that way it is a
/// <em>relation</em>, and a relation cannot be tested by one test per spelling: two separate tests each
/// pinning one form stay green even if the two forms diverge, because neither test knows about the other.
/// Each assertion below evaluates both forms and compares them.
/// </para>
/// <para>
/// Semicolon-as-newline is a <b>declared feature</b>, not universal syntax. This fixture therefore splits
/// presets by whether they declare it. For presets that do not, the honest assertion is the opposite one:
/// the semicolon must <b>not</b> silently act as a separator, because accepting it would mean the preset's
/// feature list does not describe the language the engine actually implements.
/// </para>
/// </summary>
[TestFixture]
public sealed class MetamorphicParityTests
{
    /// <summary>Presets that declare <c>wist.semicolon-as-new-line</c> (measured, not assumed).</summary>
    private static readonly string[] SemicolonPresets = ParityCorpusGenerator.DualBackendPresets()
        .Where(preset => WistLanguageDefinitions.Create(preset).SelectedFeatures
            .Contains(new LanguageFeatureId("wist.semicolon-as-new-line")))
        .ToArray();

    /// <summary>Dual-backend presets that do not declare it.</summary>
    private static readonly string[] NonSemicolonPresets = ParityCorpusGenerator.DualBackendPresets()
        .Except(SemicolonPresets, StringComparer.Ordinal)
        .ToArray();

    [Test]
    public void TheSplit_IsMeasuredAndBothSidesAreNonEmpty()
    {
        // If every preset declared the feature, the negative half of this fixture would be vacuous and a
        // regression that made semicolons universal would pass unnoticed.
        Assert.Multiple(() =>
        {
            Assert.That(SemicolonPresets, Is.Not.Empty,
                "no dual-backend preset declares semicolon-as-new-line; the positive half cannot be tested");
            Assert.That(NonSemicolonPresets, Is.Not.Empty,
                "every dual-backend preset now declares semicolon-as-new-line; the negative half is untested");
            Assert.That(SemicolonPresets.Concat(NonSemicolonPresets).Distinct(StringComparer.Ordinal).Count(),
                Is.EqualTo(ParityCorpusGenerator.DualBackendPresets().Length),
                "the split must partition the preset set, not sample it");
        });
    }

    [Test]
    public void SemicolonAndNewline_AgreeWhereTheFeatureIsDeclared()
    {
        var failures = new List<string>();

        foreach (var preset in SemicolonPresets)
        foreach (var backend in ParityCorpusGenerator.Backends)
        foreach (var pair in StatementPairs)
        {
            var semicolon = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, pair.Semicolon);
            var newline = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, pair.Newline);

            if (!semicolon.Equals(newline))
            {
                failures.Add(
                    $"{preset}/{backend} [{pair.Name}]: semicolon={semicolon} newline={newline}");
            }
        }

        Assert.That(failures, Is.Empty,
            "';' and a newline must mean the same thing where the feature is declared:" + Environment.NewLine +
            string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void Semicolon_IsNotASeparatorWhereTheFeatureIsNotDeclared()
    {
        // The other half of the relation. A preset that does not declare the feature must reject the
        // semicolon form; if it accepted it, the dialect would implement more than it declares, and a
        // consumer reading the feature list would be misled about the language they are getting.
        var failures = new List<string>();
        var compared = 0;

        foreach (var preset in NonSemicolonPresets)
        foreach (var backend in ParityCorpusGenerator.Backends)
        foreach (var pair in StatementPairs)
        {
            var semicolon = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, pair.Semicolon);
            var newline = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, pair.Newline);
            compared++;

            var violation = UndeclaredSemicolonViolation(semicolon, newline);
            if (violation != null)
                failures.Add($"{preset}/{backend} [{pair.Name}]: {violation}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(compared, Is.GreaterThan(0),
                "no negative-half measurement ran; this assertion would pass without checking anything");
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        });
    }

    /// <summary>
    /// The negative-half predicate, as a pure function so it can be proven able to fire.
    /// <para>
    /// Returns a description when a preset that does <b>not</b> declare semicolon-as-new-line nevertheless
    /// treats <c>;</c> as a separator: the semicolon form succeeded <em>and</em> produced the same outcome as
    /// the newline form. A failure of the newline form too is not a violation — it means the preset lacks a
    /// different capability, and the semicolon was not the deciding factor.
    /// </para>
    /// </summary>
    internal static string? UndeclaredSemicolonViolation(ParityObservation semicolon, ParityObservation newline)
    {
        if (semicolon.IsFailure)
            return null;
        return semicolon.Equals(newline)
            ? $"the semicolon form was accepted and equals the newline form ({semicolon}) although the preset " +
              "does not declare semicolon-as-new-line"
            : null;
    }

    [Test]
    public void NegativeHalfPredicate_FiresOnTheShapesItMust()
    {
        // The negative half is the half most likely to be a green blank: if the predicate never fires, the
        // loop above passes on every preset for the wrong reason. These cases are the shapes it must report.
        var accepted = ParityObservation.ForValue(3);
        var acceptedOther = ParityObservation.ForValue(6);
        var rejected = ParityObservation.ForFailure("LexerException");

        Assert.Multiple(() =>
        {
            Assert.That(UndeclaredSemicolonViolation(accepted, accepted), Is.Not.Null,
                "a semicolon form that succeeds and matches the newline form IS the violation");
            Assert.That(UndeclaredSemicolonViolation(accepted, acceptedOther), Is.Null,
                "differing outcomes mean the semicolon was not acting as a separator");
            Assert.That(UndeclaredSemicolonViolation(rejected, accepted), Is.Null,
                "a rejected semicolon form is the expected, correct behaviour");
            Assert.That(UndeclaredSemicolonViolation(rejected, rejected), Is.Null,
                "both forms failing means the preset lacks another capability, not that ';' works");
        });
    }

    [Test]
    public void Semicolon_IsTheReason_TheNonDeclaringPresetsRejectIt()
    {
        // The test above only says "the two forms are not interchangeable", which a preset that cannot
        // parse either form would satisfy for the wrong reason. This one isolates the semicolon: take an
        // expression that succeeds without it and add one, and the failure must appear. That is what makes
        // the rejection a statement about the feature rather than about the preset's overall capability.
        var isolated = new List<string>();

        foreach (var preset in NonSemicolonPresets)
        foreach (var backend in ParityCorpusGenerator.Backends)
        {
            var withoutSemicolon = ParityCorpusGenerator.Probe(
                preset, backend, WistSsaPolicy.Disabled, "1 + 2");
            var withSemicolon = ParityCorpusGenerator.Probe(
                preset, backend, WistSsaPolicy.Disabled, "1 + 2;");

            if (withoutSemicolon.IsFailure)
                continue;

            isolated.Add($"{preset}/{backend}: plain={withoutSemicolon} with-semicolon={withSemicolon}");
            Assert.That(withSemicolon.IsFailure, Is.True,
                $"{preset}/{backend}: a preset that does not declare semicolon-as-new-line accepted '1 + 2;'. " +
                "The dialect now implements more than its feature list declares.");
        }

        Assert.That(isolated, Is.Not.Empty,
            "no non-declaring preset could parse '1 + 2', so the semicolon's effect was never isolated");
    }

    [Test]
    public void WhitespaceAndParentheses_DoNotChangeTheAnswer()
    {
        var failures = new List<string>();
        var compared = 0;

        foreach (var preset in ParityCorpusGenerator.DualBackendPresets())
        foreach (var backend in ParityCorpusGenerator.Backends)
        foreach (var (name, original, transformed) in StructuralRewrites)
        {
            var before = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, original);
            var after = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, transformed);

            // Only compare where the original is meaningful for this preset. A preset that cannot lex the
            // original says nothing about the rewrite.
            if (before.IsFailure)
                continue;

            compared++;
            if (!before.Equals(after))
            {
                failures.Add($"{preset}/{backend} [{name}]: {original} = {before}, {transformed} = {after}");
            }
        }

        Assert.Multiple(() =>
        {
            Assert.That(compared, Is.GreaterThanOrEqualTo(MinimumStructuralComparisons),
                $"only {compared} structural comparisons ran. If this drops, the rewrites stopped applying to " +
                "the shipped presets and this assertion became a green blank rather than a parity claim.");
            Assert.That(failures, Is.Empty,
                "these rewrites are meaning-preserving by definition:" + Environment.NewLine +
                string.Join(Environment.NewLine, failures));
        });
    }

    [Test]
    public void CommutativeOperands_AgreeForAdditionAndMultiplication()
    {
        var failures = new List<string>();
        var compared = 0;
        var pairs = new[] { ("2 + 3", "3 + 2"), ("2 * 3", "3 * 2"), ("100.0 + 7.5", "7.5 + 100.0") };

        foreach (var preset in ParityCorpusGenerator.DualBackendPresets())
        foreach (var backend in ParityCorpusGenerator.Backends)
        foreach (var (left, right) in pairs)
        {
            var a = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, left);
            var b = ParityCorpusGenerator.Probe(preset, backend, WistSsaPolicy.Disabled, right);
            if (a.IsFailure)
                continue;
            compared++;
            if (!a.Equals(b))
                failures.Add($"{preset}/{backend}: {left} = {a}, {right} = {b}");
        }

        Assert.Multiple(() =>
        {
            Assert.That(compared, Is.GreaterThan(0),
                "no commutativity pair ran, so this assertion checks nothing");
            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        });
    }

    [Test]
    public void Harness_DetectsAChangedMeaning()
    {
        // The self-test. Each rewrite below deliberately changes the answer, so the comparison used above
        // must report a difference for it. Without this, every green result in the fixture is meaningless.
        Assert.Multiple(() =>
        {
            var mutated = ParityCorpusGenerator.Probe(
                WistLanguageDefinitions.FullDefaultNativeId, "cil", WistSsaPolicy.Disabled, "7 / 2");
            var different = ParityCorpusGenerator.Probe(
                WistLanguageDefinitions.FullDefaultNativeId, "cil", WistSsaPolicy.Disabled, "7.0 / 2.0");

            Assert.That(mutated.Type, Is.EqualTo("Int32"));
            Assert.That(different.Type, Is.EqualTo("Double"));
            Assert.That(mutated.Equals(different), Is.False,
                "the metamorphic comparison cannot distinguish Int32:3 from Double:3.5");
        });
    }

    /// <summary>
    /// Floor for <see cref="WhitespaceAndParentheses_DoNotChangeTheAnswer"/>. Measured: all four rewrites apply
    /// to all 5 dual-backend presets on both backends, i.e. 5 x 2 x 4. It exists so a future preset change that
    /// makes a rewrite inapplicable fails loudly instead of turning the parity claim into an empty loop.
    /// </summary>
    private const int MinimumStructuralComparisons = 40;

    private static readonly (string Name, string Semicolon, string Newline)[] StatementPairs =
    [
        ("one-statement", "1 + 2;", "1 + 2\n"),
        ("let-then-use", "let a = 2; a * 3", "let a = 2\na * 3"),
        ("two-lets", "let a = 2; let b = 3; a * b", "let a = 2\nlet b = 3\na * b")
    ];

    private static readonly (string Name, string Original, string Transformed)[] StructuralRewrites =
    [
        ("whitespace", "1+2", "  1  +  2  "),
        ("redundant-parens", "2 + 3 * 4", "2 + (3 * 4)"),
        ("outer-parens", "2 + 3", "(2 + 3)"),
        ("constant-fold", "2 * 3 + 1", "6 + 1")
    ];
}
