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
