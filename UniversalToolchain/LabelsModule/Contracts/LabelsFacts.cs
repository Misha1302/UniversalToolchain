using UniversalToolchain.ModuleContracts;

namespace LabelsModule.Contracts;

public static class LabelsFacts
{
    public static CompilerFactId LabelsDeclared { get; } = new("wist.control-flow.labels-declared");

    public static CompilerFactId GotosResolved { get; } = new("wist.control-flow.gotos-resolved");
}
