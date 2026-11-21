using System.Globalization;
using System.Text.Json;
using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

/// <summary>
/// Measures the parity corpus and keeps the committed <c>ParityCorpus.json</c> honest (UT-A050).
/// <para>
/// The corpus is committed data, not a computed expectation, for the same reason the plan-hash goldens
/// are: a corpus derived at test time agrees with whatever the code currently does, so it can never fail.
/// </para>
/// <para>
/// This fixture therefore does not assert "the measurement equals the measurement". It asserts that the
/// committed file equals what a fresh measurement produces <b>today</b>, so a behaviour change that was not
/// accompanied by a deliberate corpus update fails the suite. The measurement itself refuses to run when it
/// already contradicts the parity claim: a value divergence between the backends under <c>Disabled</c> or
/// <c>Prefer</c>, a <c>Prefer</c> that changes the answer, or a durable path that disagrees with the executor
/// path, all throw before anything is compared.
/// </para>
/// <para>
/// Regenerating is a deliberate act: set <c>WIST_REGENERATE_PARITY_CORPUS=1</c>, read the diff, and decide
/// whether that diff is the change you meant to make.
/// </para>
/// </summary>
[TestFixture]
public sealed class ParityCorpusGenerator
{
    /// <summary>
    /// The measured source set. Every entry is a construct at least one shipped preset supports, and the
    /// failures are deliberate: a corpus made only of successes cannot distinguish "the backends agree"
    /// from "both happen to be unable to do anything interesting".
    /// </summary>
    internal static readonly string[] Sources =
    [
        "2 + 3",
        "2 + 3 * 4",
        "(2 + 3) * 4",
        "10 - 3 - 2",
        "-5 + 3",
        "100 / 7",
        "100.0 / 7.0",
        "2147483647 + 1",
        "1000000 * 1000000",
        "1 / 0",
        "1.0 / 0.0",
        "0.0 / 0.0",
        "2 < 3",
        "2 == 2",
        "true",
        "true and false",
        "let a = 2\nlet b = 3\na * b",
        "let s = 0\nfor (let i = 1) (i <= 5) (i = i + 1) (s = s + i)\ns"
    ];

    internal static readonly string[] Backends = ["cil", "interpreter"];

    /// <summary>
    /// The root-cause type name <c>Require</c> produces when the SSA pack does not model an AIR intrinsic.
    /// Named here so the corpus test can assert the <em>reason</em> for a divergence instead of just
    /// accepting that one exists.
    /// </summary>
    internal const string SsaConversionFailure = "AirToSsaConversionException";

    /// <summary>
    /// Environment switch that turns the check into a regeneration. Deliberately not a test attribute:
    /// an <c>[Explicit]</c> fixture cannot be part of the canonical test contract, and a golden that is
    /// never compared is not a golden.
    /// </summary>
    internal const string RegenerateVariable = "WIST_REGENERATE_PARITY_CORPUS";

    [Test]
    public void CommittedCorpus_EqualsFreshMeasurement()
    {
        var measured = Measure();
        var serialized = Serialize(measured);

        if (Environment.GetEnvironmentVariable(RegenerateVariable) == "1")
        {
            File.WriteAllText(CorpusPath(), serialized);
            TestContext.Out.WriteLine($"regenerated {measured.Rows.Count} rows at {CorpusPath()}");
            return;
        }

        Assert.That(File.Exists(CorpusPath()), Is.True,
            $"the committed corpus is missing at {CorpusPath()}. Set {RegenerateVariable}=1 to write it.");

        var committed = File.ReadAllText(CorpusPath());

        // Compare the deserialized rows rather than the raw text: formatting is not the subject, behaviour is.
        var committedCorpus = JsonSerializer.Deserialize<ParityCorpus>(committed, JsonOptions)!;
        var committedKeys = committedCorpus.Rows
            .Select(static row => RowKey(row.Preset, row.Source))
            .ToArray();
        var measuredKeys = measured.Rows
            .Select(static row => RowKey(row.Preset, row.Source))
            .ToArray();

        var drift = new List<string>();
        if (!committedKeys.SequenceEqual(measuredKeys, StringComparer.Ordinal))
        {
            drift.Add(
                "the row set changed. committed-only: " +
                $"[{string.Join(", ", committedKeys.Except(measuredKeys, StringComparer.Ordinal))}]; " +
                $"measured-only: [{string.Join(", ", measuredKeys.Except(committedKeys, StringComparer.Ordinal))}]");
        }

        var committedByKey = committedCorpus.Rows.ToDictionary(
            static row => RowKey(row.Preset, row.Source), StringComparer.Ordinal);
        foreach (var row in measured.Rows)
        {
            var key = RowKey(row.Preset, row.Source);
            if (!committedByKey.TryGetValue(key, out var recorded))
                continue;

            if (recorded.Expected != row.Expected)
                drift.Add($"{key}: expected committed={recorded.Expected} measured={row.Expected}");

            foreach (var backend in Backends)
            {
                var recordedRequire = recorded.RequireOutcomes[backend];
                var measuredRequire = row.RequireOutcomes[backend];
                if (recordedRequire != measuredRequire)
                {
                    drift.Add(
                        $"{key}/{backend}: Require committed={recordedRequire} measured={measuredRequire}");
                }
            }
        }

        if (committedCorpus.DeclaredRequireDivergences != measured.DeclaredRequireDivergences)
        {
            drift.Add(
                $"DeclaredRequireDivergences committed={committedCorpus.DeclaredRequireDivergences} " +
                $"measured={measured.DeclaredRequireDivergences}");
        }

        Assert.That(drift, Is.Empty,
            "measured behaviour no longer matches the committed corpus. If the change is intended, " +
            $"regenerate with {RegenerateVariable}=1 and review the diff:" + Environment.NewLine +
            string.Join(Environment.NewLine, drift));
    }

