using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class AirVerificationScopeTests
{
    [Test]
    public void StructuralScope_RejectsStackFault_WithoutRunningSemanticChecks()
    {
        var (table, selection) = CreateContracts();
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(UOpCode.Drop)]);
        var verifier = new AirVerifier();

        var structural = verifier.Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict,
            AirVerificationScope.Structural));
        var semantic = verifier.Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict,
            AirVerificationScope.Semantic));

        Assert.Multiple(() =>
        {
            Assert.That(
                structural.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ModuleContractDiagnosticCodes.InvalidAirStackDiscipline));
            Assert.That(semantic.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void SemanticScope_RejectsUnsupportedIntrinsic_WithoutRepeatingStructuralChecks()
    {
        var (table, selection) = CreateContracts();
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(
            UOpCode.Intrinsic,
            [IntrinsicInvocationFactory.ForCapability("load_i32", [1])])]);
        var verifier = new AirVerifier();

        var structural = verifier.Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict,
            AirVerificationScope.Structural));
        var semantic = verifier.Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict,
            AirVerificationScope.Semantic));

        Assert.Multiple(() =>
        {
            Assert.That(structural.Diagnostics, Is.Empty);
            Assert.That(
                semantic.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ModuleContractDiagnosticCodes.UnsupportedAirIntrinsic));
        });
    }

    [Test]
    public void EmptyOrUnknownScope_FailsClosed()
    {
        var (table, selection) = CreateContracts();
        var verifier = new AirVerifier();
        var air = new AbstractIR();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => verifier.Verify(new AirVerificationRequest(
                air,
                table,
                selection,
                VerificationSeverityProfile.Strict,
                AirVerificationScope.None)));
            Assert.Throws<ArgumentOutOfRangeException>(() => verifier.Verify(new AirVerificationRequest(
                air,
                table,
                selection,
                VerificationSeverityProfile.Strict,
                (AirVerificationScope)8)));
        });
    }

    private static (SelectedModuleContractTable Table, BackendCapabilitySelection Selection) CreateContracts()
    {
        var module = new ModuleId("test.air.scope");
        var capability = new BackendCapabilityId("test.air.scope.capability");
        var table = new ModuleContractTableBuilder()
            .AddFacet(new BackendCapabilityFacet(
                module,
                [new BackendCapabilityContract(capability, [])]))
            .Build();
        return (table, BackendCapabilitySelection.FromContracts(table, [capability]));
    }
}
