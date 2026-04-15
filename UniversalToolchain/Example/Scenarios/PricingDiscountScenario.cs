namespace Example.Scenarios;

public static class PricingDiscountScenario
{
    private const string Formula = "price * 0.9 + fee";
    private const string DisallowedFormula = "let discount = 0.9\nprice * discount + fee";
    private const string GeneralDialectProfileName = "full-default-native";
    private const string RestrictedDialectProfileName = "pricing-restricted";
    private const double Price = 100.0;
    private const double Fee = 5.0;

    public static void Run()
    {
        var printer = new ScenarioConsolePrinter();
        var hardcodedPricingCalculator = new HardcodedPricingCalculator();
        using var generalDslPricingCalculator = new DslPricingCalculator(GeneralDialectProfileName);
        using var restrictedDslPricingCalculator = new DslPricingCalculator(RestrictedDialectProfileName);

        printer.PrintTitle("Pricing and Discount Demo");
        printer.PrintFormula(Formula, Price, Fee);

        var hardcodedResult = hardcodedPricingCalculator.Calculate(Price, Fee);
        printer.PrintResult("Hardcoded result", hardcodedResult);

        var generalCompilerResult = generalDslPricingCalculator.CalculateWithCompiler(Formula, Price, Fee);
        printer.PrintResult("General dialect compiler result", generalCompilerResult);

        var generalInterpreterResult = generalDslPricingCalculator.CalculateWithInterpreter(Formula, Price, Fee);
        printer.PrintResult("General dialect interpreter result", generalInterpreterResult);

        var generalFastInvokerResult = generalDslPricingCalculator.CalculateWithFastInvoker(Formula, Price, Fee);
        printer.PrintResult("General dialect fast invoker result", generalFastInvokerResult);

        var restrictedCompilerResult = restrictedDslPricingCalculator.CalculateWithCompiler(Formula, Price, Fee);
        printer.PrintResult("Restricted pricing compiler result", restrictedCompilerResult);

        var restrictedInterpreterResult = restrictedDslPricingCalculator.CalculateWithInterpreter(Formula, Price, Fee);
        printer.PrintResult("Restricted pricing interpreter result", restrictedInterpreterResult);

        var restrictedPositiveAttempt = restrictedDslPricingCalculator.TryCompileWithInterpreter(Formula);
        Console.WriteLine($"Restricted pricing accepts positive formula: {restrictedPositiveAttempt.IsSuccess}");

        var restrictedStatementStyleBindingAttempt = restrictedDslPricingCalculator.TryCompileWithInterpreter(DisallowedFormula);
        var restrictedRejectsUnsupportedStatementStyleBindings = !restrictedStatementStyleBindingAttempt.IsSuccess;
        Console.WriteLine($"Restricted pricing rejects unsupported statement-style bindings: {restrictedRejectsUnsupportedStatementStyleBindings}");
        Console.WriteLine($"Restricted pricing statement-style binding rejection reason: {restrictedStatementStyleBindingAttempt.ErrorMessage}");

        var allResultsMatch = ResultsMatch(
            hardcodedResult,
            generalCompilerResult,
            generalInterpreterResult,
            generalFastInvokerResult,
            restrictedCompilerResult,
            restrictedInterpreterResult);

        printer.PrintSummary(allResultsMatch);
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
