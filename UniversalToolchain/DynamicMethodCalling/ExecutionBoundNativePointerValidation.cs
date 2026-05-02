namespace DynamicMethodCalling;

internal static class ExecutionBoundNativePointerValidation
{
    public static void ValidateDeclaredBindings(ICompiledArtifact<DynamicMethod> artifact, IReadOnlyList<Type> expectedTypes)
    {
        artifact = artifact.ArgNotNull();
        expectedTypes = expectedTypes.ArgNotNull();

        Thrower.AssertAlways(
            artifact.DeclaredBindings.Count == expectedTypes.Count,
            $"Execution-bound native pointer requires exactly {expectedTypes.Count} declared bindings.");

        for (var i = 0; i < expectedTypes.Count; i++)
        {
            var binding = artifact.DeclaredBindings[i];
            var expectedType = expectedTypes[i];

            Thrower.AssertAlways(
                binding.Type == expectedType,
                $"Declared binding '{binding.Name}' must have type {expectedType} but it has {binding.Type}.");

            if (!artifact.SlotsByName.TryGetValue(binding.Name, out var slot))
                Thrower.InvalidOpEx($"Declared binding '{binding.Name}' has no slot.");

            Thrower.AssertAlways(
                slot == i,
                $"Declared binding '{binding.Name}' must use slot {i} but it uses slot {slot}.");
        }
    }
}