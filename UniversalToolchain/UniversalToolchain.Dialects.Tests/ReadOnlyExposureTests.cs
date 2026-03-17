using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests;

public class ReadOnlyExposureTests
{
    [Test]
    public void ModulePolicy_CollectionsAreReadOnly()
    {
        var policy = new ModulePolicy(["A"], ["B"]);

        Assert.That(policy.IncludedModules, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<string>>());
        Assert.That(policy.ExcludedModules, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<string>>());
    }

    [Test]
    public void CapabilityPolicy_MapIsReadOnly()
    {
        var policy = new CapabilityPolicy([
            new KeyValuePair<string, bool>("capability-x", true)
        ]);

        Assert.That(policy.Capabilities, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyDictionary<string, bool>>());
    }

    [Test]
    public void DialectValidationResult_DiagnosticsAreReadOnly()
    {
        var result = new DialectValidationResult([
            new DialectDiagnostic("D001", "warning", DialectDiagnosticSeverity.Warning)
        ]);

        Assert.That(result.Diagnostics, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<DialectDiagnostic>>());
    }
}
