namespace Example.Scenarios;

public sealed class ScenarioConsolePrinter
{
    public void PrintTitle(string title)
    {
        Console.WriteLine(title);
        PrintSeparator();
    }

    public void PrintFormula(string formula, double price, double fee)
    {
        Console.WriteLine($"Formula: {formula}");
        Console.WriteLine($"Input: price = {price}, fee = {fee}");
        PrintSeparator();
    }

    public void PrintResult(string label, double value)
    {
        Console.WriteLine($"{label}: {value}");
    }

    public void PrintProductSummary(
        double hardcodedResult,
        double generalDialectResult,
        double restrictedPricingResult,
        bool restrictedRejectsUnsupportedStatementStyleBindings,
        bool allResultsMatch)
    {
        PrintResult("Hardcoded result", hardcodedResult);
        PrintResult("General dialect result", generalDialectResult);
        PrintResult("Restricted pricing result", restrictedPricingResult);
        Console.WriteLine($"Restricted pricing rejects unsupported statement-style bindings: {restrictedRejectsUnsupportedStatementStyleBindings}");
        PrintSummary(allResultsMatch);
    }

    public void PrintSummary(bool allResultsMatch)
    {
        PrintSeparator();
        Console.WriteLine($"All results match: {allResultsMatch}");
    }

    public void PrintSummarySeparator()
    {
        PrintSeparator();
    }

    private static void PrintSeparator()
    {
        Console.WriteLine();
    }
}
