namespace Tests;

[TestFixture]
public class MathematicalFormulasTests : TestBase
{
    [Test]
    public void Execute_QuadraticFormula_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let a = 1
                let b = -3
                let c = 2
                let discriminant = b * b - 4 * a * c
                let root1 = (0-b + discriminant) / (2 * a)
                let root2 = (0-b - discriminant) / (2 * a)
                root1 + root2
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        // For x² - 3x + 2 = 0, roots are 2 and 1, sum = 3
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(3).Within(1e-9));
    }

    [Test]
    public void Execute_PhysicsKinematicsFormula_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let initialVelocity = 10
                let acceleration = 2
                let time = 5
                let displacement = initialVelocity * time + 0.5 * acceleration * time * time
                displacement
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        // 10*5 + 0.5*2*25 = 50 + 25 = 75
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(75).Within(1e-9));
    }

    [Test]
    public void Execute_GeometryCircleCalculations_ComputesCorrectly()
    {
        // Arrange
        var code = @"
                let radius = 7
                let pi = 3.141592653589793
                let circumference = 2 * pi * radius
                let area = pi * radius * radius
                area / circumference
            ";
        var modules = new IFrontendCoreModule[]
        {
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new ArithmeticModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl()
        };

        // Act
        var result = ExecuteCode(code);

        // Assert
        // area/circumference = (πr²)/(2πr) = r/2 = 3.5
        var numberResult = (RealNumberImpl)result;
        Assert.That(numberResult.GetValue(), Is.EqualTo(3.5).Within(1e-9));
    }
}