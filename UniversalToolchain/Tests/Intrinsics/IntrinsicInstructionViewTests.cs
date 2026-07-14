using BasicCore.Builtins;

namespace Tests.Intrinsics;

[TestFixture]
public sealed class IntrinsicInstructionViewTests
{
    [TestCaseSource(nameof(CapabilityCases))]
    public void TryRead_TypedIntrinsic_ReturnsStableCapabilityId(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands,
        string expectedCapabilityId)
    {
        var instruction = BuiltinIntrinsicInstruction.Create(symbol, typeArguments, dataOperands);

        var success = IntrinsicInstructionView.TryRead(instruction, out var intrinsic);

        Assert.That(success, Is.True);
        Assert.That(intrinsic.CapabilityId, Is.EqualTo(expectedCapabilityId));
        Assert.That(intrinsic.Invocation.Symbol, Is.EqualTo(symbol));
        Assert.That(intrinsic.DataOperands, Is.EqualTo(dataOperands));
    }

    [Test]
    public void TryRead_ExplicitCapabilityInvocation_PreservesCapabilityAndData()
    {
        var instruction = IntrinsicInstructionFactory.CreateForCapability("custom.vector.add", 1, 2);

        var success = IntrinsicInstructionView.TryRead(instruction, out var intrinsic);

        Assert.That(success, Is.True);
        Assert.That(intrinsic.CapabilityId, Is.EqualTo("custom.vector.add"));
        Assert.That(intrinsic.DataOperands, Is.EqualTo(new object?[] { 1, 2 }));
    }

    [Test]
    public void TryRead_StringShapedInstruction_ReturnsFalse()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["boolean_not"]);

        var success = IntrinsicInstructionView.TryRead(instruction, out _);

        Assert.That(success, Is.False);
    }

    [Test]
    public void ReadOrThrow_StringShapedInstruction_ExplainsCanonicalShape()
    {
        var instruction = new Instruction(UOpCode.Intrinsic, ["call C#", typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!]);

        var exception = Assert.Throws<InvalidOperationException>(() => IntrinsicInstructionView.ReadOrThrow(instruction));

        Assert.That(exception!.Message, Does.Contain("structured IntrinsicInvocation"));
    }

    private static IEnumerable<TestCaseData> CapabilityCases()
    {
        yield return Case(BuiltinIntrinsicSymbols.Boolean.Not, [], [], "boolean_not");
        yield return Case(BuiltinIntrinsicSymbols.Arithmetic.Add, [IntrinsicTypeArgument.From(typeof(double))], [], "add_f64");
        yield return Case(BuiltinIntrinsicSymbols.Comparison.LessOrEqual, [IntrinsicTypeArgument.From(typeof(double))], [], "cmp_le_f64");
        yield return Case(BuiltinIntrinsicSymbols.Core.LoadConst, [IntrinsicTypeArgument.From(typeof(double))], [12.5d], "load_f64");
        yield return Case(BuiltinIntrinsicSymbols.Storage.LoadLocal, [IntrinsicTypeArgument.From(typeof(int))], ["value"], "load_local");
        yield return Case(BuiltinIntrinsicSymbols.Storage.LoadLocalRef, [IntrinsicTypeArgument.From(typeof(int))], ["value"], "load_local_ref");
        yield return Case(
            BuiltinIntrinsicSymbols.Core.CallCSharp,
            [],
            [typeof(Math).GetMethod(nameof(Math.Abs), [typeof(int)])!],
            "call C#");
    }

    private static TestCaseData Case(
        IntrinsicSymbol symbol,
        IReadOnlyList<IntrinsicTypeArgument> typeArguments,
        IReadOnlyList<object?> dataOperands,
        string expectedCapabilityId) =>
        new TestCaseData(symbol, typeArguments, dataOperands, expectedCapabilityId)
            .SetName($"Capability_{expectedCapabilityId.Replace(' ', '_').Replace('#', 's')}");
}
