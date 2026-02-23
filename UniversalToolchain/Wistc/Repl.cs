using BasicCore.Contracts;

namespace Wistc;

public class Repl
{
    private readonly ICoreRunnable _core;
    private readonly List<string> _history = new();
    private readonly string? _historyFile;

    public Repl(ICoreRunnable core, string? historyFile = null)
    {
        _core = core;
        _historyFile = historyFile;
        LoadHistory();
    }

    public int Run()
    {
        string? input;
        while (true)
        {
            Console.Write("> ");
            input = ReadLine();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            if (input.Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                Console.Clear();
                continue;
            }

            if (input.Equals("history", StringComparison.OrdinalIgnoreCase))
            {
                ShowHistory();
                continue;
            }

            try
            {
                var result = _core.Run(input);
                if (result != null)
                    Console.WriteLine($"= {result}");

                _history.Add(input);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        SaveHistory();
        return 0;
    }

    private string? ReadLine()
    {
        var input = Console.ReadLine();
        if (!string.IsNullOrEmpty(input))
            input = input.Trim();
        return input;
    }

    private void LoadHistory()
    {
        if (_historyFile != null && File.Exists(_historyFile))
            try
            {
                _history.AddRange(File.ReadAllLines(_historyFile));
            }
            catch
            {
                /* Ignore */
            }
    }

    private void SaveHistory()
    {
        if (_historyFile != null && _history.Any())
            try
            {
                File.WriteAllLines(_historyFile, _history.TakeLast(100));
            }
            catch
            {
                /* Ignore */
            }
    }

    private void ShowHistory()
    {
        Console.WriteLine("History:");
        foreach (var line in _history.TakeLast(20))
            Console.WriteLine($"  {line}");
    }
}