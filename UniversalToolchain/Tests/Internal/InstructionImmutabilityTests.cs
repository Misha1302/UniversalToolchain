namespace Tests.Internal;

[TestFixture]
public sealed class InstructionImmutabilityTests
{
    [Test]
    public void Constructor_SnapshotsOperandsAndMetadata()
    {
        var operands = new List<object?> { 1 };
        var metadata = new List<object?> { "original" };

        var instruction = new Instruction(UOpCode.Push, operands, metadata);
        operands[0] = 2;
        metadata[0] = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(instruction.Operands, Is.EqualTo(new object?[] { 1 }));
            Assert.That(instruction.Metadata, Is.EqualTo(new object?[] { "original" }));
        });
    }

    [Test]
    public void ExposedCollections_AreReadOnly()
    {
        var instruction = new Instruction(UOpCode.Push, [1], ["metadata"]);

        Assert.Multiple(() =>
        {
            Assert.That(instruction.Operands, Is.AssignableTo<IList<object?>>());
            Assert.That(((IList<object?>)instruction.Operands).IsReadOnly, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<object?>)instruction.Operands).Add(2));
            Assert.That(instruction.Metadata, Is.AssignableTo<IList<object?>>());
            Assert.Throws<NotSupportedException>(() => ((IList<object?>)instruction.Metadata).Clear());
        });
    }
}
