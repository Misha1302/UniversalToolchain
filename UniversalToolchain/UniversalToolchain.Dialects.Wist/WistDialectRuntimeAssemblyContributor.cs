using System.Reflection;
using ArithmeticModule.Module;
using CommentsModule;
using ConditionsModule.Enums;
using CSharpInteropModule.Module;
using EqualityModule;
using ExceptionsManager;
using IdentifierModule;
using InternalPreprocessorLexemesModule;
using LabelsModule.Module;
using LocalVariablesOptimizerModule;
using LoopsModule.Module;
using NativeMathModule;
using NumbersModule.Module;
using ParametersSetterModule;
using ScopesModule.Module;
using SemicolonAsNewLineModule;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Dialects.Wist;

public interface IWistDialectRuntimeAssemblyContributor
{
    int Order { get; }

    IReadOnlyList<Assembly> GetAssemblies();
}

internal static class WistDialectRuntimeAssemblyCatalog
{
    public static IReadOnlyList<Assembly> Build(IEnumerable<IWistDialectRuntimeAssemblyContributor> contributors)
    {
        if (contributors == null)
            Thrower.ArgumentNull(nameof(contributors));

        var assemblies = new List<Assembly>();
        var seenAssemblyNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var contributor in contributors
                     .Select(static contributor => contributor ?? ThrowContributorCollectionContainsNull())
                     .OrderBy(static contributor => contributor.Order)
                     .ThenBy(static contributor => contributor.GetType().FullName, StringComparer.Ordinal))
        {
            var contributedAssemblies = contributor.GetAssemblies() ?? ThrowContributorReturnedNullAssemblies(contributor);
            foreach (var assembly in contributedAssemblies
                         .Select(static assembly => assembly ?? ThrowContributorReturnedNullAssembly())
                         .Select(static assembly => CreateAssemblyEntry(assembly))
                         .OrderBy(static assembly => assembly.FullName, StringComparer.Ordinal))
            {
                if (seenAssemblyNames.Add(assembly.FullName))
                    assemblies.Add(assembly.Assembly);
            }
        }

        return assemblies;
    }

    private static (Assembly Assembly, string FullName) CreateAssemblyEntry(Assembly assembly)
    {
        if (string.IsNullOrWhiteSpace(assembly.FullName))
            return ThrowContributorReturnedAssemblyWithoutFullName();

        return (assembly, assembly.FullName);
    }

    private static IWistDialectRuntimeAssemblyContributor ThrowContributorCollectionContainsNull()
    {
        Thrower.Argument("contributors", "Contributor collection must not contain null entries.");
        return null!;
    }

    private static IReadOnlyList<Assembly> ThrowContributorReturnedNullAssemblies(IWistDialectRuntimeAssemblyContributor contributor)
    {
        return Thrower.InvalidOpEx<IReadOnlyList<Assembly>>($"Contributor '{contributor.GetType().FullName}' returned null assemblies.");
    }

    private static Assembly ThrowContributorReturnedNullAssembly()
    {
        return Thrower.InvalidOpEx<Assembly>("Contributor returned a null assembly.");
    }

    private static (Assembly Assembly, string FullName) ThrowContributorReturnedAssemblyWithoutFullName()
    {
        return Thrower.InvalidOpEx<(Assembly Assembly, string FullName)>("Contributor returned an assembly without a full name.");
    }
}

internal sealed class WistExpressionRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
{
    public int Order => 100;

    public IReadOnlyList<Assembly> GetAssemblies()
    {
        return
        [
            typeof(ArithmeticModuleImpl).Assembly,
            typeof(BooleanOperations).Assembly,
            typeof(EqualityModuleImpl).Assembly,
            typeof(NativeTypesModuleImpl).Assembly,
            typeof(NumbersModuleImpl).Assembly
        ];
    }
}

internal sealed class WistSyntaxRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
{
    public int Order => 200;

    public IReadOnlyList<Assembly> GetAssemblies()
    {
        return
        [
            typeof(CommentsModuleImpl).Assembly,
            typeof(IdentifierModuleImpl).Assembly,
            typeof(InternalPreprocessorLexemesModuleImpl).Assembly,
            typeof(SemicolonAsNewLineModuleImpl).Assembly,
            typeof(WhitespaceModuleImpl).Assembly
        ];
    }
}

internal sealed class WistStateRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
{
    public int Order => 300;

    public IReadOnlyList<Assembly> GetAssemblies()
    {
        return
        [
            typeof(ParametersSetterModuleImpl).Assembly,
            typeof(ScopesModuleImpl).Assembly,
            typeof(VariablesModuleImpl).Assembly
        ];
    }
}

internal sealed class WistControlFlowRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
{
    public int Order => 400;

    public IReadOnlyList<Assembly> GetAssemblies()
    {
        return
        [
            typeof(LabelsModuleImpl).Assembly,
            typeof(LoopsModuleImpl).Assembly
        ];
    }
}

internal sealed class WistInteropRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
{
    public int Order => 500;

    public IReadOnlyList<Assembly> GetAssemblies()
    {
        return
        [
            typeof(CSharpInteropModuleImpl).Assembly
        ];
    }
}

internal sealed class WistOptimizerRuntimeAssemblyContributor : IWistDialectRuntimeAssemblyContributor
{
    public int Order => 600;

    public IReadOnlyList<Assembly> GetAssemblies()
    {
        return
        [
            typeof(LocalVariablesOptimizer).Assembly
        ];
    }
}
