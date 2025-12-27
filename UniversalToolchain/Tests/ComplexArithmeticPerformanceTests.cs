namespace Tests;

[TestFixture]
public class ComplexArithmeticPerformanceTests : TestBase
{
    [Test]
    public void Execute_ManyNestedOperations_PerformsEfficiently()
    {
        // Arrange
        var code = """
                   let result = 0
                   let i = 0

                   @start:
                   if i >= 50 goto @end
                       result = result + (i * (i + 1) - (i / 2)) * ((i * 2) - (i / 3)) / (i + 1)
                       i = i + 1
                       goto @start
                   @end:
                   result
                   """;

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = ExecuteCode(code);
        stopwatch.Stop();

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.GreaterThan(0).Within(1e-9));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(2000));
    }

    [Test]
    public void Execute_ComplexMatrixLikeOperations_PerformsWell()
    {
        // Arrange
        var code = @"
                let a11 = 1; let a12 = 2; let a13 = 3
                let a21 = 4; let a22 = 5; let a23 = 6
                let a31 = 7; let a32 = 8; let a33 = 9
                
                let b11 = 9; let b12 = 8; let b13 = 7
                let b21 = 6; let b22 = 5; let b23 = 4
                let b31 = 3; let b32 = 2; let b33 = 1
                
                let c11 = a11*b11 + a12*b21 + a13*b31
                let c12 = a11*b12 + a12*b22 + a13*b32
                let c13 = a11*b13 + a12*b23 + a13*b33
                
                let c21 = a21*b11 + a22*b21 + a23*b31
                let c22 = a21*b12 + a22*b22 + a23*b32
                let c23 = a21*b13 + a22*b23 + a23*b33
                
                let c31 = a31*b11 + a32*b21 + a33*b31
                let c32 = a31*b12 + a32*b22 + a33*b32
                let c33 = a31*b13 + a32*b23 + a33*b33
                
                c11 + c12 + c13 + c21 + c22 + c23 + c31 + c32 + c33
            ";

        var stopwatch = new Stopwatch();

        // Act
        stopwatch.Start();
        var result = ExecuteCode(code);
        stopwatch.Stop();

        // Assert
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(621).Within(1e-9));
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
    }
}