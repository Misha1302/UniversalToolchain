using ArithmeticModule.Module;
using CommentsModule;
using ConditionsModule.Enums;
using ConditionsModule.Module;
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
using UniversalToolchain.Dialects.Abstractions;
using VariablesModule;
using WhitespacesModule;

namespace UniversalToolchain.Dialects.Wist;

public static class WistRuntimeCatalogFactory
{
    public static IDialectRuntimeCatalog Create()
    {
        return new DialectRuntimeCatalogBuilder()
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Arithmetic", ["Arithmetic"], typeof(ArithmeticModuleImpl), typeof(ArithmeticModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Conditions", ["Conditions"], typeof(ConditionsModuleImpl), typeof(ConditionsModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("BooleanConditions", ["BooleanConditions"], typeof(BooleanOperations), typeof(BooleanOperations).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("ComparisonConditions", ["ComparisonConditions"], typeof(ComparisonOperations), typeof(ComparisonOperations).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Equality", ["Equality"], typeof(EqualityModuleImpl), typeof(EqualityModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("NativeTypes", ["NativeTypes"], typeof(NativeTypesModuleImpl), typeof(NativeTypesModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Numbers", ["Numbers"], typeof(NumbersModuleImpl), typeof(NumbersModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Comments", ["Comments"], typeof(CommentsModuleImpl), typeof(CommentsModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Identifier", ["Identifier"], typeof(IdentifierModuleImpl), typeof(IdentifierModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("InternalPreprocessorLexemes", ["InternalPreprocessorLexemes"], typeof(InternalPreprocessorLexemesModuleImpl), typeof(InternalPreprocessorLexemesModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("SemicolonAsNewLine", ["SemicolonAsNewLine"], typeof(SemicolonAsNewLineModuleImpl), typeof(SemicolonAsNewLineModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Whitespaces", ["Whitespaces"], typeof(WhitespaceModuleImpl), typeof(WhitespaceModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("ParametersSetter", ["ParametersSetter"], typeof(ParametersSetterModuleImpl), typeof(ParametersSetterModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Scopes", ["Scopes"], typeof(ScopesModuleImpl), typeof(ScopesModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Variables", ["Variables"], typeof(VariablesModuleImpl), typeof(VariablesModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Labels", ["Labels"], typeof(LabelsModuleImpl), typeof(LabelsModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("Loops", ["Loops"], typeof(LoopsModuleImpl), typeof(LoopsModuleImpl).Assembly.GetName().Name))
            .RegisterModule(DialectRuntimeModuleDescriptor.Create("CSharpInterop", ["CSharpInterop"], typeof(CSharpInteropModuleImpl), typeof(CSharpInteropModuleImpl).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("LocalVariablesOptimization", ["LocalVariablesOptimization"], typeof(LocalVariablesOptimizer), typeof(LocalVariablesOptimizer).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("NativeCilOptimization", ["NativeCilOptimization"], typeof(NativeCilOptimizerModule), typeof(NativeCilOptimizerModule).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("ArithmeticOptimization", ["ArithmeticOptimization"], typeof(ArithmeticOptimizerModule), typeof(ArithmeticOptimizerModule).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("EGraphOptimization", ["EGraphOptimization"], typeof(EGraphOptimizerModule), typeof(EGraphOptimizerModule).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("NativeTypesOptimization", ["NativeTypesOptimization"], typeof(NativeTypesOptimizerModule), typeof(NativeTypesOptimizerModule).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("BooleanOptimization", ["BooleanOptimization"], typeof(BooleanOptimizerModule), typeof(BooleanOptimizerModule).Assembly.GetName().Name))
            .RegisterOptimizer(DialectRuntimeOptimizerDescriptor.Create("ComparisonIntrinsicOptimization", ["ComparisonIntrinsicOptimization"], typeof(ComparisonIntrinsicOptimizerModule), typeof(ComparisonIntrinsicOptimizerModule).Assembly.GetName().Name))
            .RegisterBackend(DialectRuntimeBackendDescriptor.Create(WistDialectBackendIds.Cil, ["compiler", "cil"], typeof(WistCilBackendDeclaration), typeof(WistCilBackendDeclaration).Assembly.GetName().Name))
            .RegisterBackend(DialectRuntimeBackendDescriptor.Create(WistDialectBackendIds.Interpreter, ["interpreter"], typeof(WistInterpreterBackendDeclaration), typeof(WistInterpreterBackendDeclaration).Assembly.GetName().Name))
            .Build();
    }
}
