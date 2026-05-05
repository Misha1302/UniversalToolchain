using ObjectExtensions;

namespace BasicCore.Compilation;

internal static class ExternalBindingsFactory
{
    public static IReadOnlyList<ExternalBinding> FromDeclaredTypes(OrderedDictionary<string, Type>? parameters)
    {
        parameters ??= [];
        return parameters.Select(x => new ExternalBinding
        {
            Name = x.Key,
            Type = x.Value,
            Kind = ExternalBindingKind.Variable
        }).ToList();
    }

    public static IReadOnlyList<ExternalBinding> FromRuntimeValues(Dictionary<string, object>? parameters)
    {
        parameters ??= [];
        return parameters.Select(x => new ExternalBinding
        {
            Name = x.Key,
            Type = x.Value.MakeNullable()?.GetType() ?? typeof(object),
            Value = x.Value,
            Kind = ExternalBindingKind.Variable
        }).ToList();
    }
}