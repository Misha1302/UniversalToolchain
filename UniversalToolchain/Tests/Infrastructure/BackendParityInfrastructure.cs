using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Infrastructure;

public sealed record BackendExecutionResult(bool IsSuccess, object? Value, Exception? Exception)
{
    public static BackendExecutionResult Success(object? value) => new(true, value, null);

    public static BackendExecutionResult Failure(Exception exception) => new(false, null, exception);
}

public static class BackendParityInfrastructure
{
    public static (BackendExecutionResult CompilerResult, BackendExecutionResult InterpreterResult) RunBoth(string dialectText, string code)
    {
        using var compilerHost = CreateHost(dialectText, "compiler");
        using var interpreterHost = CreateHost(dialectText, "interpreter");

        return (
            ExecuteSingle(() => compilerHost.Run(code, "compiler")),
            ExecuteSingle(() => interpreterHost.Run(code, "interpreter"))
        );
    }

    public static void AssertSemanticParity(BackendExecutionResult compilerResult, BackendExecutionResult interpreterResult)
    {
        Assert.That(compilerResult.IsSuccess, Is.EqualTo(interpreterResult.IsSuccess), "Compiler/interpreter success mismatch.");

        if (compilerResult.IsSuccess)
        {
            AssertSemanticEqual(compilerResult.Value, interpreterResult.Value);
            return;
        }

        var compilerException = GetComparableException(compilerResult.Exception);
        var interpreterException = GetComparableException(interpreterResult.Exception);

        Assert.That(compilerException, Is.Not.Null);
        Assert.That(interpreterException, Is.Not.Null);
        Assert.That(compilerException!.GetType(), Is.EqualTo(interpreterException!.GetType()));
        Assert.That(
            GetInvariantMessageFragment(compilerException.Message),
            Is.EqualTo(GetInvariantMessageFragment(interpreterException.Message)));
    }

    public static double AsNumber(object? value)
    {
        if (value is BackendExecutionResult result)
            return AsNumber(result.Value);

        return value switch
        {
            RealNumberImpl n => n.GetValue(),
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            decimal m => (double)m,
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to number.")
        };
    }

    public static bool AsBool(object? value)
    {
        if (value is BackendExecutionResult result)
            return AsBool(result.Value);

        return value switch
        {
            bool b => b,
            int i => i != 0,
            RealNumberImpl n => Math.Abs(n.GetValue()) > double.Epsilon,
            _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to bool.")
        };
    }

    private static WistDialectExecutionHost CreateHost(string dialectText, string backend)
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();

        using var provider = services.BuildServiceProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(EnsureSingleBackend(dialectText, backend), "tests-inline");
        if (!composition.IsSuccess)
            throw new InvalidOperationException(composition.ToDeterministicText());

        return workflow.CreateHost(composition);
    }

    private static string EnsureSingleBackend(string dialectText, string backend)
    {
        var lines = dialectText
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static line => !line.StartsWith("backend ", StringComparison.Ordinal))
            .ToList();

        lines.Add($"backend {backend}");
        return string.Join(Environment.NewLine, lines);
    }

    private static BackendExecutionResult ExecuteSingle(Func<object?> run)
    {
        try
        {
            return BackendExecutionResult.Success(run());
        }
        catch (Exception ex)
        {
            return BackendExecutionResult.Failure(ex);
        }
    }

    private static void AssertSemanticEqual(object? left, object? right)
    {
        if (left is null || right is null)
        {
            Assert.That(left, Is.EqualTo(right));
            return;
        }

        if (left is bool || right is bool)
        {
            Assert.That(AsBool(left), Is.EqualTo(AsBool(right)));
            return;
        }

        Assert.That(AsNumber(left), Is.EqualTo(AsNumber(right)).Within(1e-9));
    }

    private static Exception? GetComparableException(Exception? exception)
    {
        if (exception == null)
            return null;

        var current = exception;
        while (true)
        {
            if (current is AggregateException aggregateException && aggregateException.InnerExceptions.Count == 1)
            {
                current = aggregateException.InnerExceptions[0];
                continue;
            }

            if (current.InnerException == null)
                return current;

            current = current.InnerException;
        }
    }

    private static string GetInvariantMessageFragment(string message)
    {
        const int maxLength = 64;
        var builder = new StringBuilder(message.Length);
        foreach (var c in message)
        {
            if (char.IsLetter(c))
            {
                builder.Append(char.ToLowerInvariant(c));
                continue;
            }

            if (!char.IsDigit(c))
                continue;

            if (builder.Length == 0 || builder[^1] != '#')
                builder.Append('#');
        }

        var normalized = builder.ToString().Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
