// BasicCore.Tests/TestBase.cs

using AbstractIrConverters;
using BasicStdLib;
using DependencyInjection;
using IntermediateRepresentationAbstractions;
using LocalVariablesOptimizerModule;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

[TestFixture]
public abstract class TestBase
{
    protected const int CoresCount = 2;
    private IServiceProvider? _serviceProvider;
    private bool _useDependencyInjection = true; // По умолчанию используем DI

    protected TestBase()
    {
        Main.LoadStdLibToThisAssembly();
    }

    /// <summary>
    ///     Override this method to configure dependency injection services for the test
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    virtual protected void ConfigureTestServices(IServiceCollection services)
    {
        // Default implementation does nothing
        // Derived classes can override to add custom services
    }

    /// <summary>
    ///     Enables or disables dependency injection for this test instance
    /// </summary>
    /// <param name="enable">True to enable DI, false to use legacy mode</param>
    protected void EnableDependencyInjection(bool enable)
    {
        _useDependencyInjection = enable;
    }

    /// <summary>
    ///     Builds service provider with test configuration
    /// </summary>
    internal protected IServiceProvider BuildTestServiceProvider()
    {
        var services = new ServiceCollection();

        // Add default Wist test services
        services.AddWistTestServices();
        services.AddCoreRunnables();

        // Allow test-specific configuration
        ConfigureTestServices(services);

        _useDependencyInjection = true;
        return _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    ///     Executes code using dependency injection (primary method)
    /// </summary>
    protected object ExecuteCodeWithDI(string code)
    {
        if (_serviceProvider == null)
        {
            BuildTestServiceProvider();
        }

        var cores = _serviceProvider!.GetServices<ICoreRunnable>().ToList();
        var values = cores.Select(core => core.Run(code)).ToList();

        Thrower.AssertAlways(values.All(value => value?.Equals(values[0]) ?? value == values[0]));
        return values[0]!;
    }

    /// <summary>
    ///     Main method to execute code. Uses DI by default, falls back to legacy mode if disabled
    /// </summary>
    internal protected object ExecuteCode(
        string code,
        Dictionary<Type, object>? middleEndModules = null)
    {
        // Log warning if middleEndModules are provided (not supported in DI mode)
        if (middleEndModules != null && middleEndModules.Count > 0)
        {
            Debug.WriteLine("Warning: middleEndModules parameter is not supported in DI mode and will be ignored.");
        }

        // Use DI by default
        if (_useDependencyInjection)
        {
            return ExecuteCodeWithDI(code);
        }

        // Fall back to legacy implementation if DI is disabled
        return ExecuteCodeLegacy(code, middleEndModules);
    }

    /// <summary>
    ///     Legacy implementation (preserved for backward compatibility)
    /// </summary>
    private object ExecuteCodeLegacy(
        string code,
        Dictionary<Type, object>? middleEndModules = null)
    {
        middleEndModules ??= [];

        var values = CreateCoresLegacy(middleEndModules)
            .Select(core => core.Run(code))
            .ToList();

        Thrower.AssertAlways(values.All(value => value?.Equals(values[0]) ?? value == values[0]));

        return values[0]!;
    }

    /// <summary>
    ///     Legacy core creation (preserved for backward compatibility)
    /// </summary>
    private static IEnumerable<ICoreRunnable> CreateCoresLegacy(
        Dictionary<Type, object> middleEndModules)
    {
        var modules = CreateDefaultModulesLegacy();
        var optimizer = new LocalVariablesOptimizer();

        return
        [
            new BasicCoreImpl<DynamicMethod>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicAstToBytecodeTranslatorImpl(),
                () => new BytecodeToAbstractIrConverterImpl(),
                () => new AbstractMethodsCompilerImpl(),
                () => new DynamicMethodExecutor(),
                modules.Concat([optimizer]).ToList(),
                middleEndModules.TryGetValue(typeof(DynamicMethod), out var dmModules)
                    ? (List<IMiddleEndCoreModule<DynamicMethod>>)dmModules
                    : []
            ),
            new BasicCoreImpl<IAbstractIR>(
                () => new BasicLexerImpl(),
                () => new BasicParserImpl(),
                () => new BasicAstToBytecodeTranslatorImpl(),
                () => new BytecodeToAbstractIrConverterImpl(),
                () => new AbstractIrToAbstractIrStub(),
                () => new InterpreterImpl(),
                modules,
                middleEndModules.TryGetValue(typeof(IAbstractIR), out var airModules)
                    ? (List<IMiddleEndCoreModule<IAbstractIR>>)airModules
                    : []
            )
        ];
    }

    /// <summary>
    ///     Legacy module creation (preserved for backward compatibility)
    /// </summary>
    private static List<IFrontendCoreModule> CreateDefaultModulesLegacy()
    {
        return
        [
            new IdentifierModuleImpl(),
            new ScopesModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl(),
            new SemicolonAsNewLineModuleImpl(),
            new ArithmeticModuleImpl(),
            new CSharpInteropModuleImpl(),
            new LabelsModuleImpl(),
            new VariablesModuleImpl(),
            new EqualityModuleImpl(),
            new ConditionsModuleImpl(),
            new ComparisonOperations(),
            new BooleanOperations()
        ];
    }

    /// <summary>
    ///     Creates a core instance using dependency injection
    /// </summary>
    protected ICoreRunnable CreateCoreWithDI<TCompilationOutput>()
        where TCompilationOutput : class
    {
        if (_serviceProvider == null)
        {
            BuildTestServiceProvider();
        }

        return _serviceProvider!.GetServices<ICoreRunnable>()
            .FirstOrDefault(core =>
                core.GetType().GetGenericArguments()[0] == typeof(TCompilationOutput))
            .NotNull("Core with specified compilation output not found");
    }
}