namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal interface IDialectDirectiveHandler
{
    int Order { get; }

    string Name { get; }

    void Apply(DialectBindingExecutionContext context);
}