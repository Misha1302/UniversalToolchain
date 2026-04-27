namespace UniversalToolchain.Functions.Abstractions;

public enum FunctionPurity
{
    Pure,
    ReadsHostState,
    HasSideEffects
}
