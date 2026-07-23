using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.Testing;

public sealed record BackendParityResult(
    BackendId FirstBackend,
    object? FirstValue,
    BackendId SecondBackend,
    object? SecondValue,
    bool AreEquivalent);

public sealed class LanguageContractException(string message) : Exception(message);

public static class LanguageContractSuite
{
    public static BackendParityResult Compare(
        LanguageRuntime runtime,
        string source,
        BackendId firstBackend,
        BackendId secondBackend,
        IReadOnlyDictionary<string, object?>? arguments = null,
        Func<object?, object?, bool>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        comparer ??= static (left, right) => string.Equals(left?.ToString(), right?.ToString(), StringComparison.Ordinal);
        var first = runtime.Run(new LanguageExecutionRequest(source, firstBackend, arguments));
        var second = runtime.Run(new LanguageExecutionRequest(source, secondBackend, arguments));
        return new BackendParityResult(firstBackend, first.Value, secondBackend, second.Value, comparer(first.Value, second.Value));
    }

    public static BackendParityResult RequireParity(
        LanguageRuntime runtime,
        string source,
        BackendId firstBackend,
        BackendId secondBackend,
        IReadOnlyDictionary<string, object?>? arguments = null,
        Func<object?, object?, bool>? comparer = null)
    {
        var result = Compare(runtime, source, firstBackend, secondBackend, arguments, comparer);
        if (!result.AreEquivalent)
            throw new LanguageContractException(
                $"Backend parity failed: '{result.FirstBackend.Value}' produced '{result.FirstValue}', " +
                $"'{result.SecondBackend.Value}' produced '{result.SecondValue}'.");
        return result;
    }
}
