using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Core.Binding.Handlers;

internal sealed class DialectDirectiveHandlerContext
{
    private DialectDirectiveHandlerContext(string intrinsicContradictionCode, string optimizerContradictionCode)
    {
        IntrinsicContradictionCode = intrinsicContradictionCode;
        OptimizerContradictionCode = optimizerContradictionCode;
    }

    public string IntrinsicContradictionCode { get; }

    public string OptimizerContradictionCode { get; }

    public static DialectDirectiveHandlerContext FromInputKind(DialectBindingInputKind inputKind)
    {
        return inputKind switch
        {
            DialectBindingInputKind.Compiled => new DialectDirectiveHandlerContext("S103", "S104"),
            _ => new DialectDirectiveHandlerContext("S004", "S005")
        };
    }

    public static string FormatRuleName(string name, DialectBackendSelector target)
    {
        return target.IsAny ? name : $"{name}@{DialectBackendSelectorText.ToText(target.BackendId)}";
    }
}
