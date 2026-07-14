namespace Wistc;

public static class WistCliParser
{
    private const string Usage = """
        Usage: wistc <command> [options]

        Commands:
          run              Run Wist code.
          repl             Start interactive mode (default when no arguments are supplied).
          dialect-inspect  Parse and validate a dialect file.
          dialect-demo     Run the dialect composition demo.
          features         List provider-discovered dialect features.

        Use 'wistc <command> --help' to see command options.
        """;

    public static WistCliParseResult Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        if (args.Count == 0)
            return WistCliParseResult.Success(new ReplOptions());

        if (IsHelp(args[0]))
            return Help(Usage);

        var command = args[0];
        var commandArgs = args.Skip(1).ToArray();
        return command switch
        {
            "run" => ParseRun(commandArgs),
            "repl" => ParseRepl(commandArgs),
            "dialect-inspect" => ParseDialectInspect(commandArgs),
            "dialect-demo" => ParseDialectDemo(commandArgs),
            "features" => ParseFeatures(commandArgs),
            _ when command.StartsWith("-", StringComparison.Ordinal) => UnknownOption(command),
            _ => Failure($"Unknown command '{command}'.")
        };
    }

    private static WistCliParseResult ParseRun(IReadOnlyList<string> args)
    {
        if (ContainsHelp(args))
            return Help("Usage: wistc run [code] [--eval] [--file <path>] [--backend <id>] [--dialect-file <path>] [--trace <path>] [--list-modules]");

        var options = new RunOptions();
        var positionals = new List<string>();
        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "-e" or "--eval":
                    options.Evaluate = true;
                    break;
                case "--list-modules":
                    options.ListModules = true;
                    break;
                case "-b" or "--backend":
                    if (!TryReadValue(args, ref index, argument, out var backend, out var error))
                        return error!;
                    options.Backend = backend!;
                    break;
                case "-f" or "--file":
                    if (!TryReadValue(args, ref index, argument, out var file, out error))
                        return error!;
                    options.File = file;
                    break;
                case "--dialect-file":
                    if (!TryReadValue(args, ref index, argument, out var dialectFile, out error))
                        return error!;
                    options.DialectFile = dialectFile;
                    break;
                case "--trace":
                    if (!TryReadValue(args, ref index, argument, out var tracePath, out error))
                        return error!;
                    options.TracePath = tracePath;
                    break;
                default:
                    if (TrySplitLongOption(argument, out var optionName, out var optionValue))
                    {
                        switch (optionName)
                        {
                            case "backend": options.Backend = optionValue; break;
                            case "file": options.File = optionValue; break;
                            case "dialect-file": options.DialectFile = optionValue; break;
                            case "trace": options.TracePath = optionValue; break;
                            default: return UnknownOption("--" + optionName);
                        }
                    }
                    else if (argument.StartsWith("-", StringComparison.Ordinal))
                    {
                        return UnknownOption(argument);
                    }
                    else
                    {
                        positionals.Add(argument);
                    }
                    break;
            }
        }

        if (positionals.Count > 1)
            return Failure("Only one code argument may be provided.");

        options.Code = positionals.SingleOrDefault();
        return WistCliParseResult.Success(options);
    }

    private static WistCliParseResult ParseRepl(IReadOnlyList<string> args)
    {
        if (ContainsHelp(args))
            return Help("Usage: wistc repl [--backend <id>] [--dialect-file <path>] [--history <path>] [--list-modules]");

        var options = new ReplOptions();
        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--list-modules":
                    options.ListModules = true;
                    break;
                case "-b" or "--backend":
                    if (!TryReadValue(args, ref index, argument, out var backend, out var error))
                        return error!;
                    options.Backend = backend!;
                    break;
                case "--dialect-file":
                    if (!TryReadValue(args, ref index, argument, out var dialectFile, out error))
                        return error!;
                    options.DialectFile = dialectFile;
                    break;
                case "--history":
                    if (!TryReadValue(args, ref index, argument, out var history, out error))
                        return error!;
                    options.HistoryFile = history;
                    break;
                default:
                    if (TrySplitLongOption(argument, out var optionName, out var optionValue))
                    {
                        switch (optionName)
                        {
                            case "backend": options.Backend = optionValue; break;
                            case "dialect-file": options.DialectFile = optionValue; break;
                            case "history": options.HistoryFile = optionValue; break;
                            default: return UnknownOption("--" + optionName);
                        }
                    }
                    else
                    {
                        return argument.StartsWith("-", StringComparison.Ordinal)
                            ? UnknownOption(argument)
                            : Failure($"Unexpected argument '{argument}'.");
                    }
                    break;
            }
        }

        return WistCliParseResult.Success(options);
    }

    private static WistCliParseResult ParseDialectInspect(IReadOnlyList<string> args)
    {
        if (ContainsHelp(args))
            return Help("Usage: wistc dialect-inspect --file <path>");

        var options = new DialectInspectOptions();
        var result = ParseSingleFileOption(args, value => options.File = value);
        if (result is not null)
            return result;

        return string.IsNullOrWhiteSpace(options.File)
            ? Failure("Required option '--file' is missing.")
            : WistCliParseResult.Success(options);
    }

    private static WistCliParseResult ParseDialectDemo(IReadOnlyList<string> args)
    {
        if (ContainsHelp(args))
            return Help("Usage: wistc dialect-demo [--file <path>] [--scenario <name>]");

        var options = new DialectDemoOptions();
        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "-f" or "--file":
                    if (!TryReadValue(args, ref index, argument, out var file, out var error))
                        return error!;
                    options.File = file;
                    break;
                case "-s" or "--scenario":
                    if (!TryReadValue(args, ref index, argument, out var scenario, out error))
                        return error!;
                    options.Scenario = scenario!;
                    break;
                default:
                    if (TrySplitLongOption(argument, out var optionName, out var optionValue))
                    {
                        switch (optionName)
                        {
                            case "file": options.File = optionValue; break;
                            case "scenario": options.Scenario = optionValue; break;
                            default: return UnknownOption("--" + optionName);
                        }
                    }
                    else
                    {
                        return argument.StartsWith("-", StringComparison.Ordinal)
                            ? UnknownOption(argument)
                            : Failure($"Unexpected argument '{argument}'.");
                    }
                    break;
            }
        }

        return WistCliParseResult.Success(options);
    }

    private static WistCliParseResult ParseFeatures(IReadOnlyList<string> args)
    {
        if (ContainsHelp(args))
            return Help("Usage: wistc features --dialect-file <path>");

        var options = new FeaturesOptions();
        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (argument == "--dialect-file")
            {
                if (!TryReadValue(args, ref index, argument, out var dialectFile, out var error))
                    return error!;
                options.DialectFile = dialectFile!;
            }
            else if (TrySplitLongOption(argument, out var optionName, out var optionValue) && optionName == "dialect-file")
            {
                options.DialectFile = optionValue;
            }
            else
            {
                return argument.StartsWith("-", StringComparison.Ordinal)
                    ? UnknownOption(argument)
                    : Failure($"Unexpected argument '{argument}'.");
            }
        }

        return string.IsNullOrWhiteSpace(options.DialectFile)
            ? Failure("Required option '--dialect-file' is missing.")
            : WistCliParseResult.Success(options);
    }

    private static WistCliParseResult? ParseSingleFileOption(IReadOnlyList<string> args, Action<string> setter)
    {
        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (argument is "-f" or "--file")
            {
                if (!TryReadValue(args, ref index, argument, out var file, out var error))
                    return error;
                setter(file!);
            }
            else if (TrySplitLongOption(argument, out var optionName, out var optionValue) && optionName == "file")
            {
                setter(optionValue);
            }
            else
            {
                return argument.StartsWith("-", StringComparison.Ordinal)
                    ? UnknownOption(argument)
                    : Failure($"Unexpected argument '{argument}'.");
            }
        }

        return null;
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out string? value,
        out WistCliParseResult? error)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            value = null;
            error = Failure($"Option '{option}' requires a value.");
            return false;
        }

        value = args[++index];
        error = null;
        return true;
    }

    private static bool TrySplitLongOption(string argument, out string name, out string value)
    {
        name = string.Empty;
        value = string.Empty;
        if (!argument.StartsWith("--", StringComparison.Ordinal))
            return false;

        var separator = argument.IndexOf('=');
        if (separator <= 2 || separator == argument.Length - 1)
            return false;

        name = argument[2..separator];
        value = argument[(separator + 1)..];
        return true;
    }

    private static bool ContainsHelp(IEnumerable<string> args) => args.Any(IsHelp);
    private static bool IsHelp(string value) => value is "--help" or "-h";
    private static WistCliParseResult Help(string message) => WistCliParseResult.Failure(new WistCliParseError(message));
    private static WistCliParseResult UnknownOption(string option) => Failure($"Unknown option '{option}'.");
    private static WistCliParseResult Failure(string message) => WistCliParseResult.Failure(new WistCliParseError(message));
}
