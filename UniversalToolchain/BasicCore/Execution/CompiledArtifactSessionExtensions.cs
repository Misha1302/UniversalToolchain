namespace BasicCore.Execution;

/// <summary>
///     Convenience execution helpers for <see cref="ICompiledArtifactSession" />.
/// </summary>
public static class CompiledArtifactSessionExtensions
{
    /// <summary>
    ///     Executes the session and casts the result to <typeparamref name="T" />.
    /// </summary>
    public static T Run<T>(this ICompiledArtifactSession session)
    {
        session = session.ArgNotNull();

        var result = session.Run();
        if (result is T typed)
            return typed;

        if (result == null && default(T) == null)
            return default!;

        return Thrower.InvalidCast<T>($"Execution result of type '{result?.GetType()}' cannot be cast to '{typeof(T)}'.");
    }

    /// <summary>
    ///     Assigns positional arguments and executes the session.
    /// </summary>
    public static T Invoke<T>(
        this ICompiledArtifactSession session,
        params object?[] arguments)
    {
        session = session.ArgNotNull();

        arguments = arguments.ArgNotNull();

        if (arguments.Length != session.ArgumentCount)
            Thrower.Argument(
                nameof(arguments),
                $"Expected {session.ArgumentCount} arguments, but got {arguments.Length}.");

        for (var i = 0; i < arguments.Length; i++)
            session.SetArgument(i, arguments[i]);

        return session.Run<T>();
    }

    /// <summary>
    ///     Assigns named arguments and executes the session.
    /// </summary>
    public static T InvokeNamed<T>(
        this ICompiledArtifactSession session,
        IReadOnlyDictionary<string, object?> arguments)
    {
        session = session.ArgNotNull();

        arguments = arguments.ArgNotNull();

        if (arguments.Count != session.ArgumentCount)
            Thrower.Argument(
                nameof(arguments),
                $"Expected {session.ArgumentCount} named arguments, but got {arguments.Count}.");

        foreach (var argument in arguments)
            session.SetArgument(argument.Key, argument.Value);

        return session.Run<T>();
    }

    /// <summary>
    ///     Assigns positional arguments and executes the session.
    /// </summary>
    public static T Invoke<TCompilationOutput, T>(
        this CompiledArtifactSession<TCompilationOutput> session,
        params object?[] arguments) =>
        Invoke<T>(session, arguments);

    /// <summary>
    ///     Assigns named arguments and executes the session.
    /// </summary>
    public static T InvokeNamed<TCompilationOutput, T>(
        this CompiledArtifactSession<TCompilationOutput> session,
        IReadOnlyDictionary<string, object?> arguments) =>
        InvokeNamed<T>(session, arguments);
}