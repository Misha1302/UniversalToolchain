using System.Reflection;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;
using UniversalToolchain.StandardSemantics;

namespace Tests.Ssa;

[TestFixture]
public sealed class SsaCallableDescriptorTests
{
    [Test]
    public void SemanticDescriptorSet_ShouldBeDeterministicAndRejectDuplicateCallables()
    {
        var first = new CallableDescriptor(
            new CallableId("demo.a"),
            new CallableSignature(resultTypes: [StandardSemanticTypes.Int32]));
        var second = new CallableDescriptor(
            new CallableId("demo.b"),
            new CallableSignature(resultTypes: [StandardSemanticTypes.Int32]));

        var ordered = new SemanticDescriptorSet(callables: [second, first]);

        Assert.That(ordered.Callables.Select(static x => x.Id.Value), Is.EqualTo(new[] { "demo.a", "demo.b" }));
        Assert.Throws<ArgumentException>(() => new SemanticDescriptorSet(callables: [first, first]));
    }

    [Test]
    public void SemanticDescriptorSet_WhenUncheckedDescriptorExposesAlgebraicTraits_RejectsIt()
    {
        var descriptor = new CallableDescriptor(
            new CallableId("plugin.untrusted.add"),
            new CallableSignature(
                [StandardSemanticTypes.Int32, StandardSemanticTypes.Int32],
                [StandardSemanticTypes.Int32]),
            algebraicTraits: AlgebraicTraits.Commutative,
            trustLevel: SemanticTrustLevel.UserProvidedUnchecked);

        var exception = Assert.Throws<ArgumentException>(() => new SemanticDescriptorSet(callables: [descriptor]));

        Assert.That(exception!.Message, Does.Contain("cannot expose algebraic traits"));
    }

    [Test]
    public void StandardSemantics_ShouldKeepArithmeticOutsideSsaCore()
    {
        var descriptors = StandardSemanticDescriptors.ScalarInt32;

        Assert.Multiple(() =>
        {
            Assert.That(descriptors.TryGetCallable(StandardCallables.AddInt32Unchecked, out var add), Is.True);
            Assert.That(add!.Effects.IsPure, Is.True);
            Assert.That(add.HasTrait(AlgebraicTraits.Commutative), Is.True);
            Assert.That(add.TrustLevel, Is.EqualTo(SemanticTrustLevel.BuiltInTrusted));
        });
    }

