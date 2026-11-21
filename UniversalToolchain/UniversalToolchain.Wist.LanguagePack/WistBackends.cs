using AbstractIrConverters;
using BasicCilCompiler.Contracts;
using BasicCilCompiler.Execution;
using BasicCore.Capabilities;
using BasicInterpreter.Contracts;
using BytecodeDynamicMethodsCompiler.Compilers;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.Runtime;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Wist.LanguagePack;

/// <summary>
/// The two production backends, described as data.
/// <para>
/// This file is the <b>only</b> place in the pack that names <c>"cil"</c> or <c>"interpreter"</c> to make a
/// behavioural decision. Everything else asks <see cref="WistBackendCatalog"/>. Adding a third backend
/// means adding one descriptor here, and the compiler then tells you what else is missing, instead of a
/// string switch throwing at runtime.
/// </para>
/// <para>
/// The <c>BackendId</c> literals below are identity, not branching: a backend has to be named somewhere.
/// The point of the gate is that the name is not compared to decide behaviour.
/// </para>
/// </summary>
internal static class WistBackends
{
    public static BackendId Cil { get; } = new("cil");

    public static BackendId Interpreter { get; } = new("interpreter");

    public static IReadOnlyList<WistBackendDescriptor> Descriptors { get; } =
    [
        new WistBackendDescriptor(
            Cil,
            new AbstractMethodsCompilerImpl().SupportedIntrinsics,
            CilBackendContractDescriptorProvider.Module.Value,
            static intrinsics => new CilBackendContractDescriptorProvider(intrinsics),
            static () => new CompilerIntrinsicCapabilitySetFactory().Create(new AbstractMethodsCompilerImpl()),
            static (runtime, result) => WistBuiltArtifactActivation.CreateCilProgram(runtime, result),
            static (runtime, result) => runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Cil).SsaReport),

        new WistBackendDescriptor(
            Interpreter,
            AbstractIrToAbstractIrStub.SupportedIntrinsicIds,
            InterpreterBackendContractDescriptorProvider.Module.Value,
            static intrinsics => new InterpreterBackendContractDescriptorProvider(intrinsics),
            static () => new CompilerIntrinsicCapabilitySetFactory().Create(new AbstractIrToAbstractIrStub()),
            static (runtime, result) => WistBuiltArtifactActivation.CreateInterpreterProgram(runtime, result),
            static (runtime, result) => runtime.GetBuiltArtifactValue(result, WistDirectBackendArtifactKinds.Interpreter).SsaReport)
    ];
}
