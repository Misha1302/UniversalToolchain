using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class CapabilityDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 50;

    public string Name => "Capability";

    public void Apply(DialectBindingExecutionContext context)
    {
        context.Builder.SetCapabilityPolicy(new CapabilityPolicy(context.Source.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal)));
    }
}
