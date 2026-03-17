using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class ApplyModeGuardContractsTests
{
    [Test]
    public void Build_NullRuntimeComposition_ThrowsArgumentNullException()
    {
        var builder = new DialectApplyDescriptionBuilder();

        Assert.Throws<ArgumentNullException>(() => builder.Build(null!));
    }

    [Test]
    public void Constructor_EmptyDialectName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new DialectApplyDescription(
                " ",
                [],
                [],
                [],
                [],
                []));

        Assert.That(ex!.Message, Does.Contain("Dialect name must not be empty."));
    }

    [Test]
    public void Constructor_SnapshotsInputCollections()
    {
        var frontendModules = new List<Type> { typeof(FakeFrontendModule) };
        var irModules = new List<Type>();
        var optimizers = new List<Type>();
        var backends = new List<string> { "InterpreterBackend" };
        var intrinsics = new List<DialectApplyIntrinsicPermission>
        {
            new("add_i32", DialectBackendTarget.Any)
        };

        var description = new DialectApplyDescription(
            "x",
            frontendModules,
            irModules,
            optimizers,
            backends,
            intrinsics);

        frontendModules.Add(typeof(FakeOptimizerModule));
        backends.Add("CilBackend");
        intrinsics.Add(new DialectApplyIntrinsicPermission("sub_i32", DialectBackendTarget.Any));

        Assert.Multiple(() =>
        {
            Assert.That(description.FrontendModules.Select(x => x.Name), Is.EqualTo(new[] { nameof(FakeFrontendModule) }));
            Assert.That(description.RuntimeBackends, Is.EqualTo(new[] { "InterpreterBackend" }));
            Assert.That(description.Intrinsics.Select(x => x.Name), Is.EqualTo(new[] { "add_i32" }));
        });
    }

    [Test]
    public void Constructor_ExposesReadOnlyCollections()
    {
        var description = new DialectApplyDescription(
            "x",
            [typeof(FakeFrontendModule)],
            [],
            [],
            ["InterpreterBackend"],
            [new DialectApplyIntrinsicPermission("add_i32", DialectBackendTarget.Any)]);

        Assert.Multiple(() =>
        {
            Assert.That(description.FrontendModules, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<Type>>());
            Assert.That(description.RuntimeBackends, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<string>>());
            Assert.That(description.Intrinsics, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<DialectApplyIntrinsicPermission>>());
        });
    }

    private sealed class FakeFrontendModule;

    private sealed class FakeOptimizerModule;
}
