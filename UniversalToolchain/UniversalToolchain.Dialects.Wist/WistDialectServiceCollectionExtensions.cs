using AssemblyFinder;
using BasicCore.Contracts;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using CommonExceptions;
using DynamicMethodWrapper;
using ExceptionsManager;
using GenericMath;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.ExpressionTyping.Abstractions;
using UniversalToolchain.Functions.Abstractions;
using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Optimization;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDialectServiceCollectionExtensions
{
    /// <summary>
    /// Adds canonical Wist dialect composition services with manifest-driven runtime catalog and exact activation.
    /// </summary>
    internal static IServiceCollection AddWistDialectServices(this IServiceCollection services)
    {
        services = services.ArgNotNull();

        return services
            .AddWistRuntimeSharedContractAssemblies()
            .AddWistDialectCoreServices()
            .AddFileSystemRuntimeCatalogServices()
            .AddReflectionRuntimeResolutionServices();
    }

    /// <summary>
    /// Declares the host-owned CLR contracts that actually cross the Wist host/runtime ALC boundary.
    /// Registration preserves identity only; it does not select or activate any runtime feature.
    /// </summary>
    internal static IServiceCollection AddWistRuntimeSharedContractAssemblies(this IServiceCollection services) =>
        services.AddRuntimeSharedAssemblies(
        [
            // Frontend/module and bytecode contracts.
            typeof(IFrontendCoreModule).Assembly,
            typeof(BytecodeInstruction).Assembly,
            typeof(ExtensibleEnum<>).Assembly,
            typeof(IAbstractMethodConvertable).Assembly,
            typeof(ITypeCatalog).Assembly,
            typeof(ICustomNumber<,>).Assembly,

            // Runtime export, backend registrar, and generic runtime-resolution contracts.
            typeof(DialectRuntimeExportAttribute).Assembly,
            typeof(IDialectBackendRuntimeRegistrar).Assembly,
            typeof(IServiceCollection).Assembly,

            // Capability/provider contracts and the host-owned selected capability catalog.
            typeof(DialectCapabilityProviderAttribute).Assembly,
            typeof(ILanguageFeatureDescriptorProvider).Assembly,
            typeof(CapabilityCatalog).Assembly,
            typeof(IBuiltinFunctionDescriptorProvider).Assembly,
            typeof(IBuiltinFunctionRuntimeBindingProvider).Assembly,
            typeof(IExpressionTypeRuleProvider).Assembly,

            // Module verification, diagnostics, semantic, AIR, and SSA contracts exchanged across contexts.
            typeof(IModuleContractDescriptorProvider).Assembly,
            typeof(ImportException).Assembly,
            typeof(ToolchainDiagnostic).Assembly,
            typeof(SemanticDescriptorSet).Assembly,
            typeof(IIrOptimizationPass).Assembly,
            typeof(IAbstractIR).Assembly,
            typeof(SsaValue).Assembly,
            typeof(ISsaRouteReportSink).Assembly
        ]);
}
