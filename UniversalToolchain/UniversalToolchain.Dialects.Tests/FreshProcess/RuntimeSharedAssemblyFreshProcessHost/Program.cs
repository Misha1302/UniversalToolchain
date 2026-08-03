using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using HostOnlyContractFixture;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Wist;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.FreshProcessHost;

internal static class Program
{
    private const string RuntimeFixtureAssemblyName = "RuntimeHostileFixture";
    private const string UnregisteredDependencyFixtureAssemblyName = "RuntimeUnregisteredDependencyFixture";

    public static int Main(string[] args)
    {
        try
        {
            return args.Length > 0
                ? args[0] switch
                {
                    "hostile" => RunHostilePreload(args),
                    "unregistered-default-fallback" => RunUnregisteredDefaultFallback(args),
                    _ => RunWistScenario(args)
                }
                : RunWistScenario(args);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int RunWistScenario(IReadOnlyList<string> args)
    {
        var scenario = args.Count == 0 ? "none" : args[0];
        ApplyPreloadScenario(scenario);
        return ExecuteWistBoundaryChecks();
    }

    private static void ApplyPreloadScenario(string scenario)
    {
        switch (scenario)
        {
            case "contract-first":
                LoadDefaultFromOutput("UniversalToolchain.Functions.Abstractions.dll");
                break;
            case "unrelated-first":
                LoadDefaultFromOutput("ExceptionsManager.dll");
                LoadDefaultFromOutput("UniversalToolchain.Functions.Abstractions.dll");
                break;
            case "none":
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown preload scenario.");
        }
    }

    private static void LoadDefaultFromOutput(string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, fileName));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Preload assembly was not copied to the fresh-process output: '{path}'.", path);

        var identity = AssemblyName.GetAssemblyName(path);
        var existing = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly =>
            AssemblyIdentityEquals(assembly.GetName(), identity));
        if (existing != null)
            return;