    internal static ParityCorpus Measure()
    {
        var rows = DualBackendPresets()
            .SelectMany(preset => Sources.Select(source => MeasureRow(preset, source)))
            .ToArray();

        return new ParityCorpus(
            Schema: 1,
            DeclaredRequireDivergences: rows.Count(static row => row.RequireOutcomes.Values.Distinct().Count() > 1),
            Rows: rows);
    }

    internal static string Serialize(ParityCorpus corpus) =>
        JsonSerializer.Serialize(corpus, JsonOptions) + Environment.NewLine;

    private static ParityRow MeasureRow(string preset, string source)
    {
        var disabled = Backends.ToDictionary(
            backend => backend,
            backend => Probe(preset, backend, WistSsaPolicy.Disabled, source),
            StringComparer.Ordinal);
        RequireAgreement(preset, source, "Disabled", disabled);

        // Evaluate and Compile reach different implementations of the same AIR. Both must agree with the
        // recorded outcome, or the corpus pins only one of the two execution paths.
        foreach (var backend in Backends)
        {
            RequireAgreement(
                preset,
                source,
                $"Compile-vs-Evaluate/{backend}",
                disabled[backend],
                ProbeCompiled(preset, backend, WistSsaPolicy.Disabled, source));
        }

        // Prefer may only fall back to AIR. It must not change the answer, on either backend.
        var prefer = Backends.ToDictionary(
            backend => backend,
            backend => Probe(preset, backend, WistSsaPolicy.Prefer, source),
            StringComparer.Ordinal);
        RequireAgreement(preset, source, "Prefer", prefer);
        foreach (var backend in Backends)
            RequireAgreement(preset, source, $"Prefer-vs-Disabled/{backend}", disabled[backend], prefer[backend]);

        // Require is NOT asserted equal across backends. It is a capability axis: it refuses when the
        // AIR carry an intrinsic the SSA pack does not model, and the two backends emit different AIR.
        // The divergence is recorded as data, and ParityCorpusTests refuses to let it grow silently.
        var require = Backends.ToDictionary(
            backend => backend,
            backend => Probe(preset, backend, WistSsaPolicy.Require, source),
            StringComparer.Ordinal);

        return new ParityRow(preset, source, disabled["cil"], require);
    }

    private static void RequireAgreement(
        string preset,
        string source,
        string relation,
        ParityObservation left,
        ParityObservation right)
    {
        if (left.Equals(right))
            return;

        throw new InvalidOperationException(
            $"value parity already broken before the corpus was written: {preset} | {Escape(source)} | {relation} -> " +
            $"{left} vs {right}. Fix the divergence, or record it deliberately with a rationale; " +
            "do not bake it into a golden.");
    }

    private static void RequireAgreement(
        string preset,
        string source,
        string relation,
        IReadOnlyDictionary<string, ParityObservation> observations)
    {
        var distinct = observations.Values.Distinct().ToArray();
        if (distinct.Length == 1)
            return;

        throw new InvalidOperationException(
            $"value parity already broken before the corpus was written: {preset} | {Escape(source)} | {relation} -> " +
            string.Join(" vs ", observations.Select(pair => $"{pair.Key}={pair.Value}")) +
            ". Fix the divergence, or record it deliberately with a rationale; do not bake it into a golden.");
    }

    internal static ParityObservation Probe(string preset, string backend, WistSsaPolicy policy, string source)
    {
        try
        {
            using var wist = WistEngine.Create(new WistEngineOptions
            {
                DialectSource = WistDialectSource.FromShippedPreset(preset),
                BackendId = backend,
                Optimization = new WistOptimizationOptions { Ssa = new WistSsaOptions { Policy = policy } }
            });

            return ParityObservation.ForValue(wist.Evaluate<object>(source));
        }
        catch (Exception exception)
        {
            return ParityObservation.ForFailure(RootCause(exception).GetType().Name);
        }
    }

