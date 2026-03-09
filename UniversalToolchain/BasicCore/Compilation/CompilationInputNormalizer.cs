namespace BasicCore.Compilation;

public sealed class CompilationInputNormalizer
{
    public CompilationInput NormalizeRuntimeInput(string code, Dictionary<string, object>? parameters = null)
    {
        parameters ??= [];
        var bindings = new List<ExternalBinding>(parameters.Count);

        foreach (var pair in parameters)
        {
            bindings.Add(new ExternalBinding
            {
                Name = pair.Key,
                Type = pair.Value.GetType(),
                Value = pair.Value,
                Kind = ExternalBindingKind.Variable
            });
        }

        return new CompilationInput
        {
            SourceText = code,
            ExternalBindings = bindings,
            Options = new CompilationOptions()
        };
    }

    public CompilationInput NormalizeDeclaredInput(string code, OrderedDictionary<string, Type>? parameters = null)
    {
        parameters ??= [];
        var bindings = new List<ExternalBinding>(parameters.Count);

        foreach (var pair in parameters)
        {
            bindings.Add(new ExternalBinding
            {
                Name = pair.Key,
                Type = pair.Value,
                Kind = ExternalBindingKind.Variable
            });
        }

        return new CompilationInput
        {
            SourceText = code,
            ExternalBindings = bindings,
            Options = new CompilationOptions()
        };
    }
}