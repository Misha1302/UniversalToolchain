namespace UniversalToolchain.ModuleContracts;

public interface IBytecodeObservedEmissionReader
{
    IReadOnlyList<ObservedBytecodeEmission> Read(Bytecode bytecode);

    BytecodeObservedEmissionReadResult ReadWithDiagnostics(Bytecode bytecode);
}
