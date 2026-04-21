using UniversalToolchain.Dialects.Wist;

return Parser.Default.ParseArguments<RunOptions, ReplOptions, DialectInspectOptions, DialectDemoOptions>(args)
    .MapResult(
        (RunOptions opts) => RunCommand(opts),
        (ReplOptions opts) => ReplCommand(opts),
        (DialectInspectOptions opts) => DialectInspectCommand(opts),
        (DialectDemoOptions opts) => DialectDemoCommand(opts),
        _ => 1);

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
            ValidateDialectExecutionOptions(options);
            using var dialectHost = CreateDialectHost(options.DialectFile);
            var result = dialectHost.Run(code, options.Mode);
            if (result != null)
                Console.WriteLine(result);

            return 0;
        }

        using var host = CreateDefaultHost(options);
        var runtimeResult = host.Run(code, options.Mode);
        if (runtimeResult != null)
            Console.WriteLine(runtimeResult);

        return 0;
    }
    catch (WistException ex)
    {
        Console.Error.WriteLine(ex.ToString());
        if (Debugger.IsAttached)
            Console.Error.WriteLine(ex.StackTrace);

        return 1;
    }
    catch (Exception ex)
    {
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
        if (!string.IsNullOrWhiteSpace(options.DialectFile))
        {
            ValidateDialectExecutionOptions(options);
            using var dialectHost = CreateDialectHost(options.DialectFile);
            Console.WriteLine("Wist REPL (Ctrl+C to exit)");
            Console.WriteLine($"Mode: {options.Mode}");

            var dialectRepl = new Repl(dialectHost.GetCore(options.Mode), options.HistoryFile);
            return dialectRepl.Run();
        }

        using var defaultHost = CreateDefaultHost(options);

        Console.WriteLine("Wist REPL (Ctrl+C to exit)");
        Console.WriteLine($"Mode: {options.Mode}");

        var defaultRepl = new Repl(defaultHost.GetCore(options.Mode), options.HistoryFile);
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
    var customization = WistCliCustomizationRequest.FromOptions(options);

    if (!customization.HasCustomization)
        return CreateHostFromPreset(workflow, WistShippedDialectPresets.Default);

    var dialectText = new WistCliCustomizedDialectBuilder().Build(customization);
    var composition = workflow.ComposeText(dialectText, "cli-customized");
    if (!composition.IsSuccess)
        Thrower.InvalidOpEx(FormatComposition(composition));

    return workflow.CreateHost(composition);
}

WistDialectExecutionHost CreateHostFromPreset(WistDialectExecutionWorkflow workflow, WistShippedDialectPreset preset)
{
    var dialectFilePath = new WistShippedDialectFileResolver().Resolve(preset);
    var composition = workflow.ComposeFile(dialectFilePath);

    if (!composition.IsSuccess)
        Thrower.InvalidOpEx(FormatComposition(composition));

    return workflow.CreateHost(composition);
}

void ValidateDialectExecutionOptions(CommonOptions options)
{
    var customization = WistCliCustomizationRequest.FromOptions(options);
    if (!customization.HasCustomization)
        return;

    if (customization.UseNativeMath)
        Thrower.Argument(nameof(options.UseNativeMath), "The --use-native-math option cannot be combined with --dialect-file. Configure arithmetic through the dialect definition instead.");

    if (customization.IncludeModules.Count > 0)
        Thrower.Argument(nameof(options.IncludeModules), "The --include-module option cannot be combined with --dialect-file. Configure modules through the dialect definition instead.");

    if (customization.ExcludeModules.Count > 0)
        Thrower.Argument(nameof(options.ExcludeModules), "The --exclude-module option cannot be combined with --dialect-file. Configure modules through the dialect definition instead.");
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
    services.AddWistCilBackend();
    services.AddWistInterpreterBackend();
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
