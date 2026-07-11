using System.Reflection;
using NativeMathModule;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;

namespace UniversalToolchain.Dialects.Wist;

/// <summary>
/// Projects the exact closed Int32 NativeArithmetic methods emitted by Wist to canonical SSA callables.
/// </summary>
internal sealed class WistNativeArithmeticSsaProjection : ISsaManagedCallableProjection
{
    public bool TryProject(
        MethodInfo method,
        bool consumesInstanceReceiver,
        out CallableId callable)
    {
        callable = default;

        if (consumesInstanceReceiver ||
            method.DeclaringType != typeof(NativeArithmetic) ||
            method.ReturnType != typeof(int) ||
            !method.IsStatic)
        {
            return false;
        }

        var parameters = method.GetParameters();
        if (parameters.Length != 2 || parameters.Any(static parameter => parameter.ParameterType != typeof(int)))
            return false;

        callable = method.Name switch
        {
            nameof(NativeArithmetic.Add) => SsaPreviewCallables.AddInt32Unchecked,
            nameof(NativeArithmetic.Subtract) => SsaPreviewCallables.SubtractInt32Unchecked,
            nameof(NativeArithmetic.Multiply) => SsaPreviewCallables.MultiplyInt32Unchecked,
            _ => default
        };

        if (callable == default)
        {
            callable = default;
            return false;
        }

        return true;
    }
}
