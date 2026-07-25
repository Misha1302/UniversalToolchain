using BasicCore.Capabilities;
using BasicCore.Validation;
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
    public void Verify_WhenSelectedCapabilitySupportsIntrinsicButSemanticsAreMissing_ReturnsStackDiagnostic()
    {
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidAirStackDiscipline));
    }

    [Test]
    public void Verify_WhenSelectedCapabilityAndSemanticDescriptorSupportIntrinsic_ReturnsValidResult()
    {
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);
        var verifier = CreateVerifierWithBackendOnlyIntrinsicSemantics();

        var result = verifier.Verify(new AirVerificationRequest(
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
    public void Verify_WhenDropConsumesEmptyStack_ReturnsStackDiagnostic()
    {
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(UOpCode.Drop)]);
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidAirStackDiscipline));
    }

    [Test]
    public void Verify_WhenConditionalJumpConsumesNonBoolean_ReturnsStackDiagnostic()
    {
        var target = Guid.NewGuid();
        var air = new AbstractIR();
        air.Push(1);
        air.JmpIf(target);
        air.AppendInstructions([new Instruction(UOpCode.Label, [target])]);
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidAirStackDiscipline));
    }

    [Test]
    public void Verify_WhenTerminalStackContainsTwoValues_ReturnsStackDiagnostic()
    {
        var air = new AbstractIR();
        air.Push(1);
        air.Push(2);
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidAirStackDiscipline));
    }

    [Test]
    public void Verify_WhenPushContainsRawNull_ReturnsSchemaDiagnostic()
    {
        var air = new AbstractIR();
        air.AppendInstructions([new Instruction(UOpCode.Push, [null])]);
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code),
            Does.Contain(ModuleContractDiagnosticCodes.InvalidAirOperandSchema));
    }

    [Test]
    public void Verify_WhenPushContainsTypedNull_ReturnsValidResult()
    {
        var air = new AbstractIR();
        air.Push<string?>(null);
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = new AirVerifier().Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void StackVerifier_ExpectedDomainFailure_ReturnsUserDiagnostic()
    {
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);
        var verifier = new AirVerifier(
            new InstructionIntrinsicReader(),
            new ThrowingStackProcessor(new InvalidOperationException("invalid user program")));
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var result = verifier.Verify(new AirVerificationRequest(
            air,
            table,
            selection,
            VerificationSeverityProfile.Strict));

        Assert.That(result.Diagnostics.Single().Message, Does.Contain("invalid user program"));
    }

    [Test]
    public void StackVerifier_UnexpectedImplementationFailure_PropagatesAsInternalFailure()
    {
        var air = CreateIntrinsicAir(_backendOnlyIntrinsic);
        var verifier = new AirVerifier(
            new InstructionIntrinsicReader(),
            new ThrowingStackProcessor(new NullReferenceException("implementation defect")));
        var table = CreateTable(includeBackendOnlySupport: true);
        var selection = BackendCapabilitySelection.FromContracts(table, [_compilerCapability]);

        var exception = Assert.Throws<InternalVerifierException>(() =>
            verifier.Verify(new AirVerificationRequest(
                air,
                table,
                selection,
                VerificationSeverityProfile.Warn)));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Verifier, Is.EqualTo("AirStackDisciplineVerifier"));
            Assert.That(exception.InnerException, Is.TypeOf<NullReferenceException>());
            Assert.That(exception.Message, Does.Contain("Internal verifier failure"));
        });
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

    private static AirVerifier CreateVerifierWithBackendOnlyIntrinsicSemantics()
    {
        var catalog = new IntrinsicCatalogBuilder().Build([new BackendOnlyIntrinsicDescriptorProvider()]);
        return new AirVerifier(
            new InstructionIntrinsicReader(),
            new IntrinsicTypeStackProcessor(catalog, new IntrinsicTypeResolutionContext()));
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
    private sealed class ThrowingStackProcessor(Exception exception) : IIntrinsicTypeStackProcessor
    {
        public void Process(IntrinsicInvocation invocation, List<Type> stack) => throw exception;
    }

    private sealed class BackendOnlyIntrinsicDescriptorProvider : IIntrinsicDescriptorProvider
    {
        public IReadOnlyList<IntrinsicSemanticDescriptor> GetDescriptors() =>
        [
            new()
            {
                Symbol = new IntrinsicSymbol(
                    IntrinsicCapabilityNameEncoder.CapabilityNamespace,
                    _backendOnlyIntrinsic.Value),
                Category = IntrinsicCategory.BackendSpecific,
                StackRule = new NoStackEffectRule(),
                ValidationRule = new NoValidationRule()
            }
        ];
    }

    private sealed class NoStackEffectRule : IIntrinsicStackRule
    {
        public void Apply(
            IntrinsicInvocation invocation,
            List<Type> stack,
            IIntrinsicTypeResolutionContext context)
        {
        }
    }

}
