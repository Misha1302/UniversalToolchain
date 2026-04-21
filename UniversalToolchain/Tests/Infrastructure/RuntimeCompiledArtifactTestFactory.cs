using System.Reflection.Emit;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Infrastructure;

internal static class RuntimeCompiledArtifactTestFactory
{
    public static ICompiledArtifact<DynamicMethod> CreateUnaryAddOneArtifact()
    {
        var dynamicMethod = new DynamicMethod("AddOne", typeof(int), [typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "x + 1",
            [new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    public static ICompiledArtifact<DynamicMethod> CreateBinaryArtifact()
    {
        var dynamicMethod = new DynamicMethod("ConcatDecimalDigits", typeof(int), [typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_S, 10);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "x * 10 + y",
            [
                new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "y", Type = typeof(int), Kind = ExternalBindingKind.Variable }
            ],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    public static ICompiledArtifact<DynamicMethod> CreateDeclaredOrderArtifact()
    {
        var dynamicMethod = new DynamicMethod("DeclaredBindingOrder", typeof(int), [typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_S, 10);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "left * 10 + right",
            [
                new ExternalBinding { Name = "left", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "right", Type = typeof(int), Kind = ExternalBindingKind.Variable }
            ],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    public static WistDialectExecutionHost CreateHost()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(
            """
            dialect RuntimeContracts
            use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Variables,Scopes,Labels,InternalPreprocessorLexemes,CSharpInterop
            backend compiler,interpreter
            """,
            "runtime-contracts-inline");

        if (!composition.IsSuccess)
            Thrower.InvalidOpEx(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
    }

    public static BasicCoreImpl<DynamicMethod> GetCompilerCore(WistDialectExecutionHost host) =>
        host.GetCore("compiler") as BasicCoreImpl<DynamicMethod>
        ?? Thrower.InvalidOpEx<BasicCoreImpl<DynamicMethod>>("Compiler core must be BasicCoreImpl<DynamicMethod>.");

    public static BasicCoreImpl<IAbstractIR> GetInterpreterCore(WistDialectExecutionHost host) =>
        host.GetCore("interpreter") as BasicCoreImpl<IAbstractIR>
        ?? Thrower.InvalidOpEx<BasicCoreImpl<IAbstractIR>>("Interpreter core must be BasicCoreImpl<IAbstractIR>.");

    private sealed class DynamicMethodExecutor : IExecutor<DynamicMethod>
    {
        public object? Execute(DynamicMethod compilation, IExecutionEnvironment environment)
        {
            var parameters = compilation.GetParameters();
            var args = new object?[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
                args[i] = environment.GetExternalValue(i);

            return compilation.Invoke(null, args);
        }
    }
}