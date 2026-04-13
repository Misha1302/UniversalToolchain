using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class CapabilityDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 0;

    public string Name => "Capability";

    public void Apply(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics)
    {
        if (source == null)
            Thrower.ArgumentNull(nameof(source));

        if (builder == null)
            Thrower.ArgumentNull(nameof(builder));

        if (diagnostics == null)
            Thrower.ArgumentNull(nameof(diagnostics));

        builder.SetCapabilityPolicy(new CapabilityPolicy(source.Capabilities.OrderBy(x => x.Key, StringComparer.Ordinal)));
    }
}
