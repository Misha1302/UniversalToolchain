namespace UniversalToolchain.PlanFuzz;

public enum PlanFuzzProgramClass
{
    ValidDeterministic,
    ValidWithExpectedRuntimeFailure,
    InvalidSyntax,
    InvalidBinding,
    UnsupportedShape,
    PolicyRejected
}
