namespace BytecodeDynamicMethodsCompiler.Core;

internal sealed class CompilationContext(GroboIL il, Dictionary<string, int> externalSlots)
{
    public Dictionary<string, int> ExternalSlots { get; } = externalSlots;
    public Dictionary<string, GroboIL.Local> LocalVariables { get; } = new();
    public Dictionary<Guid, GroboIL.Label> InstructionLabels { get; } = new();
    public GroboIL Il { get; } = il;

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