using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Parsing;
using ArithmeticModule.Module;
using ScopesModule.Module;
using ConditionsModule.Module;
using LabelsModule.Module;
using LoopsModule.Module;
using ATarget = UniversalToolchain.Dialects.Abstractions.DialectBackendTarget;


return Parser.Default.ParseArguments<RunOptions, ReplOptions, DialectInspectOptions, DialectDemoOptions>(args)
    .MapResult(
        (RunOptions opts) => RunCommand(opts),
        (ReplOptions opts) => ReplCommand(opts),
        (DialectInspectOptions opts) => DialectInspectCommand(opts),
        (DialectDemoOptions opts) => DialectDemoCommand(opts),
        _ => 1
    );

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

        var provider = BuildServiceProvider(options);
        var core = GetCoreRunnable(provider, options.Mode);

        var result = core.Run(code);
        if (result != null)
            Console.WriteLine(result);

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
        var provider = BuildServiceProvider(options);
        var core = GetCoreRunnable(provider, options.Mode);

        Console.WriteLine("Wist REPL (Ctrl+C to exit)");
        Console.WriteLine($"Mode: {options.Mode}");

        var repl = new Repl(core, options.HistoryFile);
        return repl.Run();
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
        var demoWorkflow = new DialectFrameworkDemoWorkflow(
            new DialectFrameworkCompositionWorkflow(
                new UniversalToolchain.Dialects.Frontend.DialectDslCompiler(),
                new DialectCompiledDialectBuildPlanBuilder(),
                new DialectRuntimeCompositionResolver()));

        var registry = CreateDefaultDialectRegistry();
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
        var parser = new DialectDefinitionParser();
        var buildPlanBuilder = new DialectBuildPlanBuilder();
        var resolver = new DialectRuntimeCompositionResolver();
        var workflow = new DialectInspectWorkflow(parser, buildPlanBuilder, resolver);
        var registry = CreateDefaultDialectRegistry();

        var result = workflow.InspectFile(options.File, registry);
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

IServiceProvider BuildServiceProvider(CommonOptions options)
{
    var services = new ServiceCollection();

    // Register core services
    services.AddWistServices(wistOptions =>
        wistOptions.ArithmeticMode = options.UseNativeMath
            ? WistOptions.ArithmeticModeEnum.Native
            : WistOptions.ArithmeticModeEnum.Universal
    );


    // Filter modules based on options
    var frontendModules = services
        .Where(s => s.ServiceType == typeof(IFrontendCoreModule))
        .ToList();

    var modulesToRemove = new List<ServiceDescriptor>();
    var modulesToAdd = new List<IFrontendCoreModule>();

    // Process exclusions
    if (options.ExcludeModules != null && options.ExcludeModules.Any())
    {
        var excludeSet = new HashSet<string>(options.ExcludeModules.Select(m => m.Trim()));
        foreach (var module in frontendModules)
        {
            var typeName = module.ImplementationType?.FullName;
            if (typeName != null && excludeSet.Contains(typeName))
                modulesToRemove.Add(module);
        }
    }

    // Process inclusions
    if (options.IncludeModules != null && options.IncludeModules.Any())
        foreach (var moduleName in options.IncludeModules)
        {
            var type = Type.GetType(moduleName.Trim()) ??
                       AppDomain.CurrentDomain.GetAssemblies()
                           .SelectMany(a => a.GetTypes())
                           .FirstOrDefault(t => t.FullName == moduleName.Trim());

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

    // Apply changes
    foreach (var module in modulesToRemove)
        services.Remove(module);

    foreach (var module in modulesToAdd)
        services.AddSingleton(module);

    return services.BuildServiceProvider();
}

ICoreRunnable GetCoreRunnable(IServiceProvider provider, string mode)
{
    var runnables = provider.GetServices<ICoreRunnable>().ToList();

    if (mode.Equals("compiler", StringComparison.OrdinalIgnoreCase))
    {
        // Find compiler-based implementation (DynamicMethod)
        var compiler = runnables.FirstOrDefault(r =>
            r.GetType().IsGenericType &&
            r.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
            r.GetType().GetGenericArguments()[0] == typeof(DynamicMethod));

        return compiler ?? runnables.First();
    }
    if (mode.Equals("interpreter", StringComparison.OrdinalIgnoreCase))
    {
        // Find interpreter-based implementation (IAbstractIR)
        var interpreter = runnables.FirstOrDefault(r =>
            r.GetType().IsGenericType &&
            r.GetType().GetGenericTypeDefinition() == typeof(BasicCoreImpl<>) &&
            r.GetType().GetGenericArguments()[0] == typeof(IAbstractIR));

        return interpreter ?? runnables.Last();
    }
    Thrower.Argument(nameof(mode), $"Unknown execution mode '{mode}'. Supported modes: 'compiler', 'interpreter'.");
    return null;
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

        if (modules.Any())
        {
            Console.WriteLine($"\nAssembly: {assembly.GetName().Name}");
            foreach (var module in modules)
            {
                var attr = module.GetCustomAttribute<AutoRegisterServiceAttribute>();
                var lifetime = attr?.Lifetime.ToString() ?? "Transient";
                Console.WriteLine($"  {module.FullName} [{lifetime}]");
            }
        }
    }
}

DialectRuntimeDescriptorRegistry CreateDefaultDialectRegistry()
{
    return new DialectRuntimeDescriptorRegistryBuilder()
        .RegisterModule(new RuntimeModuleDescriptor("Arithmetic", typeof(ArithmeticModuleImpl)))
        .RegisterModule(new RuntimeModuleDescriptor("Variables", typeof(VariablesModuleImpl)))
        .RegisterModule(new RuntimeModuleDescriptor("Scopes", typeof(ScopesModuleImpl)))
        .RegisterModule(new RuntimeModuleDescriptor("Conditions", typeof(ConditionsModuleImpl)))
        .RegisterModule(new RuntimeModuleDescriptor("Labels", typeof(LabelsModuleImpl)))
        .RegisterModule(new RuntimeModuleDescriptor("Loops", typeof(LoopsModuleImpl)))
        .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Interpreter, "InterpreterBackend"))
        .RegisterBackend(new RuntimeBackendDescriptor(ATarget.Cil, "CilBackend"))
        .RegisterOptimizer(new RuntimeOptimizerDescriptor("LocalVariablesOptimization", typeof(LocalVariablesOptimizerModule.LocalVariablesOptimizer)))
        .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", ATarget.Any))
        .Build();
}
