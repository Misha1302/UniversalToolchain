namespace BasicCilCompiler.Execution;

public sealed class CilCompilationOutput
{
    public CilCompilationOutput(DynamicMethod method, ArtifactConstantPool? constantPool = null)
    {
        ArgumentNullException.ThrowIfNull(method);

        ConstantPool = constantPool;
        Method = method;
    }

    public DynamicMethod Method { get; }

    public ArtifactConstantPool? ConstantPool { get; }

    public bool HasConstantPool => ConstantPool is not null;
}
