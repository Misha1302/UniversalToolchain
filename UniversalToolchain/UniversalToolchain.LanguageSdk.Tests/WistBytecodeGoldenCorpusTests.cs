using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// Snapshot of the bytecode the current lowering dispatch produces.
/// <para>
/// The follow-up to the module-descriptor work replaces a thirteen-term <c>||</c> chain and 27
/// <c>RequireModule(...)</c> calls with the ownership registry. That is a behaviour-preserving
/// refactor, and "behaviour-preserving" has to be observable rather than asserted: the artifact at the
/// semantic-to-bytecode boundary is the thing that must not move.
/// </para>
/// <para>
/// The corpus is deliberately non-commuting. A single arithmetic expression would pass even if the
/// dispatch picked the wrong lowering owner, because only one owner participates. This source mixes
/// scopes, variables, arithmetic, comparison, boolean logic, conditional control flow and loops, so an
/// ordering or ownership mistake changes the instruction sequence.
/// </para>
/// </summary>
[TestFixture]
public sealed class WistBytecodeGoldenCorpusTests
{
    private const string MultiFeatureSource = """
        let sum = 0
        let limit = 5
        for (let i = 1) (i <= limit) (i = i + 1) (
            sum = sum + i * 2
        )
        if (sum > 20) then (sum) else (0 - sum)
        """;

    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void MultiFeatureLowering_MatchesPinnedBytecode()
    {
        var expected = GoldenBytecode.Load("full-default|multifeature");
        var actual = Lower(MultiFeatureSource);

        Assert.That(actual, Is.EqualTo(expected),
            "Lowering output changed for the multi-feature corpus. If this is an intentional " +
            "lowering change, regenerate the golden file and explain why in the commit message; " +
            "if this came from a dispatch refactor, the refactor is not behaviour-preserving.");
    }

    [Test]
    public void Lowering_IsDeterministicAcrossRepeatedRuns()
    {
        // Rendering must not depend on HashSet iteration order, or the golden file becomes a coin flip.
        var first = Lower(MultiFeatureSource);
        var second = Lower(MultiFeatureSource);
        var third = Lower(MultiFeatureSource);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(third, Is.EqualTo(first));
        });
    }

    [Test]
    public void Lowering_IsIndependentOfRuntimeInstance()
    {
        // A fresh runtime is a fresh capability catalog and resolver; the bytecode must not depend on
        // which instance happened to be created first.
        var first = Lower(MultiFeatureSource, freshRuntime: false);
        var second = Lower(MultiFeatureSource, freshRuntime: true);

        Assert.That(second, Is.EqualTo(first));
    }

    private static string Lower(string source, bool freshRuntime = false)
    {
        var package = new WistLanguageFeaturePackage();
        var definition = WistFacadeLanguageDefinitionFactory.FromPreset(
            WistLanguageDefinitions.FullDefaultId,
            Interpreter.Value,
            WistFacadeSsaPolicy.Disabled);
        var plan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition)
            .GetRequiredPlan();

        var recorder = new BytecodeRecorder();
        using var runtime = LanguageRuntime.Create(
            plan,
            new ILanguageRouteComponentSource[] { package },
            new LanguageRuntimeOptions().AddRouteListener(recorder));

        var result = runtime.Run(new LanguageExecutionRequest(source, Interpreter));
        Assert.That(result.Value, Is.Not.Null, $"the corpus source did not execute: {source}");
        Assert.That(recorder.Bytecodes, Has.Count.EqualTo(1),
            "exactly one semantic-to-bytecode transformation must occur per execution");
        return recorder.Bytecodes[0];
    }

    private sealed class BytecodeRecorder : ILanguageArtifactRouteListener
    {
        public List<string> Bytecodes { get; } = [];

        public void AfterTransformation(LanguageArtifactRouteObservationContext observation)
        {
            ArgumentNullException.ThrowIfNull(observation);
            if (observation.Artifact is LanguageArtifact<WistBytecodeArtifact> artifact)
                Bytecodes.Add(CanonicalBytecodeText.Render(artifact.Value.Bytecode));
        }
    }
}
