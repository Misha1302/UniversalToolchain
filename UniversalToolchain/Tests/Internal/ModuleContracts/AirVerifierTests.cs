using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class AirVerifierTests
{
    private static readonly ModuleId _testModule = new("test.air");
    private static readonly BackendCapabilityId _interpreterCapability = new("test.backend.interpreter");
    private static readonly BackendCapabilityId _compilerCapability = new("test.backend.compiler");
    private static readonly IntrinsicSymbolId _backendOnlyIntrinsic = new("test.backend.only");

    [Test]
    public void Verify_WhenBackendDoesNotSupportIntrinsic_ReturnsUnsupportedIntrinsicDiagnostic()
    {
        var table = CreateTable(includeBackendOnlySupport: false);
        var selection = BackendCapabilitySelection.FromContracts(table, [_interpreterCapability]);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ModuleContractDiagnosticCodes.UnsupportedAirIntrinsic));
    }

    [Test]
    public void Verify_WhenInterpreterPolicySeesBackendSpecificIntrinsic_RejectsBeforeExecution()
    {
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = new BackendCapabilitySelection(
            [_interpreterCapability],
            [KnownCoreIntrinsicSymbols.CallCSharp, KnownCoreIntrinsicSymbols.CallCSharpConstructor, _backendOnlyIntrinsic],
            AirBackendPolicy.UniversalInterpreter);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ModuleContractDiagnosticCodes.InterpreterBackendIntrinsicViolation));
    }

    [Test]
    public void Verify_WhenRequiredBackendCapabilityIsMissing_ReturnsCapabilityDiagnostic()
    {
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_interpreterCapability]);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ModuleContractDiagnosticCodes.MissingBackendCapability));
    }

    [Test]
    public void Verify_WhenSelectedCapabilitySupportsIntrinsic_ReturnsValidResult()
    {
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Verify_WhenBranchTargetIsMissing_ReturnsBranchDiagnostic()
    {
        var air = new AbstractIR();
        air.Jmp(Guid.NewGuid());
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Warn));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(ModuleContractDiagnosticCodes.MissingAirBranchTarget));
        Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Warning));
    }

    [Test]
    public void Verify_WhenInstructionSchemaIsInvalid_ReturnsSchemaDiagnostic()
    {
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(UOpCode.Jmp, ["not-a-guid"])]);
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(ModuleContractDiagnosticCodes.InvalidAirOperandSchema));
    }

    [Test]
    public void Validate_WhenOptimizerEmitsUnsupportedIntrinsic_ReturnsLegalityDiagnostic()
    {
        var table = CreateTable(includeBackendOnlySupport: false);
        var selection = BackendCapabilitySelection.FromContracts(table, [_interpreterCapability]);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);

        var result = new OptimizerAirValidationHook(new AirVerifier()).Validate(new OptimizerAirValidationRequest(
            "optimizer.test",
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(ModuleContractDiagnosticCodes.UnsupportedAirIntrinsic));
    }

    private static IAbstractIR CreateIntrinsicAir(IntrinsicSymbolId intrinsic)
    {
        var air = new AbstractIR();
        air.Intrinsic(intrinsic.Value);
        return air;
    }

    private static SelectedModuleContractTable CreateTable(bool includeBackendOnlySupport)
    {
        var compilerIntrinsics = includeBackendOnlySupport
            ? new[] { KnownCoreIntrinsicSymbols.CallCSharp, KnownCoreIntrinsicSymbols.CallCSharpConstructor, _backendOnlyIntrinsic }
            : [KnownCoreIntrinsicSymbols.CallCSharp, KnownCoreIntrinsicSymbols.CallCSharpConstructor];

        return new ModuleContractTableBuilder()
            .AddFacet(new AirContractFacet(
                _testModule,
                [
                    new AirEmissionContract(
                        KnownCoreBytecodePatterns.AbstractMethodConvertable,
                        [new AirPatternId("test.air.backend-only")],
                        [_backendOnlyIntrinsic],
                        [_compilerCapability])
                ]))
            .AddFacet(new BackendCapabilityFacet(
                _testModule,
                [
                    new BackendCapabilityContract(
                        _interpreterCapability,
                        [KnownCoreIntrinsicSymbols.CallCSharp, KnownCoreIntrinsicSymbols.CallCSharpConstructor]),
                    new BackendCapabilityContract(
                        _compilerCapability,
                        compilerIntrinsics)
                ]))
            .Build();
    }
}
