namespace Example.Scenarios;

public static class PricingDiscountScenario
{
    private const string Formula = "price * 0.9 + fee";
    private const string DisallowedFormula = "let discount = 0.9\nprice * discount + fee";
    private const string GeneralDialectProfileName = "full-default-native";
    private const string RestrictedDialectProfileName = "pricing-restricted";
    private const double Price = 100.0;
    private const double Fee = 5.0;

    public static void Run(bool verbose)
    {
        var printer = new ScenarioConsolePrinter();
        var hardcodedPricingCalculator = new HardcodedPricingCalculator();
        using var generalDslPricingCalculator = new DslPricingCalculator(GeneralDialectProfileName);
        using var restrictedDslPricingCalculator = new DslPricingCalculator(RestrictedDialectProfileName);

        printer.PrintTitle("Pricing and Discount Demo");
        printer.PrintFormula(Formula, Price, Fee);

        var hardcodedResult = hardcodedPricingCalculator.Calculate(Price, Fee);

        var generalCompilerResult = generalDslPricingCalculator.CalculateWithCompiler(Formula, Price, Fee);
        var generalInterpreterResult = generalDslPricingCalculator.CalculateWithInterpreter(Formula, Price, Fee);
        var generalFastInvokerResult = generalDslPricingCalculator.CalculateWithFastInvoker(Formula, Price, Fee);
        var generalDialectResult = generalCompilerResult;

        var restrictedCompilerResult = restrictedDslPricingCalculator.CalculateWithCompiler(Formula, Price, Fee);
        var restrictedInterpreterResult = restrictedDslPricingCalculator.CalculateWithInterpreter(Formula, Price, Fee);
        var restrictedPricingResult = restrictedCompilerResult;

        var restrictedPositiveAttempt = restrictedDslPricingCalculator.TryCompileWithInterpreter(Formula);

        var restrictedStatementStyleBindingAttempt = restrictedDslPricingCalculator.TryCompileWithInterpreter(DisallowedFormula);
        var restrictedRejectsUnsupportedStatementStyleBindings = !restrictedStatementStyleBindingAttempt.IsSuccess;

        var allResultsMatch = ResultsMatch(
            hardcodedResult,
            generalCompilerResult,
            generalInterpreterResult,
            generalFastInvokerResult,
            restrictedCompilerResult,
            restrictedInterpreterResult);

        printer.PrintProductSummary(
            hardcodedResult,
            generalDialectResult,
            restrictedPricingResult,
            restrictedRejectsUnsupportedStatementStyleBindings,
            allResultsMatch);

        if (verbose)
        {
            printer.PrintSummarySeparator();
            printer.PrintResult("General dialect compiler result", generalCompilerResult);
            printer.PrintResult("General dialect interpreter result", generalInterpreterResult);
            printer.PrintResult("General dialect fast invoker result", generalFastInvokerResult);
            printer.PrintResult("Restricted pricing compiler result", restrictedCompilerResult);
            printer.PrintResult("Restricted pricing interpreter result", restrictedInterpreterResult);
            Console.WriteLine($"Restricted pricing accepts positive formula: {restrictedPositiveAttempt.IsSuccess}");
            Console.WriteLine($"Restricted pricing statement-style binding rejection reason: {restrictedStatementStyleBindingAttempt.ErrorMessage}");
        }
    }

    private static bool ResultsMatch(
        double hardcodedResult,
        double generalCompilerResult,
        double generalInterpreterResult,
        double generalFastInvokerResult,
        double restrictedCompilerResult,
        double restrictedInterpreterResult)
    {
        return hardcodedResult == generalCompilerResult &&
               hardcodedResult == generalInterpreterResult &&
               hardcodedResult == generalFastInvokerResult &&
               hardcodedResult == restrictedCompilerResult &&
               hardcodedResult == restrictedInterpreterResult;
    }
}
