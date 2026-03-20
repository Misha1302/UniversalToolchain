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
            using var host = CreateDialectHost(options.DialectFile);
            var result = host.Run(code, options.Mode);
            if (result != null)
                Console.WriteLine(result);

            return 0;
        }

        var provider = BuildDefaultServiceProvider(options);
        var core = GetLegacyCoreRunnable(provider, options.Mode);

        var legacyResult = core.Run(code);
        if (legacyResult != null)
            Console.WriteLine(legacyResult);

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
            using var host = CreateDialectHost(options.DialectFile);
            Console.WriteLine("Wist REPL (Ctrl+C to exit)");
            Console.WriteLine($"Mode: {options.Mode}");

            var repl = new Repl(host.GetCore(options.Mode), options.HistoryFile);
            return repl.Run();
        }

        var provider = BuildDefaultServiceProvider(options);
        var core = GetLegacyCoreRunnable(provider, options.Mode);

        Console.WriteLine("Wist REPL (Ctrl+C to exit)");
        Console.WriteLine($"Mode: {options.Mode}");

        var legacyRepl = new Repl(core, options.HistoryFile);
        return legacyRepl.Run();
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
        var demoWorkflow = new DialectFrameworkDemoWorkflow(provider.GetRequiredService<DialectFrameworkCompositionWorkflow>());
        var registry = provider.GetRequiredService<DialectRuntimeDescriptorRegistry>();
        var report = CreateDemoReport(options, demoWorkflow, registry);

        Console.WriteLine(report.ToDeterministicText());
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

DialectFrameworkDemoReport CreateDemoReport(
    DialectDemoOptions options,
    DialectFrameworkDemoWorkflow demoWorkflow,
    DialectRuntimeDescriptorRegistry registry)
{
    if (!string.IsNullOrWhiteSpace(options.File))
    {
        if (!File.Exists(options.File))
            Thrower.FileNotFound(options.File);

        return demoWorkflow.RunSource(File.ReadAllText(options.File), registry, options.File);
    }

    var scenario = ParseDemoScenario(options.Scenario);
    return demoWorkflow.RunScenario(scenario, registry);
}

DialectFrameworkDemoScenario ParseDemoScenario(string scenarioText)
{
    if (string.IsNullOrWhiteSpace(scenarioText))
        return DialectFrameworkDemoScenario.Valid;

    return scenarioText.Trim().ToLowerInvariant() switch
    {
        "valid" => DialectFrameworkDemoScenario.Valid,
        "invalid-syntax" => DialectFrameworkDemoScenario.InvalidSyntax,
        "semantic-conflict" => DialectFrameworkDemoScenario.SemanticConflict,
        "unresolved-module" => DialectFrameworkDemoScenario.UnresolvedModule,
        _ => UnknownDemoScenario(scenarioText)
    };
}

DialectFrameworkDemoScenario UnknownDemoScenario(string scenarioText)
{
    Thrower.Argument(nameof(scenarioText), $"Unknown demo scenario '{scenarioText}'.");
    return DialectFrameworkDemoScenario.Valid;
}

