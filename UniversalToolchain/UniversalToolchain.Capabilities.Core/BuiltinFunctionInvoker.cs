using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Capabilities.Core;

public sealed class BuiltinFunctionInvoker
{
    public BuiltinFunctionInvocationResult Invoke(BuiltinFunctionResolution resolution, IReadOnlyList<object?> arguments)
    {
        resolution = resolution.ArgNotNull();
        arguments = arguments.ArgNotNull();

        if (!resolution.IsSuccess || resolution.RuntimeBinding == null)
            return new BuiltinFunctionInvocationResult(false, null, resolution.Diagnostics);

        var parameters = resolution.RuntimeBinding.Method.GetParameters();
        if (parameters.Length != arguments.Count)
        {
            return Failure(
                ToolchainDiagnosticCodes.WrongFunctionArgumentCount,
                $"Builtin function '{resolution.RuntimeBinding.Signature.Name}' expects {parameters.Length} runtime arguments, but received {arguments.Count}.");
        }

        var normalizedArguments = new object?[arguments.Count];
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var parameterType = parameters[i].ParameterType;
            if (argument == null)
            {
                if (parameterType.IsValueType)
                {
                    return Failure(
                        ToolchainDiagnosticCodes.WrongFunctionArgumentType,
                        $"Builtin function '{resolution.RuntimeBinding.Signature.Name}' argument {i} must not be null.");
                }

                normalizedArguments[i] = null;
                continue;
            }

            if (!parameterType.IsInstanceOfType(argument))
            {
                return Failure(
                    ToolchainDiagnosticCodes.WrongFunctionArgumentType,
                    $"Builtin function '{resolution.RuntimeBinding.Signature.Name}' argument {i} must have runtime type '{parameterType.FullName}'. Actual runtime type: '{argument.GetType().FullName}'.");
            }

            normalizedArguments[i] = argument;
        }

        return new BuiltinFunctionInvocationResult(
            true,
            resolution.RuntimeBinding.Method.Invoke(null, normalizedArguments),
            []);
    }

    private static BuiltinFunctionInvocationResult Failure(string code, string message)
    {
        return new BuiltinFunctionInvocationResult(
            false,
            null,
            [
                new ToolchainDiagnostic(
                    code,
                    ToolchainDiagnosticSeverity.Error,
                    message,
                    null,
                    [])
            ]);
    }
}
