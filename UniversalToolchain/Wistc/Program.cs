using UniversalToolchain.Wist;
using UniversalToolchain.Wist.LanguagePack;

if (TryRejectRemovedDialectMutationOption(args, out var removedDialectMutationExitCode))
    return removedDialectMutationExitCode;

var parseResult = WistCliParser.Parse(args);
if (!parseResult.IsSuccess)
{
    var hasHelp = false;
    foreach (var error in parseResult.Errors)
    {
        if (error.Message.StartsWith("Usage:", StringComparison.Ordinal))
        {
            Console.WriteLine(error.Message);
            hasHelp = true;
        }
        else
        {
            Console.Error.WriteLine(error.Message);
        }
    }
    return hasHelp ? 0 : 1;
}

return parseResult.Options switch
{
    RunOptions options => RunCommand(options),
    ReplOptions options => ReplCommand(options),
    DialectInspectOptions options => DialectInspectCommand(options),
    DialectDemoOptions options => DialectDemoCommand(options),
    FeaturesOptions options => FeaturesCommand(options),
    _ => throw new InvalidOperationException("The CLI parser returned an unsupported options type.")
};

static bool TryRejectRemovedDialectMutationOption(string[] args, out int exitCode)
{
    foreach (var arg in args)
    {
        var optionName = arg switch
        {
            "--use-native-math" => "use-native-math",
            "--include-module" => "include-module",
            "--exclude-module" => "exclude-module",
            _ when arg.StartsWith("--include-module=", StringComparison.Ordinal) => "include-module",
            _ when arg.StartsWith("--exclude-module=", StringComparison.Ordinal) => "exclude-module",
            _ => null
        };
        if (optionName == null)
            continue;
        Console.Error.WriteLine($"Option '{optionName}' is unknown");
        exitCode = 1;
        return true;
    }
    exitCode = 0;
    return false;
}

int RunCommand(RunOptions options)
{
    try
    {
        if (options.ListModules)
        {
            ListAllModules();
            return 0;
        }

        var code = GetCode(options);
        if (string.IsNullOrEmpty(code))
        {
            Console.Error.WriteLine("Error: No code provided. Use --file, --eval, or provide code as argument.");
            return 1;
        }

        using var engine = CreateEngine(options);
        var result = engine.Evaluate<object?>(code);
        if (result != null)
            Console.WriteLine(result);
        WriteTraceIfRequested(options, code, ResolveDialectLabel(options), result);
        return 0;
    }
    catch (WistException ex)
    {
        WriteFailureTraceIfRequested(options, ex);
        Console.Error.WriteLine(ex.ToString());
        if (Debugger.IsAttached)
            Console.Error.WriteLine(ex.StackTrace);
        return 1;
    }
    catch (Exception ex)
    {
        WriteFailureTraceIfRequested(options, ex);
        Console.Error.WriteLine($"Error: {ex.Message}");
        if (Debugger.IsAttached)
            Console.Error.WriteLine(ex.StackTrace);
        return 1;
    }
}

