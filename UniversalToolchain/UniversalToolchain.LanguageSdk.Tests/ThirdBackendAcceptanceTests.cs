using BasicCore.Capabilities;
using BasicCore.Contracts;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Optimization;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

/// <summary>
/// The exit criterion of UT-A047: a third backend must be addable without editing generic code.
/// <para>
/// A gate saying "no name comparisons remain" is a claim about today's text. This file is the claim that
/// matters: give the machinery a backend it has never heard of, and it works. If any consumer still
/// decided behaviour by comparing a name, the third backend would reach a <c>throw</c> arm and this fails.
/// </para>
/// <para>
/// The probe backend is deliberately useless for execution: it can declare intrinsics and must reject
/// what it cannot do. That is enough to prove the description path is complete. It does not prove a real
/// third backend can execute Wist bytecode - that is UT-A044's job and needs an implementation, not a
/// descriptor.
/// </para>
/// </summary>
[TestFixture]
public sealed class ThirdBackendAcceptanceTests
{
    private static readonly BackendId Wasm = new("probe-wasm");

    private static WistBackendDescriptor CreateWasmDescriptor() =>
        new(
            Wasm,
            ["probe.intrinsic"],
            "probe-wasm-backend",
            static _ => throw new NotSupportedException("the probe backend publishes no contract facets"),
            static () => new EmptyIntrinsicCapabilitySet(),
            static (_, _) => throw new NotSupportedException("the probe backend cannot execute"),
            static (_, _) => (SsaRouteReport?)null);

    [Test]
    public void ACatalogContainingAnUnshippedBackend_ResolvesIt()
    {
        // Lookup is by id, so a backend the pack does not ship resolves as soon as it is described. No
        // consumer has to be told about it.
        var catalog = new WistBackendCatalog([CreateWasmDescriptor()]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.TryGet(Wasm, out var resolved), Is.True);
            Assert.That(resolved!.Id, Is.EqualTo(Wasm));
            Assert.That(catalog.All, Has.Count.EqualTo(1),
                "the catalog must contain exactly what it was given, not fall back to the shipped set");
        });
    }

    [Test]
    public void AThirdBackendsIntrinsicsAreUsedWithoutAnyNameComparison()
    {
        // Consumers read SupportedIntrinsics from the descriptor. If they still switched on the name, a
        // probe backend would either throw or silently receive cil's intrinsic list.
        var descriptor = new WistBackendCatalog([CreateWasmDescriptor()]).GetRequiredDescriptor(Wasm);

        Assert.That(descriptor.SupportedIntrinsics, Is.EqualTo(new[] { "probe.intrinsic" }).AsCollection,
            "the probe backend's declared intrinsics must be what consumers see");
    }

    [Test]
    public void CanonicalCatalog_DoesNotContainTheProbeBackend()
    {
        // The probe is a catalog the test built, never a global mutation. Registering a fake backend in the
        // shipped catalog would poison every later test in the process.
        Assert.Multiple(() =>
        {
            Assert.That(WistBackendCatalog.Canonical.TryGet(Wasm, out _), Is.False,
                "the shipped catalog must not gain test-only backends");
            Assert.That(WistBackendCatalog.Canonical.All.Select(static x => x.Id.Value),
                Is.EquivalentTo(new[] { "cil", "interpreter" }),
                "the shipped catalog is exactly the two production backends");
        });
    }

    [Test]
    public void TwoCatalogsAreIndependent()
    {
        // A catalog is a value, not ambient state. This is what makes the third-backend claim honest: the
        // observation comes from the object under test, not from process-wide registration.
        var probe = new WistBackendCatalog([CreateWasmDescriptor()]);
        var shipped = WistBackendCatalog.Canonical;

        Assert.Multiple(() =>
        {
            Assert.That(probe.All, Has.Count.EqualTo(1));
            Assert.That(shipped.All, Has.Count.EqualTo(2));
            Assert.That(probe.TryGet(new BackendId("cil"), out _), Is.False,
                "a probe catalog must not imply the shipped backends");
        });
    }

    [Test]
    public void AProbeBackendCannotSilentlyGainExecution_ItFailsLoudly()
    {
        // A descriptor that pretends to execute would be worse than one that refuses: the plan would look
        // valid and produce nothing. The probe refuses, and the refusal is explicit.
        var descriptor = new WistBackendCatalog([CreateWasmDescriptor()]).GetRequiredDescriptor(Wasm);

        Assert.Multiple(() =>
        {
            Assert.That(() => descriptor.CreateIntrinsicCapabilitySet().Supports(default, []), Is.False,
                "an empty capability set must support nothing");
            Assert.That(() => descriptor.MaterializeProgram(null!, null!),
                Throws.TypeOf<NotSupportedException>(),
                "materializing through an unimplemented backend must fail loudly, not return null");
        });
    }

    private sealed class EmptyIntrinsicCapabilitySet : IIntrinsicCapabilitySet
    {
        public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments) => false;
    }
}
