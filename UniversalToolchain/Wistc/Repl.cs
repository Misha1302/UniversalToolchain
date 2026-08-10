namespace Wistc;

public class Repl
{
    private readonly Func<string, object?> _run;
    private readonly List<string> _history = new();
    private readonly string? _historyFile;

    public Repl(Func<string, object?> run, string? historyFile = null)
    {
        _run = run ?? throw new ArgumentNullException(nameof(run));
        _historyFile = historyFile;
        LoadHistory();
    }

    public int Run()
    {
        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine()?.Trim();
            if (input == null)
                break;
            if (string.IsNullOrWhiteSpace(input))
                continue;
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
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
                var result = _run(input);
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

    private void LoadHistory()
    {
        if (_historyFile != null && File.Exists(_historyFile))
        {
            try { _history.AddRange(File.ReadAllLines(_historyFile)); }
            catch { }
        }
    }

    private void SaveHistory()
    {
        if (_historyFile != null && _history.Any())
        {
            try { File.WriteAllLines(_historyFile, _history.TakeLast(100)); }
            catch { }
        }
    }

    private void ShowHistory()
    {
        Console.WriteLine("History:");
        foreach (var line in _history.TakeLast(20))
            Console.WriteLine($"  {line}");
    }
}
