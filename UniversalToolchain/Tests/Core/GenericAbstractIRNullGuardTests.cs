namespace Tests.Core;

[TestFixture]
public class GenericAbstractIrNullGuardTests
{
    [Test]
    public void Intrinsic_WithNullInstructionIdentifier_ThrowsArgumentNullException()
    {
        var air = new GenericAbstractIR<int>();

        Assert.Throws<ArgumentNullException>(() => air.Intrinsic(null!));
    }

    [Test]
    public void Intrinsic_WithNullOperandsArray_ThrowsArgumentNullException()
    {
        var air = new GenericAbstractIR<int>();

        Assert.Throws<ArgumentNullException>(() => air.Intrinsic("add_i32", null!));
    }

    [Test]
    public void AppendInstructions_WithNullCollection_ThrowsArgumentNullException()
    {
        var air = new GenericAbstractIR<int>();

        Assert.Throws<ArgumentNullException>(() => air.AppendInstructions(null!));
    }
}