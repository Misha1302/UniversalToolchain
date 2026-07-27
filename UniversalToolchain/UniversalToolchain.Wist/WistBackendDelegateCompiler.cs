using System.Linq.Expressions;
using System.Reflection;
using BasicCilCompiler.Execution;
using BasicCore.Compilation;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Wist;

/// <summary>
/// Creates a typed delegate over the backend selected by the facade.
/// The CIL backend uses its native DynamicMethod when the emitted signature is
/// directly compatible with the requested delegate; all other cases use a
/// typed wrapper over the immutable compiled artifact.
/// </summary>
internal sealed class WistBackendDelegateCompiler : IWistDelegateCompiler
{
    private static readonly MethodInfo InvokeArtifactMethod = typeof(WistBackendDelegateCompiler)
        .GetMethod(nameof(InvokeArtifact), BindingFlags.NonPublic | BindingFlags.Static)!;

    public TDelegate CompileDelegate<TDelegate>(
        WistDialectExecutionHost host,
        string formula,
        OrderedDictionary<string, Type> declaredBindings,
        string backend,
        WistRuntimeBoundary runtimeBoundary)
        where TDelegate : Delegate
    {
        var artifact = host.Compile(formula, declaredBindings, backend);

        if (string.Equals(backend, "cil", StringComparison.Ordinal) &&
            artifact is ICompiledArtifact<CilCompilationOutput> cilArtifact &&
            TryCreateNativeDelegate(cilArtifact.CompilationOutput, out TDelegate? nativeDelegate))
        {
            return nativeDelegate!;
        }

        return CreateArtifactDelegate<TDelegate>(artifact, runtimeBoundary);
    }

    private static bool TryCreateNativeDelegate<TDelegate>(
        CilCompilationOutput output,
        out TDelegate? compiledDelegate)
        where TDelegate : Delegate
    {
        try
        {
            compiledDelegate = output.HasConstantPool
                ? (TDelegate)output.Method.CreateDelegate(typeof(TDelegate), output.ConstantPool)
                : (TDelegate)output.Method.CreateDelegate(typeof(TDelegate));
            return true;
        }
        catch (ArgumentException)
        {
            compiledDelegate = null;
            return false;
        }
    }

    private static TDelegate CreateArtifactDelegate<TDelegate>(
        ICompiledArtifact artifact,
        WistRuntimeBoundary runtimeBoundary)
        where TDelegate : Delegate
    {
        var delegateType = typeof(TDelegate);
        var invoke = delegateType.GetMethod("Invoke")
                     ?? throw new ArgumentException($"Type '{delegateType.FullName}' is not a delegate type.", nameof(TDelegate));
        var parameters = invoke.GetParameters()
            .Select(static parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
            .ToArray();
        var boxedArguments = Expression.NewArrayInit(
            typeof(object),
            parameters.Select(static parameter => Expression.Convert(parameter, typeof(object))));
        var body = Expression.Call(
            InvokeArtifactMethod.MakeGenericMethod(invoke.ReturnType),
            Expression.Constant(artifact, typeof(ICompiledArtifact)),
            Expression.Constant(runtimeBoundary, typeof(WistRuntimeBoundary)),
            boxedArguments);

        return Expression.Lambda<TDelegate>(body, parameters).Compile();
    }

    private static TResult InvokeArtifact<TResult>(
        ICompiledArtifact artifact,
        WistRuntimeBoundary runtimeBoundary,
        object?[] arguments)
    {
        var session = artifact.CreateSession();
        if (arguments.Length != session.ArgumentCount)
        {
            throw new ArgumentException(
                $"Expected {session.ArgumentCount} arguments, but got {arguments.Length}.",
                nameof(arguments));
        }

        for (var index = 0; index < arguments.Length; index++)
            session.SetArgument(index, runtimeBoundary.NormalizeArgument(arguments[index]));

        return WistResultConverter.ConvertTo<TResult>(session.Run());
    }
}
