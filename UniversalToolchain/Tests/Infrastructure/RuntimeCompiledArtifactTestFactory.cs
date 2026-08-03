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

    public static ICompiledArtifact<DynamicMethod> CreateEnvironmentAndTwoArgumentsArtifact()
    {
        var dynamicMethod = new DynamicMethod("EnvironmentAndTwoArguments", typeof(int), [typeof(IExecutionEnvironment), typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4_S, 10);
        il.Emit(OpCodes.Mul);
        il.Emit(OpCodes.Ldarg_2);
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

    public static ICompiledArtifact<DynamicMethod> CreateExternalRuntimeLoadThroughProviderArtifact()
    {
        var dynamicMethod = new DynamicMethod("ExternalRuntimeLoadThroughProvider", typeof(int), [typeof(IExecutionEnvironment), typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        var helper = typeof(RuntimeCompiledArtifactTestFactory)
            .GetMethod(nameof(LoadExternalSlotsThroughProvider), BindingFlags.NonPublic | BindingFlags.Static)
            .NotNull();

        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Call, helper);
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

    public static ICompiledArtifact<DynamicMethod> CreateEnvironmentOnlyArtifact()
    {
        var dynamicMethod = new DynamicMethod("EnvironmentOnly", typeof(int), [typeof(IExecutionEnvironment)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, 42);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "42",
            [],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    public static ICompiledArtifact<DynamicMethod> CreateEnvironmentAndOneArgumentArtifact()
    {
        var dynamicMethod = new DynamicMethod("EnvironmentAndOneArgument", typeof(int), [typeof(IExecutionEnvironment), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "x + 1",
            [new ExternalBinding { Name = "x", Type = typeof(int), Kind = ExternalBindingKind.Variable }],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    public static ICompiledArtifact<DynamicMethod> CreateEnvironmentAndSevenArgumentsArtifact()
    {
        var dynamicMethod = new DynamicMethod("EnvironmentAndSevenArguments", typeof(int), [typeof(IExecutionEnvironment), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)]);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_S, (byte)4);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_S, (byte)5);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_S, (byte)6);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_S, (byte)7);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "a + b + c + d + e + f + g",
            [
                new ExternalBinding { Name = "a", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "b", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "c", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "d", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "e", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "f", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "g", Type = typeof(int), Kind = ExternalBindingKind.Variable }
            ],
            dynamicMethod,
            new DynamicMethodExecutor());
    }

    public static ICompiledArtifact<DynamicMethod> CreateEnvironmentAndTenArgumentsArtifact()
    {
        var parameterTypes = new[]
        {
            typeof(IExecutionEnvironment), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int)
        };

        var dynamicMethod = new DynamicMethod("EnvironmentAndTenArguments", typeof(int), parameterTypes);
        var il = dynamicMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_1);
        for (short i = 2; i <= 10; i++)
        {
            il.Emit(OpCodes.Ldarg, i);
            il.Emit(OpCodes.Add);
        }

        il.Emit(OpCodes.Ret);

        return new CompiledArtifact<DynamicMethod>(
            "a + b + c + d + e + f + g + h + i + j",
            [
                new ExternalBinding { Name = "a", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "b", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "c", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "d", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "e", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "f", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "g", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "h", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "i", Type = typeof(int), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "j", Type = typeof(int), Kind = ExternalBindingKind.Variable }
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

        ServiceProvider? provider = services.BuildServiceProvider();
        try
        {
            var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
            var composition = workflow.ComposeText(
                """
                dialect RuntimeContracts
                use Whitespaces,SemicolonAsNewLine,Comments,Numbers,Identifier,Arithmetic,Equality,Conditions,Loops,Scopes,Variables,Labels,InternalPreprocessorLexemes,CSharpInterop
                backend cil,interpreter
                """,
                "runtime-contracts-inline");

            if (!composition.IsSuccess)
                Thrower.InvalidOpEx(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

            var owner = provider;
            provider = null;
            return workflow.CreateHost(composition, new WistRuntimeServiceOptions(), owner);
        }
        finally
        {
            provider?.Dispose();
        }
    }

    private static int LoadExternalSlotsThroughProvider(IExecutionEnvironment environment, int unusedFirstArgument, int unusedSecondArgument)
    {
        _ = unusedFirstArgument;
        _ = unusedSecondArgument;
        environment = environment.ArgNotNull();

        var provider = (ExternalRuntimeCallProvider)environment.GetRequiredProvider(typeof(ExternalRuntimeCallProvider));
        var loadedEnvironment = provider.LoadEnvironment();

        var first = ExternalRuntimeCalls.LoadExternal<int>(loadedEnvironment, 0);
        var second = ExternalRuntimeCalls.LoadExternal<int>(loadedEnvironment, 1);

        return first * 10 + second;
    }

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
