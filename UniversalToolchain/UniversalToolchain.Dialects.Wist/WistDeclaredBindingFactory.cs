using System.Collections.Specialized;
using ExceptionsManager;

namespace UniversalToolchain.Dialects.Wist;

internal static class WistDeclaredBindingFactory
{
    public static OrderedDictionary<string, Type> FromRuntimeArguments(
        IReadOnlyDictionary<string, object?> arguments)
    {
        arguments = arguments.ArgNotNull();

        var declaredBindings = new OrderedDictionary<string, Type>();
        foreach (var argument in arguments)
        {
            if (string.IsNullOrWhiteSpace(argument.Key))
                Thrower.Argument(nameof(arguments), "Argument names must not be empty.");

            declaredBindings[argument.Key] = argument.Value?.GetType() ?? typeof(object);
        }

        return declaredBindings;
    }

    public static OrderedDictionary<string, Type> FromDeclaredTypes(
        IReadOnlyDictionary<string, Type> bindings)
    {
        bindings = bindings.ArgNotNull();

        var declaredBindings = new OrderedDictionary<string, Type>();
        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Key))
                Thrower.Argument(nameof(bindings), "Binding names must not be empty.");

            declaredBindings[binding.Key] = binding.Value.ArgNotNull();
        }

        return declaredBindings;
    }
}
