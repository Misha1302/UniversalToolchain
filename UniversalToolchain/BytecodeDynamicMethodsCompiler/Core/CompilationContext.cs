namespace BytecodeDynamicMethodsCompiler.Core;

internal sealed class CompilationContext(
    GroboIL il,
    Dictionary<string, int> externalSlots,
    List<object> constantPoolValues,
    int externalArgumentOffset = 0,
    int? constantPoolArgumentIndex = null,
    int? executionEnvironmentArgumentIndex = null)
{
    public Dictionary<string, int> ExternalSlots { get; } = externalSlots;
    public int ExternalArgumentOffset { get; } = externalArgumentOffset;
    public int? ConstantPoolArgumentIndex { get; } = constantPoolArgumentIndex;
    public int? ExecutionEnvironmentArgumentIndex { get; } = executionEnvironmentArgumentIndex;
    public Dictionary<string, GroboIL.Local> LocalVariables { get; } = new();
    public Dictionary<Guid, GroboIL.Label> InstructionLabels { get; } = new();
    public GroboIL Il { get; } = il;

    public int AddConstant(object value)
    {
        constantPoolValues.Add(value);
        return constantPoolValues.Count - 1;
    }

    public GroboIL.Local GetOrCreateLocal(string varName, Type varType, bool initializeWithDefault = false)
    {
        if (!LocalVariables.TryGetValue(varName, out var local))
        {
            local = Il.DeclareLocal(varType);
            LocalVariables[varName] = local;

            if (initializeWithDefault)
                InitializeLocal(local, varType);
        }

        return local;
    }

    private void InitializeLocal(GroboIL.Local local, Type varType)
    {
        if (varType.IsValueType)
        {
            Il.Ldloca(local);
            Il.Initobj(varType);
            return;
        }

        Il.Ldnull();
        Il.Stloc(local);
    }
}