        _ = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ExecuteWistBoundaryChecks()
    {
        using var provider = CreateProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var safeMathPath = ResolveDialectPath("function-calls-safe-math");
        var composition = workflow.ComposeFile(safeMathPath);
        if (!composition.IsSuccess || composition.RuntimeSelection is not SelectedRuntimePlan plan)
            throw new InvalidOperationException("SafeMath dialect did not produce a successful selected runtime plan.");

        var safeMathEntry = plan.OrderedModules.SingleOrDefault(static entry =>
            string.Equals(entry.AssemblySimpleName, "SafeMathFunctionsModule", StringComparison.Ordinal));
        if (safeMathEntry == null)
            throw new InvalidOperationException("Selected runtime plan does not contain SafeMathFunctionsModule.");

        var assemblyTypeLoader = provider.GetRequiredService<IRuntimeAssemblyTypeLoader>();
        var hostContract = typeof(IBuiltinFunctionDescriptorProvider);
        var loadedContractAssembly = assemblyTypeLoader.LoadAssembly(hostContract.Assembly.GetName().Name!);
        if (!ReferenceEquals(hostContract.Assembly, loadedContractAssembly))
        {
            throw new InvalidOperationException(
                "Explicit shared contract root did not resolve to host identity. " +
                $"host={DescribeAssembly(hostContract.Assembly)}; loaded={DescribeAssembly(loadedContractAssembly)}.");
        }

        var componentType = provider.GetRequiredService<IRuntimeComponentTypeLoader>().LoadType(safeMathEntry);
        var providerAttribute = componentType
            .GetCustomAttributes(typeof(DialectCapabilityProviderAttribute), inherit: false)
            .Cast<DialectCapabilityProviderAttribute>()
            .Single();
        var providerType = providerAttribute.ProviderType;
        var implementationContract = providerType.GetInterfaces().SingleOrDefault(candidate =>
            string.Equals(candidate.FullName, hostContract.FullName, StringComparison.Ordinal));
        if (!ReferenceEquals(hostContract, implementationContract) || !hostContract.IsAssignableFrom(providerType))
        {
            throw new InvalidOperationException(
                "SafeMath provider contract has split CLR identity across load contexts. " +
                $"host={DescribeType(hostContract)}; implementation={DescribeType(implementationContract)}; provider={DescribeType(providerType)}.");
        }

        using var host = workflow.CreateHost(composition);
        const string source = """
                              let base = 100.0 * 3.0
                              let discountValue = clamp(base * 0.15, 0.0, 50.0)
                              let result = base - discountValue
                              if result < 0.0 then 0.0 else result
                              """;

        var interpreter = host.Run(source, "interpreter");
        var cil = host.Run(source, "cil");
        var interpreterValue = Normalize(interpreter);
        var cilValue = Normalize(cil);
        if (!string.Equals(interpreterValue, "255", StringComparison.Ordinal) ||
            !string.Equals(cilValue, "255", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unexpected SafeMath results: interpreter={interpreterValue}, cil={cilValue}.");
        }

        var interpreterCategory = NormalizeCategory(interpreter);
        var cilCategory = NormalizeCategory(cil);
        if (!string.Equals(interpreterCategory, cilCategory, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Backend CLR value categories differ: interpreter={interpreterCategory}, cil={cilCategory}.");
        }

        var negativeComposition = workflow.ComposeFile(ResolveDialectPath("minimal-arithmetic"));
        if (!negativeComposition.IsSuccess)
            throw new InvalidOperationException("Negative dialect did not compose successfully.");

        using var negativeHost = workflow.CreateHost(negativeComposition);
        var negativeRejected = false;
        try
        {
            _ = negativeHost.Run("clamp(300.0, 0.0, 255.0)", "interpreter");
        }
        catch
        {
            negativeRejected = true;
        }

        if (!negativeRejected)
            throw new InvalidOperationException("Shared contract registration incorrectly activated clamp in minimal-arithmetic.");

        var planSignature = string.Join(",", plan.OrderedModules.Select(static entry => entry.ComponentId.Value)) +
                            "|" +
                            string.Join(",", plan.EnabledBackends.Select(static entry => entry.ComponentId.Value));

        Console.WriteLine($"SELECTED_PLAN={planSignature}");
        Console.WriteLine("DIALECT_INSPECT=PASS");
        Console.WriteLine("TYPE_IDENTITY=PASS");
        Console.WriteLine($"INTERPRETER_RESULT={interpreterValue}");
        Console.WriteLine($"CIL_RESULT={cilValue}");
        Console.WriteLine($"CLR_VALUE_CATEGORY={interpreterCategory}");
        Console.WriteLine("BACKEND_PARITY=PASS");
        Console.WriteLine("NEGATIVE_SURFACE=PASS");
        return 0;
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        return services.BuildServiceProvider();
    }

    private static string ResolveDialectPath(string presetId) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "Dialects",
            "examples",
            "wist",
            presetId,
            "dialect.wistdialect"));

    private static string Normalize(object? value)
    {
        if (value is not null && value.GetType().GetMethod("GetValue", Type.EmptyTypes) is MethodInfo getValue)
        {
            var primitive = getValue.Invoke(value, null);
            return Convert.ToDouble(primitive, CultureInfo.InvariantCulture).ToString("G17", CultureInfo.InvariantCulture);
        }

        return value switch
        {
            double doubleValue => doubleValue.ToString("G17", CultureInfo.InvariantCulture),
            null => "<null>",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? "<unknown>"
        };
    }

    private static string NormalizeCategory(object? value)
    {
        if (value is null)
            return "null";

        var getValue = value.GetType().GetMethod("GetValue", Type.EmptyTypes);
        return getValue?.ReturnType.FullName ?? value.GetType().FullName ?? value.GetType().Name;
    }

    private static int RunHostilePreload(IReadOnlyList<string> args)
    {
        if (args.Count != 3)
            throw new ArgumentException("Hostile mode requires canonical and hostile assembly paths.");

        var canonicalPath = Path.GetFullPath(args[1]);
        var hostilePath = Path.GetFullPath(args[2]);
        var hostile = AssemblyLoadContext.Default.LoadFromAssemblyPath(hostilePath);
        if (hostile.GetName().Version != new Version(2, 0, 0, 0))
            throw new InvalidOperationException("Hostile fixture version is not 2.0.0.0.");

        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new StaticLocator(RuntimeFixtureAssemblyName, canonicalPath),
            new DefaultRuntimeSharedAssemblyResolver([]));
        var loaded = strategy.LoadAssembly(RuntimeFixtureAssemblyName);
        var context = AssemblyLoadContext.GetLoadContext(loaded);