    [Test]
    public void StandardInt32ConstantEvaluator_ShouldEvaluateTrustedArithmeticCallables()
    {
        Assert.That(
            StandardSemanticDescriptors.ScalarInt32.TryGetCallable(StandardCallables.AddInt32Unchecked, out var add),
            Is.True);

        var evaluated = new StandardInt32ConstantEvaluator().TryEvaluate(
            add!,
            [
                new ConstantValue(StandardSemanticTypes.Int32, int.MaxValue.ToString()),
                new ConstantValue(StandardSemanticTypes.Int32, "1")
            ],
            out var result);

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.True);
            Assert.That(result.Type, Is.EqualTo(StandardSemanticTypes.Int32));
            Assert.That(result.CanonicalValue, Is.EqualTo(int.MinValue.ToString()));
        });
    }

    [Test]
    public void SsaManagedCallables_WhenMethodHasTrustedSemanticAttribute_UsesDeclaredDescriptorSemantics()
    {
        var method = typeof(SsaCallableDescriptorTests).GetMethod(
            nameof(TrustedManagedAdd),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var created = SsaManagedCallables.TryCreateMethod(
            method,
            consumesInstanceReceiver: false,
            out _,
            out var descriptor,
            out var diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.True, diagnostic);
            Assert.That(descriptor.Effects.IsPure, Is.True);
            Assert.That(descriptor.Determinism, Is.EqualTo(Determinism.Deterministic));
            Assert.That(descriptor.HasTrait(AlgebraicTraits.Commutative), Is.True);
            Assert.That(descriptor.HasTrait(AlgebraicTraits.Associative), Is.True);
            Assert.That(descriptor.TrustLevel, Is.EqualTo(SemanticTrustLevel.VerifiedPlugin));
        });
    }

    [Test]
    public void SsaManagedCallableBindingSet_WhenSameMethodIsLoweredTwice_AcceptsEquivalentDescriptors()
    {
        var method = typeof(SsaCallableDescriptorTests).GetMethod(
            nameof(TrustedManagedAdd),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.That(
            SsaManagedCallables.TryCreateMethod(
                method,
                consumesInstanceReceiver: false,
                out var firstCallable,
                out var firstDescriptor,
                out var firstDiagnostic),
            Is.True,
            firstDiagnostic);
        Assert.That(
            SsaManagedCallables.TryCreateMethod(
                method,
                consumesInstanceReceiver: false,
                out var secondCallable,
                out var secondDescriptor,
                out var secondDiagnostic),
            Is.True,
            secondDiagnostic);

        Assert.That(firstDescriptor, Is.Not.SameAs(secondDescriptor));

        var bindings = new SsaManagedCallableBindingSet(
        [
            new SsaManagedCallableBinding(firstCallable, firstDescriptor, method),
            new SsaManagedCallableBinding(secondCallable, secondDescriptor, method)
        ]);

        Assert.Multiple(() =>
        {
            Assert.That(bindings.Values, Has.Count.EqualTo(1));
            Assert.That(bindings.Values.Single().Callable, Is.EqualTo(firstCallable));
        });
    }

    [Test]
    public void SsaManagedCallables_WhenUnknownTrustMethodDeclaresAlgebraicTraits_DescriptorSetRejectsIt()
    {
        var method = typeof(SsaCallableDescriptorTests).GetMethod(
            nameof(UntrustedManagedAdd),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        Assert.That(
            SsaManagedCallables.TryCreateMethod(
                method,
                consumesInstanceReceiver: false,
                out _,
                out var descriptor,
                out var diagnostic),
            Is.True,
            diagnostic);

        var exception = Assert.Throws<ArgumentException>(() => new SemanticDescriptorSet(callables: [descriptor]));

        Assert.That(exception!.Message, Does.Contain("cannot expose algebraic traits"));
    }

    [Test]
    public void SsaManagedCallables_WhenMethodHasUnsupportedValueType_RejectsDescriptorCreation()
    {
        var method = typeof(SsaCallableDescriptorTests).GetMethod(
            nameof(UnsupportedLongParameter),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var created = SsaManagedCallables.TryCreateMethod(
            method,
            consumesInstanceReceiver: false,
            out _,
            out _,
            out var diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.False);
            Assert.That(diagnostic, Does.Contain("unsupported CLR type"));
        });
    }

    [Test]
    public void SsaManagedCallables_WhenOpenGenericMethodIsUsed_RejectsDescriptorCreation()
    {
        var method = typeof(SsaCallableDescriptorTests).GetMethod(
            nameof(GenericIdentity),
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var created = SsaManagedCallables.TryCreateMethod(
            method,
            consumesInstanceReceiver: false,
            out _,
            out _,
            out var diagnostic);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.False);
            Assert.That(diagnostic, Does.Contain("unresolved generic parameters"));
        });
    }

    [Test]
    public void SsaSemanticCallVerifier_WhenCallMatchesDescriptor_ReturnsSuccess()
    {
        var left = Value("%left", StandardSemanticTypes.Int32);
        var right = Value("%right", StandardSemanticTypes.Int32);
        var result = Value("%result", StandardSemanticTypes.Int32);
        var call = new SsaCall(
            new SsaOperationId("call.add"),
            StandardCallables.AddInt32Unchecked,
            [left.Id, right.Id],
            [result]);

        var verification = Verify(call, [left, right]);

        Assert.That(verification.IsSuccess, Is.True, FormatDiagnostics(verification));
    }

    [Test]
    public void SsaSemanticCallVerifier_WhenCallableIsUnknown_ReturnsDiagnostic()
    {
        var call = new SsaCall(
            new SsaOperationId("call.unknown"),
            new CallableId("missing.callable"));

        var verification = Verify(call, []);

        AssertDiagnostic(verification, "ssa.call.descriptor.missing");
    }

    [Test]
    public void SsaSemanticCallVerifier_WhenArgumentTypeDoesNotMatch_ReturnsDiagnostic()
    {
        var left = Value("%left", StandardSemanticTypes.Bool);
        var right = Value("%right", StandardSemanticTypes.Int32);
        var result = Value("%result", StandardSemanticTypes.Int32);
        var call = new SsaCall(
            new SsaOperationId("call.bad.arg"),
            StandardCallables.AddInt32Unchecked,
            [left.Id, right.Id],
            [result]);

        var verification = Verify(call, [left, right]);

        AssertDiagnostic(verification, "ssa.call.argument-type");
    }

    [Test]
    public void SsaSemanticCallVerifier_WhenResultTypeDoesNotMatch_ReturnsDiagnostic()
    {
        var left = Value("%left", StandardSemanticTypes.Int32);
        var right = Value("%right", StandardSemanticTypes.Int32);
        var result = Value("%result", StandardSemanticTypes.Bool);
        var call = new SsaCall(
            new SsaOperationId("call.bad.result"),
            StandardCallables.AddInt32Unchecked,
            [left.Id, right.Id],
            [result]);

        var verification = Verify(call, [left, right]);

        AssertDiagnostic(verification, "ssa.call.result-type");
    }

    [Test]
    public void SsaSemanticCallVerifier_WhenAttributeIsNotAllowed_ReturnsDiagnostic()
    {
        var callable = new CallableId("demo.call");
        var descriptors = new SemanticDescriptorSet(
            callables:
            [
                new CallableDescriptor(
                    callable,
                    new CallableSignature(),
                    allowedAttributes: [new SemanticAttributeKey("semantic.allowed")])
            ]);
        var call = new SsaCall(
            new SsaOperationId("call.with.bad.attribute"),
            callable,
            attributes: new SsaAttributeBag([new SsaAttribute(new SsaAttributeKey("semantic.bad"), "1")]));

        var verification = new SsaSemanticCallVerifier(descriptors)
            .Verify(call, new Dictionary<SsaValueId, SsaValue>());

        AssertDiagnostic(verification, "ssa.call.attribute.unknown");
    }

    private static IrVerificationResult Verify(SsaCall call, IEnumerable<SsaValue> visibleValues)
    {
        var valueMap = visibleValues.ToDictionary(static x => x.Id);
        return new SsaSemanticCallVerifier(StandardSemanticDescriptors.ScalarInt32)
            .Verify(call, valueMap);
    }

    private static SsaValue Value(string id, SemanticTypeId type) =>
        new(new SsaValueId(id), new SsaTypeId(type.Value));

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        AlgebraicTraits = AlgebraicTraits.Commutative | AlgebraicTraits.Associative,
        TrustLevel = SemanticTrustLevel.VerifiedPlugin)]
    private static int TrustedManagedAdd(int left, int right) => left + right;

    [SsaManagedCallable(
        IsPure = true,
        Determinism = Determinism.Deterministic,
        AlgebraicTraits = AlgebraicTraits.Commutative,
        TrustLevel = SemanticTrustLevel.ExternalUnknown)]
    private static int UntrustedManagedAdd(int left, int right) => left + right;

    private static int UnsupportedLongParameter(long value) => (int)value;

    private static T GenericIdentity<T>(T value) => value;

    private static void AssertDiagnostic(IrVerificationResult result, string code)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Diagnostics.Select(static x => x.Code), Does.Contain(code), FormatDiagnostics(result));
    }

    private static string FormatDiagnostics(IrVerificationResult result) =>
        string.Join(Environment.NewLine, result.Diagnostics.Select(static x => $"{x.Code}: {x.Message}"));
}
