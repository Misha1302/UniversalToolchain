namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

public interface IDialectDirectiveHandler
{
    int Order { get; }

    string Name { get; }

    void Apply(DialectDirectiveBindingContext context);
}
