using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class DialectDirectiveHandlerContext
{
    private DialectDirectiveHandlerContext(
        string moduleConflictCode,
        string backendContradictionCode,
        string intrinsicContradictionCode,
        string optimizerContradictionCode)
    {
        ModuleConflictCode = moduleConflictCode;
        BackendContradictionCode = backendContradictionCode;
        IntrinsicContradictionCode = intrinsicContradictionCode;
        OptimizerContradictionCode = optimizerContradictionCode;
    }

    public string ModuleConflictCode { get; }

    public string BackendContradictionCode { get; }

    public string IntrinsicContradictionCode { get; }

    public string OptimizerContradictionCode { get; }

    public static DialectDirectiveHandlerContext FromInputKind(DialectBindingInputKind inputKind)
    {
        return inputKind switch
        {
            DialectBindingInputKind.Compiled => new DialectDirectiveHandlerContext("S101", "S102", "S103", "S104"),
            _ => new DialectDirectiveHandlerContext("S001", "S003", "S004", "S005")
        };
    }

    public static string FormatRuleName(string name, DialectBackendSelector target)
    {
        return target.IsAny ? name : $"{name}@{DialectBackendSelectorText.ToText(target.BackendId)}";
    }
}