    /// <summary>
    /// The same source through the <b>second</b> execution path.
    /// <para>
    /// <c>Evaluate</c> and <c>Compile</c> reach different code: <c>Evaluate</c> runs the registered artifact
    /// executor, while <c>Compile</c> materializes a durable program and invokes it. Both execute the same
    /// AIR, but the two implementations are independent, so a change to one can leave the other behind.
    /// A parity corpus that measured only <c>Evaluate</c> would report full agreement while the durable
    /// path silently diverged.
    /// </para>
    /// </summary>
    internal static ParityObservation ProbeCompiled(string preset, string backend, WistSsaPolicy policy, string source)
    {
        try
        {
            using var wist = WistEngine.Create(new WistEngineOptions
            {
                DialectSource = WistDialectSource.FromShippedPreset(preset),
                BackendId = backend,
                Optimization = new WistOptimizationOptions { Ssa = new WistSsaOptions { Policy = policy } }
            });

            var program = wist.Compile<Func<object>>(source);
            return ParityObservation.ForValue(program.CompiledDelegate());
        }
        catch (Exception exception)
        {
            return ParityObservation.ForFailure(RootCause(exception).GetType().Name);
        }
    }

    /// <summary>
    /// The cause, not the envelope. The two backends legitimately differ in how they wrap an execution
    /// failure: the interpreter reports instruction context, the CIL path reports the reflection frame.
    /// The facade's documented compatibility key is <c>FailureKind</c> and diagnostic codes, not the outer
    /// exception type, so comparing envelopes would test the wrapper instead of the semantic claim.
    /// </summary>
    internal static Exception RootCause(Exception exception)
    {
        var current = exception;
        while (current.InnerException != null)
            current = current.InnerException;
        return current;
    }

    internal static string[] DualBackendPresets() =>
        WistLanguageDefinitions.PresetIds
            .Where(static id => WistLanguageDefinitions.Create(id).Backends.Count == 2)
            .ToArray();

    internal static string Escape(string source) => source.Replace("\n", "\\n", StringComparison.Ordinal);

    internal static string RowKey(string preset, string source) => $"{preset}|{Escape(source)}";

    internal static string CorpusPath() =>
        Path.Combine(PublicFacadeDirectory(), "ParityCorpus.json");

    internal static string PublicFacadeDirectory()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "UniversalToolchain", "Wist.sln")))
            {
                return Path.Combine(
                    directory.FullName,
                    "UniversalToolchain",
                    "UniversalToolchain.Dialects.Tests",
                    "Wist",
                    "PublicFacade");
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root with UniversalToolchain/Wist.sln was not found.");
    }

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
}

/// <summary>Committed parity corpus: one row per (preset, source).</summary>
internal sealed record ParityCorpus(
    int Schema,
    int DeclaredRequireDivergences,
    IReadOnlyList<ParityRow> Rows);

/// <summary>
/// <see cref="Expected"/> is the <c>Disabled</c> outcome, which both backends must share. It carries the
/// exact CLR type as well as the value, so a silent <c>Double</c>/<c>Int32</c> drift fails.
/// <para>
/// <see cref="RequireOutcomes"/> is per backend because <c>Require</c> is a capability axis, not a
/// semantic one: it refuses where the AIR carry an intrinsic the SSA pack does not model.
/// </para>
/// </summary>
internal sealed record ParityRow(
    string Preset,
    string Source,
    ParityObservation Expected,
    IReadOnlyDictionary<string, ParityObservation> RequireOutcomes);

/// <summary>
/// A measured outcome: <see cref="Type"/> + <see cref="Value"/> for success, <see cref="Failure"/> for the
/// root cause of a failure. Never both.
/// </summary>
internal sealed record ParityObservation(string? Type, string? Value, string? Failure)
{
    /// <summary>
    /// Derived, never serialized. Writing it into the corpus would put a field in the golden that
    /// deserialization ignores, so an editor could "fix" it and change nothing.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsFailure => Failure != null;

    public static ParityObservation ForValue(object? value) => value switch
    {
        null => new ParityObservation("null", "null", null),
        double d when double.IsNaN(d) => new ParityObservation("Double", "NaN", null),
        double d when double.IsPositiveInfinity(d) => new ParityObservation("Double", "+Inf", null),
        double d when double.IsNegativeInfinity(d) => new ParityObservation("Double", "-Inf", null),
        IFormattable formattable => new ParityObservation(
            value.GetType().Name,
            formattable.ToString(null, CultureInfo.InvariantCulture),
            null),
        _ => new ParityObservation(value.GetType().Name, value.ToString() ?? "", null)
    };

    public static ParityObservation ForFailure(string rootCause) => new(null, null, rootCause);

    public void Deconstruct(out string? type, out string? value, out string? failure)
    {
        type = Type;
        value = Value;
        failure = Failure;
    }

    public override string ToString() => IsFailure ? $"failure({Failure})" : $"value({Type}:{Value})";
}
