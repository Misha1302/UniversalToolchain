using System.Diagnostics;
using System.Reflection.Emit;
using AssemblyFinder;
using DependencyInjection;
using ExceptionsManager;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using NativeMathModule;
using Wistc;

return Parser.Default.ParseArguments<RunOptions, ReplOptions>(args)
    .MapResult(
        (RunOptions opts) => RunCommand(opts),
        (ReplOptions opts) => ReplCommand(opts),
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