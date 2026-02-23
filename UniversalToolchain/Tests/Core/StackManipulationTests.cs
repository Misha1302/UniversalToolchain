using NumbersModule.Core;

namespace Tests.Core;

[TestFixture]
public class StackManipulationTests : TestBase
{
    [Test]
    public void Execute_MixedBoolNumberExpressions_ComplexStackManipulation1()
    {
        var code = @"
            let a = 1 < 2
            let b = 3 > 4
            let c = 5 <= 5
            let d = 6 >= 7
            let e = 8 == 8
            let f = 9 != 9
            let g = 10 < 11
            let h = 12 > 13
            let i = 14 <= 15
            let j = 16 >= 17
            
            let num1 = (if a 100 else 0)
            let num2 = (if b 200 else 0)
            let num3 = (if c 300 else 0)
            let num4 = (if d 400 else 0)
            let num5 = (if e 500 else 0)
            let num6 = (if f 600 else 0)
            let num7 = (if g 700 else 0)
            let num8 = (if h 800 else 0)
            let num9 = (if i 900 else 0)
            let num10 = (if j 1000 else 0)
            
            num1 + num2 + num3 + num4 + num5 + num6 + num7 + num8 + num9 + num10
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(2500).Within(1e-9));
    }

    [Test]
    public void Execute_NestedBoolComparisons_ComplexStackManipulation2()
    {
        var code = @"
            let x1 = (1 < 2) and (2 < 3)
            let x2 = (3 > 4) or (4 < 5)
            let x3 = (5 == 5) and (6 != 7)
            let x4 = (8 <= 9) or (9 >= 10)
            let x5 = (10 == 11) and (11 < 12)
            let x6 = (12 > 13) or (13 == 13)
            let x7 = (14 < 15) and (15 > 16)
            let x8 = (16 >= 17) or (17 <= 18)
            let x9 = (18 == 19) and (19 != 20)
            let x10 = (20 < 21) or (21 > 22)
            let x11 = (22 <= 23) and (23 >= 24)
            let x12 = (24 == 25) or (25 < 26)
            let x13 = (26 > 27) and (27 == 28)
            let x14 = (28 >= 29) or (29 <= 30)
            let x15 = (30 == 31) and (31 != 32)
            
            let y1 = (if x1 1 else 0)
            let y2 = (if x2 2 else 0)
            let y3 = (if x3 3 else 0)
            let y4 = (if x4 4 else 0)
            let y5 = (if x5 5 else 0)
            let y6 = (if x6 6 else 0)
            let y7 = (if x7 7 else 0)
            let y8 = (if x8 8 else 0)
            let y9 = (if x9 9 else 0)
            let y10 = (if x10 10 else 0)
            let y11 = (if x11 11 else 0)
            let y12 = (if x12 12 else 0)
            let y13 = (if x13 13 else 0)
            let y14 = (if x14 14 else 0)
            let y15 = (if x15 15 else 0)
            
            y1 + y2 + y3 + y4 + y5 + y6 + y7 + y8 + y9 + y10 + y11 + y12 + y13 + y14 + y15
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(60).Within(1e-9));
    }

    [Test]
    public void Execute_DeepExpressionChaining_ComplexStackManipulation3()
    {
        var code = @"
            let v = 0

            v = v + (if (1 < 2) 1 else 0)
            v = v + (if (2 > 1) 2 else 0)
            v = v + (if (3 <= 3) 3 else 0)
            v = v + (if (4 >= 5) 4 else 0)
            v = v + (if (5 == 5) 5 else 0)
            v = v + (if (6 != 6) 6 else 0)
            v = v + (if (7 < 8) 7 else 0)
            v = v + (if (8 > 9) 8 else 0)
            v = v + (if (9 <= 10) 9 else 0)
            v = v + (if (10 >= 11) 10 else 0)

            v = v + (if (11 == 12) 11 else 0)
            v = v + (if (12 != 13) 12 else 0)
            v = v + (if (13 < 14) 13 else 0)
            v = v + (if (14 > 15) 14 else 0)
            v = v + (if (15 <= 16) 15 else 0)
            v = v + (if (16 >= 17) 16 else 0)
            v = v + (if (17 == 18) 17 else 0)
            v = v + (if (18 != 19) 18 else 0)
            v = v + (if (19 < 20) 19 else 0)
            v = v + (if (20 > 21) 20 else 0)

            v = v + (if ((21 < 22) and (22 < 23)) 21 else 0)
            v = v + (if ((23 > 24) or (24 < 25)) 22 else 0)
            v = v + (if ((25 == 25) and (26 != 26)) 23 else 0)
            v = v + (if ((27 <= 28) or (28 >= 29)) 24 else 0)
            v = v + (if ((29 == 30) and (30 < 31)) 25 else 0)
            v = v + (if ((31 > 32) or (32 == 32)) 26 else 0)
            v = v + (if ((33 < 34) and (34 > 35)) 27 else 0)
            v = v + (if ((35 >= 36) or (36 <= 37)) 28 else 0)
            v = v + (if ((37 == 38) and (38 != 39)) 29 else 0)
            v = v + (if ((39 < 40) or (40 > 41)) 30 else 0)

            v = v + (if true 31 else 0)
            v = v + (if false 32 else 0)
            v = v + (if true 33 else 0)
            v = v + (if false 34 else 0)
            v = v + (if true 35 else 0)
            v = v + (if false 36 else 0)
            v = v + (if true 37 else 0)
            v = v + (if false 38 else 0)
            v = v + (if true 39 else 0)
            v = v + (if false 40 else 0)

            v
        ";

        var result = ExecuteCode(code);
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(430).Within(1e-9));
    }
}