        if (ReferenceEquals(loaded, hostile) ||
            loaded.GetName().Version != new Version(1, 0, 0, 0) ||
            !string.Equals(context?.Name, "UniversalToolchain.Runtime.Isolated", StringComparison.Ordinal) ||
            !string.Equals(Path.GetFullPath(loaded.Location), canonicalPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Hostile default-context preload became authority. loaded={loaded.FullName}, context={context?.Name}, path={loaded.Location}.");
        }

        Console.WriteLine("HOSTILE_PRELOAD=PASS");
        return 0;
    }

    private static int RunUnregisteredDefaultFallback(IReadOnlyList<string> args)
    {
        if (args.Count != 2)
            throw new ArgumentException("Unregistered-default-fallback mode requires the runtime fixture path.");

        _ = typeof(IHostOnlyContract).Assembly;
        var runtimePath = Path.GetFullPath(args[1]);
        using var strategy = new DefaultRuntimeAssemblyLoadStrategy(
            new StaticLocator(UnregisteredDependencyFixtureAssemblyName, runtimePath),
            new DefaultRuntimeSharedAssemblyResolver([]));

        try
        {
            var loaded = strategy.LoadAssembly(UnregisteredDependencyFixtureAssemblyName);
            var componentType = loaded.GetType(
                "UnregisteredDependencyRuntimeFixture.RuntimeComponent",
                throwOnError: true,
                ignoreCase: false)!;
            _ = componentType.GetInterfaces();
        }
        catch (Exception exception) when (ContainsForbiddenDefaultFallbackDiagnostic(exception))
        {
            Console.WriteLine("UNREGISTERED_DEFAULT_FALLBACK=PASS");
            return 0;
        }

        throw new InvalidOperationException(
            "An unregistered host-only dependency was silently resolved from the default context.");
    }

    private static bool ContainsForbiddenDefaultFallbackDiagnostic(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains(
                    "Fallback to an assembly from the default context is forbidden",
                    StringComparison.Ordinal))
            {
                return true;
            }

            if (current is ReflectionTypeLoadException reflectionTypeLoadException &&
                reflectionTypeLoadException.LoaderExceptions.Any(static loaderException =>
                    loaderException != null &&
                    loaderException.Message.Contains(
                        "Fallback to an assembly from the default context is forbidden",
                        StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    private static string DescribeType(Type? type) =>
        type == null
            ? "<null>"
            : $"{type.AssemblyQualifiedName} [{DescribeAssembly(type.Assembly)}]";

    private static string DescribeAssembly(Assembly assembly)
    {
        var context = AssemblyLoadContext.GetLoadContext(assembly);
        return $"{assembly.FullName}; context={context?.Name ?? "<null>"}; location={assembly.Location}";
    }

    private static bool AssemblyIdentityEquals(AssemblyName left, AssemblyName right) =>
        string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
        left.Version == right.Version &&
        string.Equals(NormalizeCulture(left.CultureName), NormalizeCulture(right.CultureName), StringComparison.Ordinal) &&
        left.GetPublicKeyToken().AsSpan().SequenceEqual(right.GetPublicKeyToken());

    private static string NormalizeCulture(string? culture) =>
        string.IsNullOrWhiteSpace(culture) ? string.Empty : culture.ToLowerInvariant();

    private sealed class StaticLocator(string simpleName, string path) : IRuntimeAssemblyLocator
    {
        public bool TryResolveAssemblyPath(string assemblySimpleName, out string assemblyPath)
        {
            if (string.Equals(simpleName, assemblySimpleName, StringComparison.Ordinal))
            {
                assemblyPath = path;
                return true;
            }

            assemblyPath = string.Empty;
            return false;
        }
    }
}
