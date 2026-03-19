using ArithmeticModule.Module;
using CommentsModule;
using ConditionsModule.Enums;
using ConditionsModule.Module;
using ConditionsModule.Optimizers;
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

        RegisterModules(builder);
        RegisterBackends(builder);
        RegisterOptimizers(builder);
        RegisterIntrinsics(builder);
    }

    private static void RegisterModules(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        builder
            .RegisterModule(new RuntimeModuleDescriptor(typeof(ArithmeticModuleImpl), ["Arithmetic"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(BooleanOperations), ["BooleanConditions"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(CommentsModuleImpl), ["Comments"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(ComparisonOperations), ["ComparisonConditions"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(ConditionsModuleImpl), ["Conditions"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(CSharpInteropModuleImpl), ["CSharpInterop"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(EqualityModuleImpl), ["Equality"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(IdentifierModuleImpl), ["Identifier"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(InternalPreprocessorLexemesModuleImpl), ["InternalPreprocessorLexemes"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(LabelsModuleImpl), ["Labels"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(LoopsModuleImpl), ["Loops"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(NativeTypesModuleImpl), ["NativeTypes"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(NumbersModuleImpl), ["Numbers"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(ParametersSetterModuleImpl), ["ParametersSetter"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(ScopesModuleImpl), ["Scopes"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(SemicolonAsNewLineModuleImpl), ["SemicolonAsNewLine"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(VariablesModuleImpl), ["Variables"]))
            .RegisterModule(new RuntimeModuleDescriptor(typeof(WhitespaceModuleImpl), ["Whitespaces"]));
    }

    private static void RegisterBackends(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        builder
            .RegisterBackend(new RuntimeBackendDescriptor(WistDialectBackendIds.Cil, ["compiler"]))
            .RegisterBackend(new RuntimeBackendDescriptor(WistDialectBackendIds.Interpreter));
    }

    private static void RegisterOptimizers(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        builder
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(ArithmeticOptimizerModule), ["ArithmeticOptimization"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(BooleanOptimizerModule), ["BooleanOptimization"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(ComparisonIntrinsicOptimizerModule), ["ComparisonIntrinsicOptimization"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(EGraphOptimizerModule), ["EGraphOptimization"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(LocalVariablesOptimizer), ["LocalVariablesOptimization"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(NativeCilOptimizerModule), ["NativeCilOptimization"]))
            .RegisterOptimizer(new RuntimeOptimizerDescriptor(typeof(NativeTypesOptimizerModule), ["NativeTypesOptimization"]));
    }

    private static void RegisterIntrinsics(DialectRuntimeDescriptorRegistryBuilder builder)
    {
        foreach (var intrinsic in WistDialectIntrinsicRegistry.CreateDescriptors())
            builder.RegisterIntrinsic(intrinsic);
    }
}
