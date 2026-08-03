using NativeMathModule;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public sealed class NativeArithmeticPromotionTests
{
    [TestCase(typeof(int), typeof(int), typeof(int))]
    [TestCase(typeof(int), typeof(long), typeof(long))]
    [TestCase(typeof(long), typeof(int), typeof(long))]
    [TestCase(typeof(int), typeof(float), typeof(float))]
    [TestCase(typeof(float), typeof(long), typeof(float))]
    [TestCase(typeof(int), typeof(double), typeof(double))]
    [TestCase(typeof(double), typeof(float), typeof(double))]
    [TestCase(typeof(int), typeof(decimal), typeof(decimal))]
    [TestCase(typeof(decimal), typeof(long), typeof(decimal))]
    public void ResolveBinaryNumericType_UsesDocumentedFinitePromotionMatrix(
        Type left,
        Type right,
        Type expected)
    {
        Assert.That(NativeArithmeticAstVisitor.ResolveBinaryNumericType(left, right), Is.EqualTo(expected));
    }

    [TestCase(typeof(decimal), typeof(float))]
    [TestCase(typeof(double), typeof(decimal))]
    [TestCase(typeof(short), typeof(int))]
    [TestCase(typeof(int), typeof(uint))]
    public void ResolveBinaryNumericType_RejectsUnsupportedOrAmbiguousPairs(Type left, Type right)
    {
        Assert.Throws<NotSupportedException>(() =>
            NativeArithmeticAstVisitor.ResolveBinaryNumericType(left, right));
    }
}
