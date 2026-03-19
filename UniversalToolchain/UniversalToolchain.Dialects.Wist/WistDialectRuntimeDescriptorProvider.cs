using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using ArithmeticModule.Module;
using CommentsModule;
using ConditionsModule.Enums;
using ConditionsModule.Module;
using ExceptionsManager;
using ConditionsModule.Optimizers;
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
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Registers the real Wist runtime descriptor catalog used by dialect resolution.
/// </summary>
public sealed class WistDialectRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
{
    public int Order => 0;

    public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        if (builder == null)
        {
            Thrower.ArgumentNull(nameof(builder));
        }

        RegisterModules(builder);
        RegisterBackends(builder);
        RegisterOptimizers(builder);
        RegisterIntrinsics(builder);
    }

    private static void RegisterModules(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        builder
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Arithmetic, typeof(ArithmeticModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.BooleanConditions, typeof(BooleanOperations)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Comments, typeof(CommentsModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.ComparisonConditions, typeof(ComparisonOperations)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Conditions, typeof(ConditionsModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.CSharpInterop, typeof(CSharpInteropModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Equality, typeof(EqualityModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Identifier, typeof(IdentifierModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.InternalPreprocessorLexemes, typeof(InternalPreprocessorLexemesModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Labels, typeof(LabelsModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Loops, typeof(LoopsModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.NativeTypes, typeof(NativeTypesModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Numbers, typeof(NumbersModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.ParametersSetter, typeof(ParametersSetterModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Scopes, typeof(ScopesModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.SemicolonAsNewLine, typeof(SemicolonAsNewLineModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Variables, typeof(VariablesModuleImpl)))
            .RegisterModule(new RuntimeModuleDescriptor(WistDialectCatalogNames.Modules.Whitespaces, typeof(WhitespaceModuleImpl)));
    }

    private static void RegisterBackends(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        builder
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Cil, "compiler"))
            .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "interpreter"));
    }

    private static void RegisterOptimizers(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        builder
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.Arithmetic, typeof(ArithmeticOptimizerModule)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.Boolean, typeof(BooleanOptimizerModule)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.ComparisonIntrinsic, typeof(ComparisonIntrinsicOptimizerModule)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.EGraph, typeof(EGraphOptimizerModule)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.LocalVariables, typeof(LocalVariablesOptimizer)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.NativeCil, typeof(NativeCilOptimizerModule)))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(WistDialectCatalogNames.Optimizers.NativeTypes, typeof(NativeTypesOptimizerModule)));
    }

    private static void RegisterIntrinsics(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        foreach (var intrinsic in WistDialectRuntimeIntrinsics.All)
        {
            builder.RegisterIntrinsic(intrinsic);
        }
    }
}
