namespace UniversalToolchain.ModuleContracts;

public enum SideEffectPolicy
{
    Unknown,
    Pure,
    ReadsState,
    WritesState,
    ReadsAndWritesState,
    ControlFlow
}
