namespace Example.Scenarios;

public static class PricingDiscountScenario
{
    private const string Formula = "price * 0.9 + fee";
    private const double Price = 100.0;
    private const double Fee = 5.0;

    public static void Run()
    {
        var printer = new ScenarioConsolePrinter();
        var hardcodedPricingCalculator = new HardcodedPricingCalculator();
        using var dslPricingCalculator = new DslPricingCalculator();

        printer.PrintTitle("Pricing and Discount Demo");
        printer.PrintFormula(Formula, Price, Fee);

        var hardcodedResult = hardcodedPricingCalculator.Calculate(Price, Fee);
        printer.PrintResult("Hardcoded result", hardcodedResult);

        var compilerResult = dslPricingCalculator.CalculateWithCompiler(Formula, Price, Fee);
        printer.PrintResult("Compiler result", compilerResult);

        var interpreterResult = dslPricingCalculator.CalculateWithInterpreter(Formula, Price, Fee);
        printer.PrintResult("Interpreter result", interpreterResult);

        var fastInvokerResult = dslPricingCalculator.CalculateWithFastInvoker(Formula, Price, Fee);
        printer.PrintResult("Fast invoker result", fastInvokerResult);

        var allResultsMatch = ResultsMatch(
            hardcodedResult,
            compilerResult,
            interpreterResult,
            fastInvokerResult);

        printer.PrintSummary(allResultsMatch);
    }

    private static bool ResultsMatch(
        double hardcodedResult,
        double compilerResult,
        double interpreterResult,
        double fastInvokerResult)
    {
        return hardcodedResult == compilerResult &&
               hardcodedResult == interpreterResult &&
               hardcodedResult == fastInvokerResult;
    }
}
