using AbstractIrConverters;
using BasicInterpreter;
using IntermediateRepresentationAbstractions;

namespace Wistc;

public static class Program
{
    private static bool _verbose;

    public static int Main(string[] args)
    {
        BasicStdLib.Main.LoadStdLibToThisAssembly();

        try
        {
            var parser = new Parser(with =>
            {
                with.AutoHelp = false;
                with.AutoVersion = false;
                with.HelpWriter = null;
            });

            var parserResult = parser.ParseArguments<Options>(args);

            return parserResult.MapResult(Run, HandleErrors);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex.Message}");
            if (_verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    private static int Run(Options options)
    {
        _verbose = options.Verbose;

        if (options.Help)
        {
            DisplayHelp();
            return 0;
        }

        if (options.Version)
        {
            DisplayVersion();
            return 0;
        }

        if (!options.Validate(out var errorMessage))
        {
            Console.Error.WriteLine($"Error: {errorMessage}");
            return 1;
        }

        if (_verbose)
        {
            Console.WriteLine("Verbose mode enabled");
            Console.WriteLine($"Source file: {options.SourcePath}");
        }

        try
        {
            // Create modules
            var (frontendModules, middleEndModules) = CreateModules(options);

            if (_verbose)
            {
                Console.WriteLine($"Loaded {frontendModules.Count} frontend modules");
                Console.WriteLine($"Loaded {middleEndModules.Count} middle-end modules");
            }

            // Create core
            var core = new BasicCoreImpl<IAbstractIR>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicAstToBytecodeTranslatorImpl(),
                () => new BytecodeToAbstractIrConverterImpl(),
                () => new AbstractIrToAbstractIrStub(),
                () => new InterpreterImpl(),
                frontendModules,
                middleEndModules
            );

            // Read source code
            var code = File.ReadAllText(options.SourcePath);

            if (_verbose)
            {
                Console.WriteLine($"Code length: {code.Length} characters");
                Console.WriteLine("Starting execution...");
            }

            // Execute code
            var result = core.Run(code);

            Console.WriteLine(result?.ToString() ?? "(null)");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Execution error: {ex.Message}");
            if (_verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 1;
        }
    }

    private static (List<IFrontendCoreModule> frontend, List<IMiddleEndCoreModule<IAbstractIR>> middleEnd)
        CreateModules(Options options)
    {
        var frontendModules = new List<IFrontendCoreModule>();
        var middleEndModules = new List<IMiddleEndCoreModule<IAbstractIR>>();

        // Base modules (include all by default)
        var baseModules = new Dictionary<string, IFrontendCoreModule>
        {
            ["Identifier"] = new IdentifierModuleImpl(),
            ["Scopes"] = new ScopesModuleImpl(),
            ["Numbers"] = new NumbersModuleImpl(),
            ["Whitespace"] = new WhitespaceModuleImpl(),
            ["SemicolonAsNewLine"] = new SemicolonAsNewLineModuleImpl(),
            ["Arithmetic"] = new ArithmeticModuleImpl(),
            ["CSharpInterop"] = new CSharpInteropModuleImpl(),
            ["Labels"] = new LabelsModuleImpl(),
            ["Variables"] = new VariablesModuleImpl(),
            ["Equality"] = new EqualityModuleImpl(),
            ["Conditions"] = new ConditionsModuleImpl(),
            ["Comparison"] = new ComparisonOperations(),
            ["Boolean"] = new BooleanOperations()
        };

        // Determine which modules to disable
        var disabledModules = options.DisableModules?.Select(m => m.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase)
                              ?? new HashSet<string>();

        // Add base modules (except disabled ones)
        foreach (var (name, module) in baseModules)
        {
            if (disabledModules.Contains(name))
            {
                if (_verbose) Console.WriteLine($"Skipping disabled module: {name}");
                continue;
            }

            frontendModules.Add(module);
            if (_verbose) Console.WriteLine($"Added module: {name}");
        }

        // Logging module
        if (!options.NoLogging && !disabledModules.Contains("Logger"))
        {
            var logsPath = options.LogsPath ?? "wistc.log";
            frontendModules.Add(new ExecutorDebugLoggerImpl(logsPath));
            if (_verbose) Console.WriteLine($"Added logging module (output: {logsPath})");
        }

        // Parser configuration module
        if (!string.IsNullOrWhiteSpace(options.ParserConfigPath))
        {
            var actionType = options.ParserConfigRead
                ? ActionType.ReadConfiguration
                : ActionType.DumpConfiguration; // Default dump

            frontendModules.Add(new ParserConfigurationModuleImpl(actionType, options.ParserConfigPath));
            if (_verbose) Console.WriteLine($"Added parser config module ({actionType})");
        }

        // Lexer configuration module
        if (!string.IsNullOrWhiteSpace(options.LexerConfigPath))
        {
            var actionType = options.LexerConfigRead
                ? ActionType.ReadConfiguration
                : ActionType.DumpConfiguration; // Default dump

            frontendModules.Add(new LexerConfigurationModuleImpl(actionType, options.LexerConfigPath));
            if (_verbose) Console.WriteLine($"Added lexer config module ({actionType})");
        }

        // Custom modules from DLL
        if (options.CustomModuleDlls != null)
        {
            foreach (var dllPath in options.CustomModuleDlls)
            {
                try
                {
                    var customModules = LoadCustomModules(dllPath);

                    foreach (var module in customModules)
                    {
                        if (module is IFrontendCoreModule frontendModule)
                        {
                            frontendModules.Add(frontendModule);
                            if (_verbose) Console.WriteLine($"Added custom frontend module from {Path.GetFileName(dllPath)}: {module.GetType().Name}");
                        }
                        else if (module is IMiddleEndCoreModule<IAbstractIR> middleEndModule)
                        {
                            middleEndModules.Add(middleEndModule);
                            if (_verbose) Console.WriteLine($"Added custom middle-end module from {Path.GetFileName(dllPath)}: {module.GetType().Name}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Warning: Failed to load modules from {dllPath}: {ex.Message}");
                    if (_verbose) Console.Error.WriteLine($"Details: {ex}");
                }
            }
        }

        return (frontendModules, middleEndModules);
    }

    private static List<object> LoadCustomModules(string dllPath)
    {
        var modules = new List<object>();
        var assembly = Assembly.LoadFrom(dllPath);

        foreach (var type in assembly.GetTypes())
        {
            try
            {
                if (type is not { IsAbstract: false, IsInterface: false })
                    continue;


                if (typeof(IFrontendCoreModule).IsAssignableFrom(type))
                {
                    if (Activator.CreateInstance(type) is IFrontendCoreModule module)
                    {
                        modules.Add(module);
                    }
                }


                else if (type.GetInterfaces().Any(i =>
                             i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IMiddleEndCoreModule<>) &&
                             i.GetGenericArguments()[0] == typeof(IAbstractIR)))
                {
                    var module = Activator.CreateInstance(type);
                    if (module != null)
                    {
                        modules.Add(module);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_verbose) Console.WriteLine($"Warning: Failed to load type {type.Name}: {ex.Message}");
            }
        }

        return modules;
    }

    private static int HandleErrors(IEnumerable<Error> errors)
    {
        var errorList = errors.ToList();

        if (errorList.Any(e => e is HelpRequestedError))
        {
            DisplayHelp();
            return 0;
        }

        if (errorList.Any(e => e is VersionRequestedError))
        {
            DisplayVersion();
            return 0;
        }

        Console.Error.WriteLine("Error parsing command line arguments:");
        foreach (var error in errorList)
        {
            Console.Error.WriteLine($"  {error.Tag}");
        }

        Console.Error.WriteLine();
        DisplayUsage();

        return 1;
    }

    private static void DisplayHelp()
    {
        DisplayVersion();
        Console.WriteLine();
        DisplayUsage();
        Console.WriteLine();
        DisplayExamples();
    }

    private static void DisplayVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version ?? new Version(1, 0, 0);

        Console.WriteLine($"Wistc Compiler v{version.Major}.{version.Minor}.{version.Build}");
        Console.WriteLine("A flexible compiler/interpreter with modular architecture");
    }

    private static void DisplayUsage()
    {
        Console.WriteLine("USAGE:");
        Console.WriteLine("  wistc --source <file> [OPTIONS]");
        Console.WriteLine();
        Console.WriteLine("REQUIRED:");
        Console.WriteLine("  -s, --source <file>     Path to source code file");
        Console.WriteLine();
        Console.WriteLine("OPTIONS:");
        Console.WriteLine("  -l, --logs <file>       Path to log file (default: wistc.log)");
        Console.WriteLine("      --no-logging        Disable logging");
        Console.WriteLine();
        Console.WriteLine("  Parser Configuration:");
        Console.WriteLine("      --parser-config <file>      Path to parser configuration file");
        Console.WriteLine("      --parser-config-read        Read parser configuration from file");
        Console.WriteLine("      --parser-config-dump        Dump parser configuration to file");
        Console.WriteLine();
        Console.WriteLine("  Lexer Configuration:");
        Console.WriteLine("      --lexer-config <file>      Path to lexer configuration file");
        Console.WriteLine("      --lexer-config-read        Read lexer configuration from file");
        Console.WriteLine("      --lexer-config-dump        Dump lexer configuration to file");
        Console.WriteLine();
        Console.WriteLine("  Module Management:");
        Console.WriteLine("      --disable-modules <list>    Disable specific modules (comma-separated)");
        Console.WriteLine("      --custom-modules <list>     Paths to custom module DLLs (comma-separated)");
        Console.WriteLine();
        Console.WriteLine("  Other Options:");
        Console.WriteLine("  -h, --help              Show this help message");
        Console.WriteLine("  -v, --verbose           Enable verbose output");
        Console.WriteLine("      --version           Show version information");
        Console.WriteLine();
        Console.WriteLine("DEFAULT MODULES:");
        Console.WriteLine("  Identifier, Scopes, Numbers, Whitespace, SemicolonAsNewLine,");
        Console.WriteLine("  Arithmetic, CSharpInterop, Labels, Variables, Equality,");
        Console.WriteLine("  Conditions, Comparison, Boolean");
    }

    private static void DisplayExamples()
    {
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  # Basic usage");
        Console.WriteLine("  wistc --source program.wt");
        Console.WriteLine();
        Console.WriteLine("  # With logging");
        Console.WriteLine("  wistc --source program.wt --logs output.log");
        Console.WriteLine();
        Console.WriteLine("  # Disable specific modules");
        Console.WriteLine("  wistc --source program.wt --disable-modules Conditions,Boolean");
        Console.WriteLine();
        Console.WriteLine("  # Use parser configuration");
        Console.WriteLine("  wistc --source program.wt --parser-config parser.txt --parser-config-dump");
        Console.WriteLine();
        Console.WriteLine("  # Load custom modules");
        Console.WriteLine("  wistc --source program.wt --custom-modules MyModule.dll");
        Console.WriteLine();
        Console.WriteLine("  # Verbose mode");
        Console.WriteLine("  wistc --source program.wt --verbose");
    }
}