int DialectInspectCommand(DialectInspectOptions options)
{
    try
    {
        using var provider = CreateDialectWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var result = workflow.ComposeFile(options.File);
        Console.WriteLine(result.ToDeterministicText());
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

IServiceProvider BuildDefaultServiceProvider(CommonOptions options)
{
    var services = new ServiceCollection();

    services.AddWistServices(wistOptions =>
        wistOptions.ArithmeticMode = options.UseNativeMath
            ? WistOptions.ArithmeticModeEnum.Native
            : WistOptions.ArithmeticModeEnum.Universal);

    var frontendModules = services
        .Where(x => x.ServiceType == typeof(IFrontendCoreModule))
        .ToList();
    var modulesToRemove = new List<ServiceDescriptor>();
    var modulesToAdd = new List<IFrontendCoreModule>();

    if (options.ExcludeModules != null && options.ExcludeModules.Any())
    {
        var excludeSet = new HashSet<string>(options.ExcludeModules.Select(x => x.Trim()), StringComparer.Ordinal);
        foreach (var module in frontendModules)
        {
            var typeName = module.ImplementationType?.FullName;
            if (typeName != null && excludeSet.Contains(typeName))
                modulesToRemove.Add(module);
        }
    }

    if (options.IncludeModules != null && options.IncludeModules.Any())
        foreach (var moduleName in options.IncludeModules)
        {
            var type = Type.GetType(moduleName.Trim()) ??
                       AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(x => x.GetTypes())
                           .FirstOrDefault(x => x.FullName == moduleName.Trim());

            if (type != null && typeof(IFrontendCoreModule).IsAssignableFrom(type))
            {
                var module = Activator.CreateInstance(type) as IFrontendCoreModule;
                if (module != null)
                    modulesToAdd.Add(module);
            }
            else
            {
                Console.WriteLine($"Warning: Module '{moduleName}' not found or not a valid IFrontendCoreModule");
            }
        }

    foreach (var module in modulesToRemove)
        services.Remove(module);

    foreach (var module in modulesToAdd)
        services.AddSingleton(module);

    return services.BuildServiceProvider();
}

ICoreRunnable GetLegacyCoreRunnable(IServiceProvider provider, string mode)
{
    var runnables = provider.GetServices<ICoreRunnable>().ToList();

    if (mode.Equals("compiler", StringComparison.OrdinalIgnoreCase))
        return runnables.FirstOrDefault(r =>
                   r.GetType().IsGenericType &&
                   r.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
                   r.GetType().GetGenericArguments()[0] == typeof(DynamicMethod))
               ?? runnables.First();

    if (mode.Equals("interpreter", StringComparison.OrdinalIgnoreCase))
        return runnables.FirstOrDefault(r =>
                   r.GetType().IsGenericType &&
                   r.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
                   r.GetType().GetGenericArguments()[0] == typeof(IAbstractIR))
               ?? runnables.Last();

    Thrower.Argument(nameof(mode), $"Unknown execution mode '{mode}'. Supported modes: 'compiler', 'interpreter'.");
    return null!;
}

void ValidateDialectExecutionOptions(CommonOptions options)
{
    if (options.UseNativeMath)
        Thrower.Argument(nameof(options.UseNativeMath), "The --use-native-math option cannot be combined with --dialect-file. Configure arithmetic through the dialect definition instead.");

    if (options.IncludeModules != null && options.IncludeModules.Any())
        Thrower.Argument(nameof(options.IncludeModules), "The --include-module option cannot be combined with --dialect-file. Configure modules through the dialect definition instead.");

    if (options.ExcludeModules != null && options.ExcludeModules.Any())
        Thrower.Argument(nameof(options.ExcludeModules), "The --exclude-module option cannot be combined with --dialect-file. Configure modules through the dialect definition instead.");
}

WistDialectExecutionHost CreateDialectHost(string dialectFile)
{
    using var provider = CreateDialectWorkflowProvider();
    var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
    var result = workflow.ComposeFile(dialectFile);

    if (!result.IsSuccess)
        Thrower.InvalidOpEx(result.ToDeterministicText());

    return workflow.CreateHost(result);
}

ServiceProvider CreateDialectWorkflowProvider()
{
    var services = new ServiceCollection();
    services.AddWistDialectServices();
    return services.BuildServiceProvider();
}

void ListAllModules()
{
    Console.WriteLine("Available modules:");
    Console.WriteLine("==================");

    var assemblies = TypesFinder.Assemblies;
    foreach (var assembly in assemblies)
    {
        var modules = assembly.GetTypes()
            .Where(t => !t.IsAbstract && t.IsClass && typeof(IFrontendCoreModule).IsAssignableFrom(t))
            .ToList();

        if (!modules.Any())
            continue;

        Console.WriteLine($"\nAssembly: {assembly.GetName().Name}");
        foreach (var module in modules)
        {
            var attr = module.GetCustomAttribute<AutoRegisterServiceAttribute>();
            var lifetime = attr?.Lifetime.ToString() ?? "Transient";
            Console.WriteLine($"  {module.FullName} [{lifetime}]");
        }
    }
}