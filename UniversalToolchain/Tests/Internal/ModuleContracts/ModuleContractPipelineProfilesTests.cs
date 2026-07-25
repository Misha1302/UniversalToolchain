using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class ModuleContractPipelineProfilesTests
{
    [Test]
    public void Warn_IsExplicitObservationProfile()
    {
        var options = ModuleContractPipelineProfiles.Warn;

        Assert.That(options.BytecodeProfile, Is.EqualTo(VerificationSeverityProfile.Warn));
        Assert.That(options.AirProfile, Is.EqualTo(VerificationSeverityProfile.Warn));
        Assert.That(options.EnforcementPolicy.RequireNewModulesDeclared, Is.False);
        Assert.That(options.BackendPolicy, Is.EqualTo(AirBackendPolicy.CapabilityGated));
        Assert.That(options.VerifyLegacyBytecodeOperationNames, Is.False);
    }

    [Test]
    public void StrictEnforced_RejectsUndeclaredNewModules()
    {
        var options = ModuleContractPipelineProfiles.StrictEnforced;

        Assert.That(options.BytecodeProfile, Is.EqualTo(VerificationSeverityProfile.Strict));
        Assert.That(options.AirProfile, Is.EqualTo(VerificationSeverityProfile.Strict));
        Assert.That(options.EnforcementPolicy.RequireNewModulesDeclared, Is.True);
        Assert.That(options.BackendPolicy, Is.EqualTo(AirBackendPolicy.CapabilityGated));
        Assert.That(options.VerifyLegacyBytecodeOperationNames, Is.False);
    }

    [Test]
    public void StrictUniversalInterpreter_UsesUniversalInterpreterBackendPolicy()
    {
        var options = ModuleContractPipelineProfiles.StrictUniversalInterpreter;

        Assert.That(options.BytecodeProfile, Is.EqualTo(VerificationSeverityProfile.Strict));
        Assert.That(options.AirProfile, Is.EqualTo(VerificationSeverityProfile.Strict));
        Assert.That(options.EnforcementPolicy.RequireNewModulesDeclared, Is.True);
        Assert.That(options.BackendPolicy, Is.EqualTo(AirBackendPolicy.UniversalInterpreter));
        Assert.That(options.VerifyLegacyBytecodeOperationNames, Is.False);
    }
}

[TestFixture]
public sealed class ModuleContractVerificationOptionsTests
{
    [Test]
    public void EnabledVerification_RejectsNullSink()
    {
        var options = new ModuleContractVerificationOptions
        {
            Mode = ModuleContractVerificationMode.Warn,
            PipelineOptions = ModuleContractPipelineProfiles.Warn,
            DiagnosticSink = NullModuleContractDiagnosticSink.Instance
        };

        Assert.Throws<ArgumentException>(() => options.SnapshotValidated());
    }

    [Test]
    public void WarnProfile_RequiresAndPreservesObservableSink()
    {
        var sink = new InMemoryModuleContractDiagnosticSink();
        var snapshot = ModuleContractVerificationOptions.Warn(sink).SnapshotValidated();
        var diagnostic = new ToolchainDiagnostic(
            "UT-TEST",
            ToolchainDiagnosticSeverity.Warning,
            "observable warning",
            null,
            []);

        new ModuleContractDiagnosticPolicy(snapshot.DiagnosticSink)
            .ReportAndThrowIfErrors("test", [diagnostic]);

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.PipelineOptions.Enabled, Is.True);
            Assert.That(snapshot.PipelineOptions.AirProfile, Is.EqualTo(VerificationSeverityProfile.Warn));
            Assert.That(sink.Batches, Has.Count.EqualTo(1));
            Assert.That(sink.Batches[0].Diagnostics.Single().Message, Is.EqualTo("observable warning"));
        });
    }

    [Test]
    public void StrictProfile_ThrowsTypedVerificationException()
    {
        var sink = new InMemoryModuleContractDiagnosticSink();
        var diagnostic = new ToolchainDiagnostic(
            "UT-TEST",
            ToolchainDiagnosticSeverity.Error,
            "blocking error",
            null,
            []);

        var exception = Assert.Throws<ModuleContractVerificationException>(() =>
            new ModuleContractDiagnosticPolicy(sink).ReportAndThrowIfErrors("air", [diagnostic]));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Stage, Is.EqualTo("air"));
            Assert.That(exception.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(sink.Batches, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void OffProfile_DisablesPipelineExplicitly()
    {
        var snapshot = ModuleContractVerificationOptions.Off().SnapshotValidated();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Mode, Is.EqualTo(ModuleContractVerificationMode.Off));
            Assert.That(snapshot.PipelineOptions.Enabled, Is.False);
            Assert.That(snapshot.DiagnosticSink, Is.SameAs(NullModuleContractDiagnosticSink.Instance));
        });
    }
}
