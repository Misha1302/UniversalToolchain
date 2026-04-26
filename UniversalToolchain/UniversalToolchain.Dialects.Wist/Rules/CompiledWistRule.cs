using BasicCore.Compilation;
using ExceptionsManager;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public sealed class CompiledWistRule : ICompiledRule
{
    private readonly ICompiledArtifact _artifact;
    private readonly IWistRuleArgumentBinder _argumentBinder;

    public CompiledWistRule(CompiledRuleDescriptor descriptor, ICompiledArtifact artifact, IWistRuleArgumentBinder argumentBinder)
    {
        Descriptor = descriptor.ArgNotNull();
        _artifact = artifact.ArgNotNull();
        _argumentBinder = argumentBinder.ArgNotNull();
    }

    public CompiledRuleDescriptor Descriptor { get; }

    public object? Run(IReadOnlyDictionary<string, object?> arguments)
    {
        var result = TryRun(arguments);
        if (!result.IsSuccess)
        {
            var message = string.Join(Environment.NewLine, result.Diagnostics.Select(static x => x.Message));
            Thrower.Argument(nameof(arguments), message);
        }

        return result.Value;
    }

    public RuleExecutionResult TryRun(IReadOnlyDictionary<string, object?> arguments)
    {
        arguments = arguments.ArgNotNull();

        var binding = _argumentBinder.Bind(Descriptor, arguments);
        if (!binding.IsSuccess)
            return RuleExecutionResult.Failure(binding.Diagnostics);

        var session = _artifact.CreateSession();
        foreach (var parameter in Descriptor.Parameters)
            session.SetArgument(parameter.Name, binding.RuntimeArguments[parameter.Name]);

        return RuleExecutionResult.Success(session.Run());
    }
}
