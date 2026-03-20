using System.Reflection;
using ArithmeticModule.Module;
using CommentsModule;
using ConditionsModule.Enums;
using CSharpInteropModule.Module;
using EqualityModule;
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
using ExceptionsManager;
using UniversalToolchain.Dialects.Integration;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
///     Registers the real Wist runtime descriptor catalog used by dialect resolution.
/// </summary>
public sealed class WistDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 0;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        var runtimeAssemblies = GetRuntimeAssemblies();
        builder
            .RegisterAttributedModulesFromAssemblies(runtimeAssemblies)
            .RegisterAttributedOptimizersFromAssemblies(runtimeAssemblies)
            .RegisterAttributedBackendsFromAssemblies(typeof(WistDialectRuntimeDescriptorProvider).Assembly);
        RegisterIntrinsics(builder);
    }

    private static void RegisterIntrinsics(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        foreach (var intrinsic in WistDialectIntrinsicRegistry.CreateDescriptors())
            builder.RegisterIntrinsic(intrinsic);
    }

    private static Assembly[] GetRuntimeAssemblies()
    {
        return
        [
            typeof(ArithmeticModuleImpl).Assembly,
            typeof(BooleanOperations).Assembly,
            typeof(CommentsModuleImpl).Assembly,
            typeof(CSharpInteropModuleImpl).Assembly,
            typeof(EqualityModuleImpl).Assembly,
            typeof(IdentifierModuleImpl).Assembly,
            typeof(InternalPreprocessorLexemesModuleImpl).Assembly,
            typeof(LabelsModuleImpl).Assembly,
            typeof(LocalVariablesOptimizer).Assembly,
            typeof(LoopsModuleImpl).Assembly,
            typeof(NativeTypesModuleImpl).Assembly,
            typeof(NumbersModuleImpl).Assembly,
            typeof(ParametersSetterModuleImpl).Assembly,
            typeof(ScopesModuleImpl).Assembly,
            typeof(SemicolonAsNewLineModuleImpl).Assembly,
            typeof(VariablesModuleImpl).Assembly,
            typeof(WhitespaceModuleImpl).Assembly
        ];
    }
}
