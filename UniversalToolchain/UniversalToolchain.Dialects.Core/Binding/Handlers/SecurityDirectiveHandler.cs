using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class SecurityDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 40;

    public string Name => "Security";

    public void Apply(DialectBindingExecutionContext context)
    {
        context.Builder.SetSecurityPolicy(context.Source.SecurityProfile.HasValue ? new SecurityPolicy(context.Source.SecurityProfile.Value) : null);
    }
}
