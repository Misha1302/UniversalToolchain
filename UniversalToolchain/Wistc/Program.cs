using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Dialects.Wist.Facade;
using UniversalToolchain.Rules.Abstractions;

return Parser.Default.ParseArguments<RunOptions, ReplOptions, DialectInspectOptions, DialectDemoOptions, FeaturesOptions, RuleSchemaOptions, RuleRunOptions>(args)
    .MapResult(
        (RunOptions opts) => RunCommand(opts),
        (ReplOptions opts) => ReplCommand(opts),
        (DialectInspectOptions opts) => DialectInspectCommand(opts),
        (DialectDemoOptions opts) => DialectDemoCommand(opts),
        (FeaturesOptions opts) => FeaturesCommand(opts),
        (RuleSchemaOptions opts) => RuleSchemaCommand(opts),
        (RuleRunOptions opts) => RuleRunCommand(opts),
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

int RuleSchemaCommand(RuleSchemaOptions options)
{
    try
    {
        using var facade = CreateFacade(options.DialectFile);
        var source = ReadRequiredFile(options.Source);
        var result = facade.CompileRuleSet(source, options.Backend);

        if (!result.IsSuccess || result.RuleSet == null)
        {
            PrintDiagnostics(result.Diagnostics);
            return 1;
        }

        PrintSchema(result.RuleSet.GetSchema());
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

int RuleRunCommand(RuleRunOptions options)
{
    try
    {
        using var facade = CreateFacade(options.DialectFile);
        var source = ReadRequiredFile(options.Source);
        var compileResult = facade.CompileRuleSet(source, options.Backend);

        if (!compileResult.IsSuccess || compileResult.RuleSet == null)
        {
            PrintDiagnostics(compileResult.Diagnostics);
            return 1;
        }

        var arguments = ParseRuleArguments(options.Arguments);
        var runResult = compileResult.RuleSet.TryRun(options.Rule, arguments);
        if (!runResult.IsSuccess)
        {
            PrintDiagnostics(runResult.Diagnostics);
            return 1;
        }

        Console.WriteLine($"rule: {options.Rule}");
        Console.WriteLine($"backend: {options.Backend}");
        Console.WriteLine($"result: {runResult.Value}");
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

WistRuntimeFacade CreateFacade(string dialectFile)
{
    return WistRuntimeFacadeBuilder
        .CreateDefault()
        .WithDialectFile(dialectFile)
        .Build();
}

string ReadRequiredFile(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        Thrower.Argument(nameof(path), "File path must not be empty.");

    if (!File.Exists(path))
        Thrower.FileNotFound(path);

    return File.ReadAllText(path);
}

Dictionary<string, object?> ParseRuleArguments(IEnumerable<string> rawArguments)
{
    var arguments = new Dictionary<string, object?>(StringComparer.Ordinal);
    foreach (var rawArgument in rawArguments.OrderBy(static x => x, StringComparer.Ordinal))
    {
        var separatorIndex = rawArgument.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == rawArgument.Length - 1)
            Thrower.Argument(nameof(rawArguments), $"Invalid rule argument '{rawArgument}'. Expected key=value.");

        var name = rawArgument[..separatorIndex].Trim();
        var valueText = rawArgument[(separatorIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(name))
            Thrower.Argument(nameof(rawArguments), $"Invalid rule argument '{rawArgument}'. Argument name must not be empty.");

        arguments[name] = ParseRuleArgumentValue(valueText);
    }

    return arguments;
}

object ParseRuleArgumentValue(string valueText)
{
    if (bool.TryParse(valueText, out var boolValue))
        return boolValue;

    if (double.TryParse(
            valueText,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var doubleValue))
    {
        return doubleValue;
    }

    return valueText;
}

void PrintSchema(RuleSetSchema schema)
{
    foreach (var rule in schema.Rules.OrderBy(static x => x.Name, StringComparer.Ordinal))
    {
        Console.WriteLine($"rule {rule.Name} -> {rule.ReturnType}");
        foreach (var parameter in rule.Parameters.OrderBy(static x => x.Name, StringComparer.Ordinal))
            Console.WriteLine($"  {parameter.Name}: {parameter.Type}");
    }
}

void PrintDiagnostics(IEnumerable<ToolchainDiagnostic> diagnostics)
{
    foreach (var diagnostic in diagnostics.OrderBy(static x => x.Code, StringComparer.Ordinal).ThenBy(static x => x.Message, StringComparer.Ordinal))
        Console.Error.WriteLine($"{diagnostic.Severity} {diagnostic.Code}: {diagnostic.Message}");
}

WistDialectExecutionHost CreateDefaultHost(CommonOptions options)
{
    using var provider = CreateDialectWorkflowProvider();
    var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
    var plan = new WistCliDialectPlanBuilder().Build(options);

    return plan.Kind switch
    {
        WistCliDialectPlanKind.Preset => CreateHostFromPreset(workflow, plan.BasePreset),
        WistCliDialectPlanKind.CustomizedPreset => CreateHostFromCustomizedPresetPlan(workflow, plan),
        _ => Thrower.ArgumentOutOfRange<WistDialectExecutionHost>(nameof(plan.Kind), $"Unsupported CLI dialect plan kind '{plan.Kind}'.")
    };
}

WistDialectExecutionHost CreateHostFromCustomizedPresetPlan(WistDialectExecutionWorkflow workflow, WistCliDialectPlan plan)
{
    workflow = workflow.ArgNotNull();
    plan = plan.ArgNotNull();

    if (plan.Kind != WistCliDialectPlanKind.CustomizedPreset)
        Thrower.Argument(nameof(plan), "CLI dialect plan must be of kind CustomizedPreset.");

    var dialectText = plan.CustomizedDialectText;
    if (string.IsNullOrWhiteSpace(dialectText))
        Thrower.Argument(nameof(plan), "Customized preset plan must provide dialect text.");

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
