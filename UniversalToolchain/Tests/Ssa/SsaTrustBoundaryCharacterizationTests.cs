using System.Reflection;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace Tests.Ssa;

/// <summary>
/// Negative-control research characterization of the current SSA purity trust boundary.
/// A passing assertion about an intentionally unsafe rewrite is NOT a soundness guarantee.
/// See internal-docs/research/ideal-ut-2026-10-08/SSA_TRUST_PREREG_2026-10-12.md.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class SsaTrustBoundaryCharacterizationTests
{
    private static int _observableSideEffects;

    [Test]
    public void TruthfulPureVerifiedMethod_IsRemovedWhenResultIsUnused()
    {
        var method = Method(nameof(ActuallyPure));
        var (artifact, descriptors, descriptor) = Build(method);

        var optimized = Optimize(artifact, descriptors);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor.TrustLevel, Is.EqualTo(SemanticTrustLevel.VerifiedPlugin));
            Assert.That(descriptor.Effects.IsPure, Is.True);
            Assert.That(ContainsCandidateCall(optimized), Is.False);
        });
        TestContext.Progress.WriteLine("case=truthful_pure original_call=1 optimized_call=0 oracle_effect_delta=0");
    }

    [Test]
    public void SelfDeclaredUncheckedEffectfulMethod_IsRetained()
    {
        var (artifact, descriptors, descriptor) = Build(Method(nameof(EffectfulUntrusted)));
        var optimized = Optimize(artifact, descriptors);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.TrustLevel, Is.EqualTo(SemanticTrustLevel.UserProvidedUnchecked));
            Assert.That(ContainsCandidateCall(optimized), Is.True);
        });
        TestContext.Progress.WriteLine("case=unchecked_effectful original_call=1 optimized_call=1");
    }

    [Test]
    public void UnannotatedEffectfulMethod_IsRetained()
    {
        var (artifact, descriptors, descriptor) = Build(Method(nameof(EffectfulUnannotated)));
        var optimized = Optimize(artifact, descriptors);

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.TrustLevel, Is.EqualTo(SemanticTrustLevel.ExternalUnknown));
            Assert.That(ContainsCandidateCall(optimized), Is.True);
        });
        TestContext.Progress.WriteLine("case=unknown_effectful original_call=1 optimized_call=1");
    }

    [Test]
    public void FalselySelfDeclaredVerifiedPurity_CanEraseAnObservableEffect()
    {
        var method = Method(nameof(EffectfulFalselyVerified));

        // Independent observation from actual managed method, not from the SSA optimizer.
        _observableSideEffects = 0;
        var returned = (int)method.Invoke(null, [5])!;
        var oracleDelta = _observableSideEffects;
        Assert.Multiple(() =>
        {
            Assert.That(returned, Is.EqualTo(6));
            Assert.That(oracleDelta, Is.EqualTo(1));
        });

        var (artifact, descriptors, descriptor) = Build(method);
        Assert.That(ContainsCandidateCall(artifact), Is.True);

        // H-B: current UT optimizer and its structural re-verification.
        var optimized = Optimize(artifact, descriptors);
        var callAfter = ContainsCandidateCall(optimized);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor.TrustLevel, Is.EqualTo(SemanticTrustLevel.VerifiedPlugin));
            Assert.That(descriptor.Effects.IsPure, Is.True);
            Assert.That(callAfter, Is.False, "Characterization: a forgeable purity claim permits SSA dead-call elimination.");
        });

        // H-A: straightforward host-owned approval gate; intentionally conservative.
        // This is a control for trust authorization, not an implementation of N+1.
        var approval = new HashSet<MethodInfo> { Method(nameof(ActuallyPure)) };
        var baseline = approval.Contains(method) ? Optimize(artifact, descriptors) : artifact;
        Assert.That(ContainsCandidateCall(baseline), Is.True,
            "A host-owned exact-method allowlist refuses a self-asserted VerifiedPlugin label.");

        // Explicit scientific failure classification: structural acceptance != effect preservation.
        var lostEffect = oracleDelta != 0 && !callAfter;
        Assert.That(lostEffect, Is.True);
        TestContext.Progress.WriteLine(
            $"case=self_asserted_verified_effectful original_call=1 optimized_call={(callAfter ? 1 : 0)} " +
            $"oracle_effect_delta={oracleDelta} baseline_host_approved=0 baseline_call=1 " +
            $"semantic_safety={(lostEffect ? "FAIL_SAFE" : "PRESERVED")} structural_acceptance=PASS");
    }

    private static MethodInfo Method(string name) =>
        typeof(SsaTrustBoundaryCharacterizationTests).GetMethod(
            name, BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Missing fixture method: {name}");

    private static (
        SsaArtifact Artifact,
        SemanticDescriptorSet Descriptors,
        CallableDescriptor Descriptor) Build(MethodInfo method)
    {
        Assert.That(
            SsaManagedCallables.TryCreateMethod(
                method,
                consumesInstanceReceiver: false,
                out var callable,
                out var descriptor,
                out var error),
            Is.True,
            error);

        var argument = new SsaValue(new SsaValueId("%argument"), SsaTypes.Int32);
        var ignored = new SsaValue(new SsaValueId("%ignored"), SsaTypes.Int32);
        var returned = new SsaValue(new SsaValueId("%returned"), SsaTypes.Int32);
        var block = new SsaBlock(
            new SsaBlockId("entry"),
            instructions:
            [
                SsaConstantMaterializer.Int32(new SsaOperationId("const.input"), argument, 5),
                new SsaCall(
                    new SsaOperationId("call.candidate"),
                    callable,
                    [argument.Id],
                    [ignored]),
                SsaConstantMaterializer.Int32(new SsaOperationId("const.output"), returned, 0)
            ],
            terminator: SsaTerminator.Return([returned.Id]));

        var artifact = new SsaArtifact(
            new SsaModule(
                new SsaModuleId("trust.boundary.fixture"),
                [
                    new SsaFunction(
                        new SsaFunctionId("main"),
                        new SsaBlockId("entry"),
                        [block],
                        returnType: SsaTypes.Int32)
                ]),
            new SsaManagedCallableBindingSet(
                [new SsaManagedCallableBinding(callable, descriptor, method)]));

        var descriptors = new SemanticDescriptorSet(
            types: [new SemanticTypeDescriptor(SsaSemanticTypes.Int32)],
            callables: [descriptor]);

        return (artifact, descriptors, descriptor);
    }

    private static SsaArtifact Optimize(SsaArtifact artifact, SemanticDescriptorSet descriptors) =>
        new SsaOptimizerPipeline(
                [new SsaDeadPureInstructionEliminationPass(
                    SsaCoreDescriptors.ConstantMaterialization, descriptors)],
                SsaCoreDescriptors.ConstantMaterialization,
                descriptors)
            .Run(artifact, new IrPipelineContext())
            .Artifact.As<SsaArtifact>();

    private static bool ContainsCandidateCall(SsaArtifact artifact) =>
        artifact.Module.Functions.SelectMany(static f => f.Blocks)
            .SelectMany(static b => b.Calls)
            .Any(static c => c.Id == new SsaOperationId("call.candidate"));

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        TrustLevel = SemanticTrustLevel.VerifiedPlugin)]
    private static int ActuallyPure(int x) => x + 1;

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        TrustLevel = SemanticTrustLevel.VerifiedPlugin)]
    private static int EffectfulFalselyVerified(int x)
    {
        _observableSideEffects++;
        return x + 1;
    }

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        TrustLevel = SemanticTrustLevel.UserProvidedUnchecked)]
    private static int EffectfulUntrusted(int x)
    {
        _observableSideEffects++;
        return x + 1;
    }

    private static int EffectfulUnannotated(int x)
    {
        _observableSideEffects++;
        return x + 1;
    }
}
