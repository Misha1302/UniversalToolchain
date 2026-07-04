using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Ssa.Core;

public sealed class SsaSemanticCallVerifier
{
    private readonly SemanticDescriptorSet _descriptors;

    public SsaSemanticCallVerifier(SemanticDescriptorSet descriptors)
    {
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
    }

    public IrVerificationResult Verify(
        SsaCall call,
        IReadOnlyDictionary<SsaValueId, SsaValue> visibleValues)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentNullException.ThrowIfNull(visibleValues);

        var diagnostics = new List<IrDiagnostic>();
        if (!_descriptors.TryGetCallable(call.Callee, out var descriptor))
        {
            diagnostics.Add(Diagnostic(
                "ssa.call.descriptor.missing",
                $"SSA call '{call.Id}' references unknown callable descriptor '{call.Callee}'."));
            return new IrVerificationResult(diagnostics);
        }

        VerifyArgumentCount(call, descriptor, diagnostics);
        VerifyResultCount(call, descriptor, diagnostics);
        VerifyArgumentTypes(call, descriptor, visibleValues, diagnostics);
        VerifyResultTypes(call, descriptor, diagnostics);
        VerifyAttributes(call, descriptor, diagnostics);

        return diagnostics.Count == 0 ? IrVerificationResult.Success : new IrVerificationResult(diagnostics);
    }

    private static void VerifyArgumentCount(
        SsaCall call,
        CallableDescriptor descriptor,
        List<IrDiagnostic> diagnostics)
    {
        if (call.Arguments.Count == descriptor.Signature.ParameterTypes.Count)
            return;

        diagnostics.Add(Diagnostic(
            "ssa.call.argument-count",
            $"SSA call '{call.Id}' to '{descriptor.Id}' expects {descriptor.Signature.ParameterTypes.Count} arguments but has {call.Arguments.Count}."));
    }

    private static void VerifyResultCount(
        SsaCall call,
        CallableDescriptor descriptor,
        List<IrDiagnostic> diagnostics)
    {
        if (call.Results.Count == descriptor.Signature.ResultTypes.Count)
            return;

        diagnostics.Add(Diagnostic(
            "ssa.call.result-count",
            $"SSA call '{call.Id}' to '{descriptor.Id}' expects {descriptor.Signature.ResultTypes.Count} results but has {call.Results.Count}."));
    }

    private static void VerifyArgumentTypes(
        SsaCall call,
        CallableDescriptor descriptor,
        IReadOnlyDictionary<SsaValueId, SsaValue> visibleValues,
        List<IrDiagnostic> diagnostics)
    {
        var count = Math.Min(call.Arguments.Count, descriptor.Signature.ParameterTypes.Count);
        for (var index = 0; index < count; index++)
        {
            if (!visibleValues.TryGetValue(call.Arguments[index], out var argument))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.call.argument.undefined",
                    $"SSA call '{call.Id}' uses undefined argument value '{call.Arguments[index]}'."));
                continue;
            }

            var expected = descriptor.Signature.ParameterTypes[index];
            if (!SameType(argument.Type, expected))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.call.argument-type",
                    $"SSA call '{call.Id}' argument {index} expects type '{expected}' but value '{argument.Id}' has type '{argument.Type}'."));
            }
        }
    }

    private static void VerifyResultTypes(
        SsaCall call,
        CallableDescriptor descriptor,
        List<IrDiagnostic> diagnostics)
    {
        var count = Math.Min(call.Results.Count, descriptor.Signature.ResultTypes.Count);
        for (var index = 0; index < count; index++)
        {
            var expected = descriptor.Signature.ResultTypes[index];
            if (!SameType(call.Results[index].Type, expected))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.call.result-type",
                    $"SSA call '{call.Id}' result {index} expects type '{expected}' but value '{call.Results[index].Id}' has type '{call.Results[index].Type}'."));
            }
        }
    }

    private static void VerifyAttributes(
        SsaCall call,
        CallableDescriptor descriptor,
        List<IrDiagnostic> diagnostics)
    {
        foreach (var required in descriptor.RequiredAttributes)
        {
            if (!ContainsAttribute(call, required))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.call.attribute.missing",
                    $"SSA call '{call.Id}' to '{descriptor.Id}' is missing required semantic attribute '{required}'."));
            }
        }

        foreach (var attribute in call.Attributes.Values)
        {
            if (!descriptor.AllowedAttributes.Any(allowed => SameAttribute(attribute.Key, allowed)))
            {
                diagnostics.Add(Diagnostic(
                    "ssa.call.attribute.unknown",
                    $"SSA call '{call.Id}' has attribute '{attribute.Key}' that is not allowed by callable descriptor '{descriptor.Id}'."));
            }
        }
    }

    private static bool SameType(SsaTypeId ssaType, SemanticTypeId semanticType) =>
        string.Equals(ssaType.Value, semanticType.Value, StringComparison.Ordinal);

    private static bool ContainsAttribute(SsaCall call, SemanticAttributeKey key) =>
        call.Attributes.Values.Any(attribute => SameAttribute(attribute.Key, key));

    private static bool SameAttribute(SsaAttributeKey ssaKey, SemanticAttributeKey semanticKey) =>
        string.Equals(ssaKey.Value, semanticKey.Value, StringComparison.Ordinal);

    private static IrDiagnostic Diagnostic(string code, string message) =>
        new(IrDiagnosticSeverity.Error, code, message);
}
