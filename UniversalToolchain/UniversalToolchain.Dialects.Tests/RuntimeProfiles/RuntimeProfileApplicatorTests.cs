using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests.RuntimeProfiles;

public sealed class RuntimeProfileApplicatorTests
{
    [Test]
    public void Apply_AddsMissingDefaultsAndRecordsProvenance()
    {
        var profile = new RuntimeProfileDefinition(
            "arith-interpreter",
            defaultModules: ["Arithmetic", "Numbers"],
            defaultBackends: [new DialectBackendId("interpreter")],
            defaultOptimizers: ["LocalVariables"],
            defaultSecurityProfile: SecurityProfile.Restricted,
            defaultCapabilities: [new KeyValuePair<string, bool>("safe-math", true)]);

        var result = new RuntimeProfileApplicator().Apply("dialect Profiled\nuse Arithmetic", profile);

        Assert.Multiple(() =>
        {
            Assert.That(result.CanCompose, Is.True);
            Assert.That(result.SourceText, Does.Contain("use Numbers"));
            Assert.That(result.SourceText, Does.Contain("backend interpreter"));
            Assert.That(result.SourceText, Does.Contain("enable optimizer LocalVariables for any"));
            Assert.That(result.SourceText, Does.Contain("security restricted"));
            Assert.That(result.SourceText, Does.Contain("capability safe-math = true"));
            Assert.That(
                result.Provenance.Select(static x => $"{x.DirectiveKind}:{x.DirectiveName}:{x.Source}"),
                Is.SupersetOf(new[]
                {
                    "use:Arithmetic:source",
                    "use:Numbers:profile:arith-interpreter",
                    "backend:interpreter:profile:arith-interpreter"
                }));
        });
    }

    [Test]
    public void Apply_StrictModeReportsProfileSourceConflicts()
    {
        var profile = new RuntimeProfileDefinition(
            "strict",
            defaultModules: ["Arithmetic"],
            defaultBackends: [new DialectBackendId("interpreter")]);

        var result = new RuntimeProfileApplicator().Apply(
            "dialect Conflicting\nexclude Arithmetic\nbackend interpreter disable",
            profile,
            RuntimeProfileOverridePolicy.StrictNoConflicts);

        Assert.Multiple(() =>
        {
            Assert.That(result.CanCompose, Is.False);
            Assert.That(result.Diagnostics.Count(static x => x.Code == "R301" && x.Severity == DialectDiagnosticSeverity.Error), Is.EqualTo(2));
        });
    }

    [Test]
    public void CatalogRejectsDuplicateProfilesAndResolvesByName()
    {
        var profile = new RuntimeProfileDefinition("default");
        var catalog = new RuntimeProfileCatalog([profile]);

        Assert.Multiple(() =>
        {
            Assert.That(catalog.TryGet("default", out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(profile));
            Assert.That(
                () => new RuntimeProfileCatalog([profile, new RuntimeProfileDefinition("default")]),
                Throws.TypeOf<InvalidOperationException>().With.Message.Contain("default"));
        });
    }

    [Test]
    public void BuilderCreatesDeduplicatedProfileAndCatalog()
    {
        var catalog = RuntimeProfileCatalogBuilder
            .Create()
            .Add(_ => RuntimeProfileDefinitionBuilder
                .Create("release")
                .Describe("Release defaults")
                .UseModules("Arithmetic", "Arithmetic", "Numbers")
                .EnableBackend("cil")
                .EnableOptimizer("SsaPreview")
                .Security(SecurityProfile.Restricted)
                .Capability("safe-math")
                .Capability("native-interop", enabled: false))
            .Build();

        Assert.That(catalog.TryGet("release", out var profile), Is.True);
        Assert.That(profile, Is.Not.Null);
        var resolved = profile!;
        Assert.Multiple(() =>
        {
            Assert.That(resolved.Description, Is.EqualTo("Release defaults"));
            Assert.That(resolved.DefaultModules, Is.EqualTo(new[] { "Arithmetic", "Numbers" }));
            Assert.That(resolved.DefaultBackends.Select(static x => x.Value), Is.EqualTo(new[] { "cil" }));
            Assert.That(resolved.DefaultOptimizers, Is.EqualTo(new[] { "SsaPreview" }));
            Assert.That(resolved.DefaultSecurityProfile, Is.EqualTo(SecurityProfile.Restricted));
            Assert.That(resolved.DefaultCapabilities["safe-math"], Is.True);
            Assert.That(resolved.DefaultCapabilities["native-interop"], Is.False);
        });
    }
}
