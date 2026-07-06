using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class SecurityDirectiveHandler : IDialectDirectiveHandler
{
    public int Order => 40;

    public string Name => "Security";

    public void Apply(DialectDirectiveBindingContext context)
    {
        context.SetSecurityPolicy(context.SecurityProfile.HasValue ? new SecurityPolicy(context.SecurityProfile.Value) : null);
    }
}
