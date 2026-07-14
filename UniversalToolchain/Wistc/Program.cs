using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Dialects.Wist;

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

        if (!string.IsNullOrWhiteSpace(options.DialectFile))
        {
            using var dialectHost = CreateDialectHost(options.DialectFile);
            var result = dialectHost.Run(code, options.Backend);
            if (result != null)
                Console.WriteLine(result);

            WriteTraceIfRequested(options, code, dialectHost.Configuration.DialectName, result);
            return 0;
        }

        using var host = CreateDefaultHost(options);
        var runtimeResult = host.Run(code, options.Backend);
        if (runtimeResult != null)
            Console.WriteLine(runtimeResult);

        WriteTraceIfRequested(options, code, host.Configuration.DialectName, runtimeResult);
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

void WriteTraceIfRequested(RunOptions options, string code, string dialect, object? result)
{
    if (string.IsNullOrWhiteSpace(options.TracePath))
        return;

    WistCliTraceWriter.WriteSuccess(options.TracePath, code, dialect, options.Backend, result);
}

void WriteFailureTraceIfRequested(RunOptions options, Exception exception)
{
    if (string.IsNullOrWhiteSpace(options.TracePath))
        return;

    WistCliTraceWriter.WriteFailure(options.TracePath, options.Code ?? string.Empty, options.DialectFile ?? "unknown", options.Backend, exception);
}

int ReplCommand(ReplOptions options)
{
    try
    {
        if (!string.IsNullOrWhiteSpace(options.DialectFile))
        {
            using var dialectHost = CreateDialectHost(options.DialectFile);
            Console.WriteLine("Wist REPL (Ctrl+C to exit)");
            Console.WriteLine($"Backend: {options.Backend}");

            var dialectRepl = new Repl(dialectHost.GetCore(options.Backend), options.HistoryFile);
            return dialectRepl.Run();
        }

        using var defaultHost = CreateDefaultHost(options);

        Console.WriteLine("Wist REPL (Ctrl+C to exit)");
        Console.WriteLine($"Backend: {options.Backend}");

        var defaultRepl = new Repl(defaultHost.GetCore(options.Backend), options.HistoryFile);
        return defaultRepl.Run();
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

int DialectDemoCommand(DialectDemoOptions options)
{
    try
    {
        using var provider = CreateDialectWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var report = string.IsNullOrWhiteSpace(options.File)
            ? workflow.ComposeText("""
                                   dialect Demo
                                   use Arithmetic,Numbers
                                   backend interpreter
                                   """, "demo-inline")
            : workflow.ComposeFile(options.File);

        Console.WriteLine(FormatComposition(report));
        return report.IsSuccess ? 0 : 1;
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

int FeaturesCommand(FeaturesOptions options)
{
    try
    {
        using var provider = CreateDialectWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var runtimeComponentCatalog = provider.GetRequiredService<IRuntimeComponentCatalog>();
        var typeLoader = provider.GetRequiredService<IRuntimeComponentTypeLoader>();
        var composition = workflow.ComposeFile(options.DialectFile);

        if (!composition.IsSuccess)
        {
            Console.Error.WriteLine(FormatComposition(composition));
            return 1;
        }

        var selectedRuntimePlan = (SelectedRuntimePlan)composition.RuntimeSelection!;
        var knownCatalog = new KnownCapabilityCatalogBuilder(typeLoader).Build(runtimeComponentCatalog);
        var selectedCatalog = new SelectedCapabilityCatalogBuilder(typeLoader).Build(selectedRuntimePlan);
        var explanation = DialectFeatureExplanationProjector.Project(
            knownCatalog,
            selectedCatalog,
            selectedRuntimePlan,
            composition.BuildPlan!.Name);

        Console.WriteLine(DialectFeatureExplanationFormatter.FormatDeterministic(explanation));
        return 0;
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
        using var provider = CreateDialectWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(options.File);
        Console.WriteLine(FormatComposition(result));
        return result.IsSuccess ? 0 : 1;
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

string GetCode(RunOptions options)
{
    if (!string.IsNullOrEmpty(options.File))
    {
        if (!File.Exists(options.File))
            Thrower.FileNotFound(options.File);

        return File.ReadAllText(options.File);
    }

    if (options.Evaluate && !string.IsNullOrEmpty(options.Code))
        return options.Code;

    return options.Code ?? string.Empty;
}

WistDialectExecutionHost CreateDefaultHost(CommonOptions options)
{
    using var provider = CreateDialectWorkflowProvider();
    var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
    var plan = new WistCliDialectPlanBuilder().Build(options);

    return plan.Kind switch
    {
        WistCliDialectPlanKind.Preset => CreateHostFromPreset(workflow, plan.BasePreset),
        _ => Thrower.ArgumentOutOfRange<WistDialectExecutionHost>(nameof(plan.Kind), $"Unsupported CLI dialect plan kind '{plan.Kind}'.")
    };
}

WistDialectExecutionHost CreateHostFromPreset(WistDialectExecutionWorkflow workflow, WistShippedDialectPreset preset)
{
    var dialectFilePath = new WistShippedDialectFileResolver().Resolve(preset);
    var composition = workflow.ComposeFile(dialectFilePath);

    if (!composition.IsSuccess)
        Thrower.InvalidOpEx(FormatComposition(composition));

    return workflow.CreateHost(composition);
}

WistDialectExecutionHost CreateDialectHost(string dialectFile)
{
    using var provider = CreateDialectWorkflowProvider();
    var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
    var result = workflow.ComposeFile(dialectFile);

    if (!result.IsSuccess)
        Thrower.InvalidOpEx(FormatComposition(result));

    return workflow.CreateHost(result);
}

ServiceProvider CreateDialectWorkflowProvider()
{
    var services = new ServiceCollection();
    services.AddWistDialectServices();
    return services.BuildServiceProvider();
}

static string FormatComposition(DialectFrameworkCompositionResult result)
{
    return DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(result));
}

void ListAllModules()
{
    using var provider = CreateDialectWorkflowProvider();
    var catalog = provider.GetRequiredService<IRuntimeComponentCatalog>();
    Console.Write(WistCliRuntimeListingFormatter.Format(catalog));
}
