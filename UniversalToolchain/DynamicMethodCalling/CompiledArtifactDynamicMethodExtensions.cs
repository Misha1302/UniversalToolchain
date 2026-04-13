namespace DynamicMethodCalling;

/// <summary>
///     Provides typed delegate access helpers for compiled <see cref="DynamicMethod" /> artifacts.
/// </summary>
public static class CompiledArtifactDynamicMethodExtensions
{
    private static readonly ConditionalWeakTable<ICompiledArtifact<DynamicMethod>, NativeDelegateInvoker> _invokers = new();

    public static INativeDelegateInvoker GetNativeDelegateInvoker(this ICompiledArtifact<DynamicMethod> artifact)
    {
        artifact = artifact.ArgNotNull();

        return _invokers.GetValue(artifact, CreateInvoker);
    }

    public static Func<TResult> AsFunc<TResult>(this ICompiledArtifact<DynamicMethod> artifact)
    {
        artifact = artifact.ArgNotNull();

        return artifact.GetNativeDelegateInvoker().AsFunc<TResult>();
    }

    public static Func<T1, TResult> AsFunc<T1, TResult>(this ICompiledArtifact<DynamicMethod> artifact)
    {
        artifact = artifact.ArgNotNull();

        return artifact.GetNativeDelegateInvoker().AsFunc<T1, TResult>();
    }

    public static Func<T1, T2, TResult> AsFunc<T1, T2, TResult>(this ICompiledArtifact<DynamicMethod> artifact)
    {
        artifact = artifact.ArgNotNull();

        return artifact.GetNativeDelegateInvoker().AsFunc<T1, T2, TResult>();
    }

    public static Func<T1, T2, T3, TResult> AsFunc<T1, T2, T3, TResult>(this ICompiledArtifact<DynamicMethod> artifact)
    {
        artifact = artifact.ArgNotNull();

        return artifact.GetNativeDelegateInvoker().AsFunc<T1, T2, T3, TResult>();
    }

    public static Func<T1, T2, T3, T4, TResult> AsFunc<T1, T2, T3, T4, TResult>(this ICompiledArtifact<DynamicMethod> artifact)
    {
        artifact = artifact.ArgNotNull();

        return artifact.GetNativeDelegateInvoker().AsFunc<T1, T2, T3, T4, TResult>();
    }

    private static NativeDelegateInvoker CreateInvoker(ICompiledArtifact<DynamicMethod> artifact) => new(artifact.CompilationOutput);
}