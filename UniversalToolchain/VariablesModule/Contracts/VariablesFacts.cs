using UniversalToolchain.ModuleContracts;

namespace VariablesModule.Contracts;

public static class VariablesFacts
{
    public static CompilerFactId LocalsDeclared { get; } = new("wist.variables.locals-declared");

    public static CompilerFactId ExternalBindingsReferenced { get; } = new("wist.variables.external-bindings-referenced");
}
