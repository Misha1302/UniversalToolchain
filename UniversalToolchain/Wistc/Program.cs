using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Facade;

return Parser.Default.ParseArguments<RunOptions, ReplOptions, DialectInspectOptions, DialectDemoOptions, FeaturesOptions>(args)
    .MapResult(
        (RunOptions opts) => RunCommand(opts),
        (ReplOptions opts) => ReplCommand(opts),
        (DialectInspectOptions opts) => DialectInspectCommand(opts),
        (DialectDemoOptions opts) => DialectDemoCommand(opts),
        (FeaturesOptions opts) => FeaturesCommand(opts),
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
            using var dialectHost = CreateDialectHost(options.DialectFile);
            var result = dialectHost.Run(code, options.Backend);
            if (result != null)
                Console.WriteLine(result);

            return 0;
        }

        using var host = CreateDefaultHost(options);
        var runtimeResult = host.Run(code, options.Backend);
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