int ReplCommand(ReplOptions options)
{
    try
    {
        using var engine = CreateEngine(options);
        Console.WriteLine("Wist REPL (Ctrl+C to exit)");
        Console.WriteLine($"Backend: {options.Backend}");
        return new Repl(code => engine.Evaluate<object?>(code), options.HistoryFile).Run();
    }
    catch (WistException ex)
    {
        Console.Error.WriteLine(ex.ToString());
        return 1;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

int DialectInspectCommand(DialectInspectOptions options)
{
    try
    {
        var slice = CompileDialectFile(options.File);
        var backend = GetFirstEnabledBackend(slice);
        var engineOptions = WistEngineOptions.FromDialectFile(options.File);
        engineOptions.BackendId = backend;
        using var engine = WistEngine.Create(engineOptions);
        Console.WriteLine("Success: True");
        Console.WriteLine($"Dialect: {slice.Name}");
        Console.WriteLine($"Backend: {backend}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

int DialectDemoCommand(DialectDemoOptions options)
{
    try
    {
        const string inlineDialect = "dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter\nsecurity restricted";
        var source = string.IsNullOrWhiteSpace(options.File)
            ? inlineDialect
            : ReadRequiredFile(options.File);
        var sourceName = string.IsNullOrWhiteSpace(options.File) ? "demo-inline" : Path.GetFileName(options.File);
        using var compiler = new DialectDslCompiler();
        var slice = compiler.Compile(source);
        var backend = GetFirstEnabledBackend(slice);
        var engineOptions = WistEngineOptions.FromDialectText(source, sourceName);
        engineOptions.BackendId = backend;
        using var engine = WistEngine.Create(engineOptions);
        Console.WriteLine("Success: True");
        Console.WriteLine($"Dialect: {slice.Name}");
        Console.WriteLine($"Backend: {backend}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

int FeaturesCommand(FeaturesOptions options)
{
    try
    {
        var slice = CompileDialectFile(options.DialectFile);
        Console.WriteLine($"Dialect: {slice.Name}");
        Console.WriteLine($"Modules: {string.Join(", ", slice.UseModules.OrderBy(static x => x, StringComparer.Ordinal))}");
        Console.WriteLine($"Backends: {string.Join(", ", slice.BackendDirectives.Where(static x => x.Enabled).Select(static x => x.Backend.Value).OrderBy(static x => x, StringComparer.Ordinal))}");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
        return 1;
    }
}

WistEngine CreateEngine(CommonOptions options)
{
    WistEngineOptions engineOptions;
    if (!string.IsNullOrWhiteSpace(options.DialectFile))
    {
        EnsureFileExists(options.DialectFile);
        engineOptions = WistEngineOptions.FromDialectFile(options.DialectFile);
    }
    else
    {
        engineOptions = WistEngineOptions.FromPresetId(WistLanguageDefinitions.FullDefaultId);
    }
    engineOptions.BackendId = options.Backend;
    return WistEngine.Create(engineOptions);
}

static DialectDefinitionSlice CompileDialectFile(string path)
{
    var source = ReadRequiredFile(path);
    using var compiler = new DialectDslCompiler();
    return compiler.Compile(source);
}

static string GetFirstEnabledBackend(DialectDefinitionSlice slice) =>
    slice.BackendDirectives.FirstOrDefault(static directive => directive.Enabled)?.Backend.Value
    ?? throw new InvalidOperationException($"Dialect '{slice.Name}' does not enable any backend.");

string GetCode(RunOptions options)
{
    if (!string.IsNullOrEmpty(options.File))
        return ReadRequiredFile(options.File);
    if (options.Evaluate && !string.IsNullOrEmpty(options.Code))
        return options.Code;
    return options.Code ?? string.Empty;
}

static string ReadRequiredFile(string path)
{
    EnsureFileExists(path);
    return File.ReadAllText(path);
}

static void EnsureFileExists(string path)
{
    if (!File.Exists(path))
        Thrower.FileNotFound(path);
}

static string ResolveDialectLabel(CommonOptions options) =>
    string.IsNullOrWhiteSpace(options.DialectFile)
        ? WistLanguageDefinitions.FullDefaultId
        : Path.GetFileName(options.DialectFile);

void WriteTraceIfRequested(RunOptions options, string code, string dialect, object? result)
{
    if (!string.IsNullOrWhiteSpace(options.TracePath))
        WistCliTraceWriter.WriteSuccess(options.TracePath, code, dialect, options.Backend, result);
}

void WriteFailureTraceIfRequested(RunOptions options, Exception exception)
{
    if (!string.IsNullOrWhiteSpace(options.TracePath))
        WistCliTraceWriter.WriteFailure(options.TracePath, options.Code ?? string.Empty, options.DialectFile ?? "unknown", options.Backend, exception);
}

void ListAllModules() =>
    Console.Write(WistCliRuntimeListingFormatter.Format(new WistLanguageFeaturePackage().Descriptor));
