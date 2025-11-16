// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

namespace TypeInference;

public class TypeInferenceContext(TypeInferenceContext? parent = null)
{
    private readonly Dictionary<string, Type> _variableTypes = new();

    public bool TryGetVariableType(string variableName, out Type? type)
    {
        type = null;
        if (_variableTypes.TryGetValue(variableName, out type))
            return true;

        return parent?.TryGetVariableType(variableName, out type) ?? false;
    }

    public void DeclareVariable(string variableName, Type type)
    {
        if (!_variableTypes.TryAdd(variableName, type))
            throw new TypeInferenceException($"Variable '{variableName}' already declared in current scope");
    }

    public TypeInferenceContext CreateChildContext()
    {
        return new TypeInferenceContext(this);
    }

    public IEnumerable<KeyValuePair<string, Type>> GetAllVariables()
    {
        var parentVars = parent?.GetAllVariables() ?? [];
        return parentVars.Concat(_variableTypes);
    }
}