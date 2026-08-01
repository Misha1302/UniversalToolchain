using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class BytecodeVerifierTests
{
    [Test]
    public void Verify_WhenBytecodeContainsUnknownTag_ReturnsWarningDiagnosticInWarnMode()
    {
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(
                ["unknown.tag"],
                new LevelCollection<float, IAbstractMethodConvertable>())
        ]);
        var table = new ModuleContractTableBuilder().Build();

        var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            bytecode,
            table,
            VerificationSeverityProfile.Warn));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(ModuleContractDiagnosticCodes.UnknownBytecodeTag));
        Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Warning));
    }

    [Test]
    public void Verify_DoesNotInferPatternFromRuntimeMethodName()
    {
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(new AbstractMethodImpl("unknown.pattern", (_, _) => { }))
        ]);
        var table = new ModuleContractTableBuilder().Build();

        var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            bytecode,
            table,
            VerificationSeverityProfile.Warn));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void Verify_WhenObservedEmissionViolatesProducerOrSourceIdentity_ReturnsDiagnostic()
    {
        var moduleId = new ModuleId("wist.test");
        var nodeKind = new AstNodeKind("wist.test.ast.node");
        var declaredPattern = new BytecodePatternId("wist.test.bytecode.declared");
        var table = CreateTable(moduleId, nodeKind, declaredPattern);

        var undeclaredPatternResult = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            new Bytecode([]),
            table,
            VerificationSeverityProfile.Warn,
            [
                new ObservedBytecodeEmission(
                    moduleId,
                    nodeKind,
                    [],
                    [new BytecodePatternId("wist.test.bytecode.undeclared")])
            ]));

        Assert.That(undeclaredPatternResult.IsValid, Is.True);
        Assert.That(
            undeclaredPatternResult.Diagnostics.Single().Code,
            Is.EqualTo(ModuleContractDiagnosticCodes.UndeclaredBytecodeProducer));

        var wrongSourceResult = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            new Bytecode([]),
            table,
            VerificationSeverityProfile.Strict,
            [
                new ObservedBytecodeEmission(
                    moduleId,
                    new AstNodeKind("wist.test.ast.other"),
                    [],
                    [declaredPattern])
            ]));

        Assert.That(wrongSourceResult.IsValid, Is.False);
        Assert.That(
            wrongSourceResult.Diagnostics.Single().Code,
            Is.EqualTo(ModuleContractDiagnosticCodes.UndeclaredBytecodeSource));
    }

    [Test]
    public void Verify_WhenObservedStackEffectDiffersFromDeclaration_ReturnsDiagnostic()
    {
        var moduleId = new ModuleId("wist.test");
        var nodeKind = new AstNodeKind("wist.test.ast.node");
        var pattern = new BytecodePatternId("wist.test.bytecode.pattern");
        var table = CreateTable(moduleId, nodeKind, pattern);

        var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            new Bytecode([]),
            table,
            VerificationSeverityProfile.Warn,
            [
                new ObservedBytecodeEmission(
                    moduleId,
                    nodeKind,
                    [],
                    [pattern],
                    new StackEffect(1, 0))
            ]));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(ModuleContractDiagnosticCodes.BytecodeStackEffectMismatch));
    }

    [Test]
    public void Verify_WhenStrictModeFindsUnknownTag_MarksResultInvalid()
    {
        var bytecode = new Bytecode(
        [
            new BytecodeInstruction(
                ["unknown.tag"],
                new LevelCollection<float, IAbstractMethodConvertable>())
        ]);
        var table = new ModuleContractTableBuilder().Build();

        var result = new BytecodeVerifier().Verify(new BytecodeVerificationRequest(
            bytecode,
            table,
            VerificationSeverityProfile.Strict));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics.Single().Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
    }

    private static SelectedModuleContractTable CreateTable(
        ModuleId moduleId,
        AstNodeKind nodeKind,
        BytecodePatternId pattern) =>
        new ModuleContractTableBuilder()
            .AddFacet(new BytecodeContractFacet(
                moduleId,
                [
                    new BytecodeEmissionContract(
                        nodeKind,
                        [],
                        [pattern],
                        new StackEffect(0, 1),
                        SideEffectPolicy.Pure)
                ]))
            .Build();
}
