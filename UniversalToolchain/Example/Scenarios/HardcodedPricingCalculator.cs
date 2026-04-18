namespace Example.Scenarios;

public sealed class HardcodedPricingCalculator
{
    public double Calculate(double price, double fee) => price * 0.9 + fee;
}