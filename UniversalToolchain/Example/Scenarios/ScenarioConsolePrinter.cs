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

    public void PrintSummary(bool allResultsMatch)
    {
        PrintSeparator();
        Console.WriteLine($"All results match: {allResultsMatch}");
    }

    private static void PrintSeparator()
    {
        Console.WriteLine();
    }
}
