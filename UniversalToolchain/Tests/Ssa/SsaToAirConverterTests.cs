using IntermediateRepresentationAbstractions;
using System.Reflection;
using UniversalToolchain.Air.Analysis;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.Ssa.Emission;
using UniversalToolchain.Ssa.Lowering;
using UniversalToolchain.Ssa.Optimization;
using UniversalIntermediateRepresentation;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaToAirConverterTests
{
    [Test]
    public void Run_WhenSsaWasProducedFromSupportedAirSubset_ProducesVerifiableAir()
    {
        var high = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var merge = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var source = new AbstractIR();
        source.Push(true);
        source.JmpIf(high);
        source.Push(10);
        source.Jmp(merge);
        source.SetLabel(high);
        source.Push(20);
        source.SetLabel(merge);

        var ssa = PreviewLowerer()
            .Run(new AirArtifact(source), new IrPipelineContext())
            .Artifact
            .As<SsaArtifact>();

        var result = PreviewEmitter().Run(ssa, new IrPipelineContext());
        var air = result.Artifact.As<AirArtifact>();

        Assert.Multiple(() =>
        {
            Assert.That(air.Kind, Is.EqualTo(AirIrKinds.Air));
            Assert.That(air.Program.Instructions.Select(static x => x.UOpCode), Does.Contain(UOpCode.JmpIf));
            Assert.That(result.Facts.Contains(UniversalToolchain.Air.Analysis.AirFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_WhenSsaContainsLegacyArithmeticOperation_RejectsItBeforeEmission()
    {
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        ConstI32("left", left, 2),
                        ConstI32("right", right, 3),
                        new SsaOperation(new SsaOperationId("add"), new SsaOpId("test.legacy.add"), [left.Id, right.Id], [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            new SsaToAirConverter().Run(artifact, new IrPipelineContext()));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.input.invalid"));
            Assert.That(exception.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.operation.descriptor.missing"));
        });
    }

    [Test]
    public void Run_WhenSsaContainsCallableInstruction_ThrowsMissingCallLoweringDiagnostic()
    {
        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("input", input, 1),
                        new SsaCall(
                            new SsaOperationId("call.identity"),
                            TestCallables.IdentityInt32,
                            [input.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));

        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, TestSemanticDescriptors),
            new StructuralAirVerifier(),
            new SsaCallAirIntrinsicLoweringPlanner(
                TestSemanticDescriptors,
                SsaCallAirIntrinsicLoweringSet.Empty,
                AirCoreIntrinsicDescriptors.ArithmeticInt32));

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.call-lowering.missing"));
    }

    [Test]
    public void Run_WhenSsaContainsPreviewArithmeticCallable_LowersToVerifiableAirIntrinsic()
    {
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("left", left, 2),
                        ConstI32("right", right, 3),
                        new SsaCall(
                            new SsaOperationId("call.add"),
                            SsaPreviewCallables.AddInt32Unchecked,
                            [left.Id, right.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));

        var conversion = PreviewEmitter().Run(artifact, new IrPipelineContext());
        var air = conversion.Artifact.As<AirArtifact>();

        Assert.Multiple(() =>
        {
            Assert.That(air.Program.Instructions.Select(static x => x.UOpCode), Does.Contain(UOpCode.Intrinsic));
            Assert.That(
                air.Program.Instructions.Single(static x => x.UOpCode == UOpCode.Intrinsic).Operands,
                Is.EqualTo(new object[] { AirIntrinsicIds.AddInt32Unchecked }));
            Assert.That(conversion.Facts.Contains(AirFacts.StructuralVerification), Is.True);
        });
    }

    [Test]
    public void Run_WhenSsaContainsManagedStaticMethodCallable_LowersToCallCSharpIntrinsic()
    {
        var method = typeof(SsaToAirConverterTests).GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.That(
            SsaManagedCallables.TryCreateMethod(method, consumesInstanceReceiver: false, out var callable, out var descriptor, out var diagnostic),
            Is.True,
            diagnostic);

        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("input", input, 41),
                        new SsaCall(
                            new SsaOperationId("call.add-one"),
                            callable,
                            [input.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32),
            new SsaManagedCallableBinding(callable, descriptor, method));

        var conversion = PreviewEmitter().Run(artifact, new IrPipelineContext());
        var intrinsic = conversion.Artifact.As<AirArtifact>().Program.Instructions.Single(static x => x.UOpCode == UOpCode.Intrinsic);

        Assert.That(intrinsic.Operands, Is.EqualTo(new object[] { AirIntrinsicIds.CallCSharp, method }));
    }

    [Test]
    public void Run_WhenSsaContainsManagedConstructorCallable_LowersToCallCSharpCtorIntrinsic()
    {
        var constructor = typeof(ManagedTestBox).GetConstructor(Type.EmptyTypes)!;
        Assert.That(
            SsaManagedCallables.TryCreateConstructor(constructor, out var callable, out var descriptor, out var diagnostic),
            Is.True,
            diagnostic);

        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Object);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        new SsaCall(
                            new SsaOperationId("call.ctor"),
                            callable,
                            [],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Object),
            new SsaManagedCallableBinding(callable, descriptor, constructor));

        var conversion = PreviewEmitter().Run(artifact, new IrPipelineContext());
        var intrinsic = conversion.Artifact.As<AirArtifact>().Program.Instructions.Single(static x => x.UOpCode == UOpCode.Intrinsic);

        Assert.That(intrinsic.Operands, Is.EqualTo(new object[] { AirIntrinsicIds.CallCSharpConstructor, constructor }));
    }

    [Test]
    public void Run_WhenCallableLoweringTargetsUnavailableIntrinsic_ThrowsCapabilityDiagnostic()
    {
        var artifact = CreatePreviewAddArtifact();
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, SsaPreviewSemanticDescriptors.ArithmeticInt32),
            new StructuralAirVerifier(),
            SsaPreviewAirIntrinsicLowerings.ArithmeticInt32,
            AirIntrinsicDescriptorSet.Empty);

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.intrinsic-capability.missing"));
    }

    [Test]
    public void Run_WhenCallableLoweringIntrinsicShapeDoesNotMatchDescriptor_ThrowsShapeDiagnostic()
    {
        var artifact = CreatePreviewAddArtifact();
        var badLowering = new SsaCallAirIntrinsicLoweringSet(
        [
            new SsaCallAirIntrinsicLowering(
                SsaPreviewCallables.AddInt32Unchecked,
                AirIntrinsicIds.EqualInt32)
        ]);
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, SsaPreviewSemanticDescriptors.ArithmeticInt32),
            new StructuralAirVerifier(),
            badLowering,
            AirCoreIntrinsicDescriptors.ArithmeticInt32);

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.call-lowering.shape"));
    }

    [Test]
    public void Run_WhenCallableHasTwoSupportedSamePriorityTargets_ThrowsAmbiguousTargetDiagnostic()
    {
        var artifact = CreatePreviewAddArtifact();
        var targets = new SsaCallableLoweringTargetSet(
        [
            SsaCallableLoweringTarget.AirIntrinsic(SsaPreviewCallables.AddInt32Unchecked, AirIntrinsicIds.AddInt32Unchecked),
            SsaCallableLoweringTarget.AirIntrinsic(SsaPreviewCallables.AddInt32Unchecked, AirIntrinsicIds.SubtractInt32Unchecked)
        ]);
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, SsaPreviewSemanticDescriptors.ArithmeticInt32),
            new StructuralAirVerifier(),
            targets,
            AirCoreIntrinsicDescriptors.ArithmeticInt32);

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.call-lowering.ambiguous"));
    }

    [Test]
    public void Run_WhenCallableHasSupportedTargetsWithDifferentPriority_SelectsBestPriorityTarget()
    {
        var method = typeof(SsaToAirConverterTests).GetMethod(nameof(AddPair), BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.That(
            SsaManagedCallables.TryCreateMethod(method, consumesInstanceReceiver: false, out var callable, out var descriptor, out var diagnostic),
            Is.True,
            diagnostic);

        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("left", left, 2),
                        ConstI32("right", right, 3),
                        new SsaCall(
                            new SsaOperationId("call.add-pair"),
                            callable,
                            [left.Id, right.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32),
            new SsaManagedCallableBinding(callable, descriptor, method));
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, SsaPreviewSemanticDescriptors.ArithmeticInt32),
            new StructuralAirVerifier(),
            new SsaCallableLoweringTargetSet(
            [
                SsaCallableLoweringTarget.AirIntrinsic(callable, AirIntrinsicIds.AddInt32Unchecked)
            ]),
            AirCoreIntrinsicDescriptors.ArithmeticInt32);

        var conversion = converter.Run(artifact, new IrPipelineContext());
        var intrinsic = conversion.Artifact.As<AirArtifact>().Program.Instructions.Single(static x => x.UOpCode == UOpCode.Intrinsic);

        Assert.That(intrinsic.Operands, Is.EqualTo(new object[] { AirIntrinsicIds.AddInt32Unchecked }));
    }

    [Test]
    public void Run_WhenManagedCallableBindingDescriptorHasMismatchedTypes_FailsStructuralVerification()
    {
        var method = typeof(SsaToAirConverterTests).GetMethod(nameof(AddOne), BindingFlags.NonPublic | BindingFlags.Static)!;
        Assert.That(
            SsaManagedCallables.TryCreateMethod(method, consumesInstanceReceiver: false, out var callable, out _, out var diagnostic),
            Is.True,
            diagnostic);

        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var function = new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("input", input, 1),
                        new SsaCall(
                            new SsaOperationId("call.bad-managed-shape"),
                            callable,
                            [input.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32);
        var mismatchedDescriptors = new SemanticDescriptorSet(
            types:
            [
                new SemanticTypeDescriptor(new SemanticTypeId(SsaTypes.Int32.Value)),
                new SemanticTypeDescriptor(new SemanticTypeId(SsaTypes.Bool.Value))
            ],
            callables:
            [
                new CallableDescriptor(
                    callable,
                    new CallableSignature(
                        [new SemanticTypeId(SsaTypes.Bool.Value)],
                        [new SemanticTypeId(SsaTypes.Int32.Value)]),
                    effects: SemanticEffectSummary.Pure,
                    determinism: Determinism.Deterministic,
                    trustLevel: SemanticTrustLevel.BuiltInTrusted)
            ]);
        var mismatchedDescriptor = mismatchedDescriptors.Callables.Single();
        var artifact = Artifact(
            function,
            new SsaManagedCallableBinding(callable, mismatchedDescriptor, method));
        var planner = new SsaCallableLoweringPlanner(
            mismatchedDescriptors,
            SsaCallableLoweringTargetSet.Empty,
            AirCoreIntrinsicDescriptors.ArithmeticInt32);
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, SsaPreviewSemanticDescriptors.ArithmeticInt32),
            new StructuralAirVerifier(),
            planner);

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.call.argument-type"));
    }

    [Test]
    public void Run_WhenCallableHasOnlyCilTarget_RejectsBeforeAirEmission()
    {
        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("input", input, 1),
                        new SsaCall(
                            new SsaOperationId("call.identity"),
                            TestCallables.IdentityInt32,
                            [input.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));
        var planner = new SsaCallableLoweringPlanner(
            TestSemanticDescriptors,
            new SsaCallableLoweringTargetSet(
            [
                SsaCallableLoweringTarget.CilOpcode(TestCallables.IdentityInt32, "cil.ldc.i4")
            ]),
            AirCoreIntrinsicDescriptors.ArithmeticInt32);
        var converter = new SsaToAirConverter(
            new StructuralSsaVerifier(SsaCoreDescriptors.ConstantMaterialization, TestSemanticDescriptors),
            new StructuralAirVerifier(),
            planner);

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            converter.Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.cil-target.unsupported"));
    }

    [Test]
    public void Run_WhenSsaValueIsUsedTwiceWithoutTempStrategy_ThrowsStackShapeDiagnostic()
    {
        var input = new SsaValue(new SsaValueId("%input"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("input", input, 2),
                        new SsaCall(
                            new SsaOperationId("call.add"),
                            SsaPreviewCallables.AddInt32Unchecked,
                            [input.Id, input.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            PreviewEmitter().Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.stack-shape.unsupported"));
    }

    [Test]
    public void Run_WhenSsaBlocksAreNotInEmissionOrder_UsesCfgLayout()
    {
        var condition = new SsaValue(new SsaValueId("%condition"), SsaTypes.Bool);
        var low = new SsaValue(new SsaValueId("%low"), SsaTypes.Int32);
        var high = new SsaValue(new SsaValueId("%high"), SsaTypes.Int32);
        var mergeParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("%merge_arg0"), SsaTypes.Int32));
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations: [ConstBool("condition", condition, true)],
                    terminator: SsaTerminator.Branch(
                        condition.Id,
                        new SsaBlockId("high"),
                        [],
                        new SsaBlockId("low"),
                        [])),
                new SsaBlock(
                    new SsaBlockId("merge"),
                    [mergeParameter],
                    terminator: SsaTerminator.Return([mergeParameter.Value.Id])),
                new SsaBlock(
                    new SsaBlockId("high"),
                    operations: [ConstI32("high", high, 20)],
                    terminator: SsaTerminator.Jump(new SsaBlockId("merge"), [high.Id])),
                new SsaBlock(
                    new SsaBlockId("low"),
                    operations: [ConstI32("low", low, 10)],
                    terminator: SsaTerminator.Jump(new SsaBlockId("merge"), [low.Id]))
            ],
            returnType: SsaTypes.Int32));

        var result = PreviewEmitter().Run(artifact, new IrPipelineContext());
        var air = result.Artifact.As<AirArtifact>();

        Assert.Multiple(() =>
        {
            Assert.That(air.Program.Instructions.Select(static x => x.UOpCode), Does.Contain(UOpCode.JmpIf));
            Assert.That(air.Program.Instructions[^1].UOpCode, Is.EqualTo(UOpCode.Label));
            Assert.That(result.Facts.Contains(UniversalToolchain.Air.Analysis.AirFacts.StructuralVerification), Is.True);
        });
    }


    [Test]
    public void Run_WhenBranchPassesDifferentArguments_ThrowsBranchArgumentDiagnostic()
    {
        var condition = new SsaValue(new SsaValueId("%condition"), SsaTypes.Bool);
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var leftParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("%left_arg0"), SsaTypes.Int32));
        var rightParameter = new SsaBlockParameter(new SsaValue(new SsaValueId("%right_arg0"), SsaTypes.Int32));
        var artifact = Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    operations:
                    [
                        ConstBool("condition", condition, true),
                        ConstI32("left", left, 10),
                        ConstI32("right", right, 20)
                    ],
                    terminator: SsaTerminator.Branch(
                        condition.Id,
                        new SsaBlockId("left"),
                        [left.Id],
                        new SsaBlockId("right"),
                        [right.Id])),
                new SsaBlock(
                    new SsaBlockId("left"),
                    [leftParameter],
                    terminator: SsaTerminator.Return([leftParameter.Value.Id])),
                new SsaBlock(
                    new SsaBlockId("right"),
                    [rightParameter],
                    terminator: SsaTerminator.Return([rightParameter.Value.Id]))
            ],
            returnType: SsaTypes.Int32));

        var exception = Assert.Throws<SsaToAirEmissionException>(() =>
            PreviewEmitter().Run(artifact, new IrPipelineContext()));

        Assert.That(exception!.Diagnostics.Select(static x => x.Code), Does.Contain("ssa.to-air.branch.arguments"));
    }

    private static SsaOperation ConstI32(string id, SsaValue result, int value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantInt32,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static SsaOperation ConstBool(string id, SsaValue result, bool value) =>
        new(
            new SsaOperationId(id),
            SsaOperations.ConstantBool,
            results: [result],
            attributes: new SsaAttributeBag([new SsaAttribute(SsaAttributeKeys.ConstantValue, value.ToString())]));

    private static SsaArtifact CreatePreviewAddArtifact()
    {
        var left = new SsaValue(new SsaValueId("%left"), SsaTypes.Int32);
        var right = new SsaValue(new SsaValueId("%right"), SsaTypes.Int32);
        var result = new SsaValue(new SsaValueId("%result"), SsaTypes.Int32);
        return Artifact(new SsaFunction(
            new SsaFunctionId("main"),
            new SsaBlockId("entry"),
            [
                new SsaBlock(
                    new SsaBlockId("entry"),
                    instructions:
                    [
                        ConstI32("left", left, 2),
                        ConstI32("right", right, 3),
                        new SsaCall(
                            new SsaOperationId("call.add"),
                            SsaPreviewCallables.AddInt32Unchecked,
                            [left.Id, right.Id],
                            [result])
                    ],
                    terminator: SsaTerminator.Return([result.Id]))
            ],
            returnType: SsaTypes.Int32));
    }

    private static SsaArtifact Artifact(
        SsaFunction function,
        params SsaManagedCallableBinding[] managedCallableBindings) =>
        new(
            new SsaModule(new SsaModuleId("test.module"), [function]),
            managedCallableBindings.Length == 0
                ? SsaManagedCallableBindingSet.Empty
                : new SsaManagedCallableBindingSet(managedCallableBindings));

    private static AirToSsaConverter PreviewLowerer() =>
        SsaRouteFactory.CreateLowerer(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Require));

    private static SsaToAirConverter PreviewEmitter() =>
        SsaRouteFactory.CreateEmitter(SsaPreviewRouteProfiles.Create(SsaRoutePolicy.Require));

    private static int AddOne(int value) => value + 1;

    private static int AddPair(int left, int right) => left + right;

    private sealed class ManagedTestBox;

    private static class TestCallables
    {
        public static CallableId IdentityInt32 { get; } = new("test.core.i32.identity");
    }

    private static SemanticDescriptorSet TestSemanticDescriptors { get; } = new(
        types:
        [
            new SemanticTypeDescriptor(new SemanticTypeId(SsaTypes.Int32.Value))
        ],
        callables:
        [
            new CallableDescriptor(
                TestCallables.IdentityInt32,
                new CallableSignature(
                    [new SemanticTypeId(SsaTypes.Int32.Value)],
                    [new SemanticTypeId(SsaTypes.Int32.Value)]),
                effects: SemanticEffectSummary.Pure,
                determinism: Determinism.Deterministic,
                trustLevel: SemanticTrustLevel.BuiltInTrusted)
        ]);
}
