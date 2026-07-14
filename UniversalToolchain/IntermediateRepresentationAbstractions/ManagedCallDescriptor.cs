using System.Reflection;

namespace IntermediateRepresentationAbstractions;

/// <summary>
///     Backend-neutral view of a managed call payload. Concrete language/runtime descriptors may implement this
///     contract without moving their public type into the IR assembly.
/// </summary>
public interface IManagedCallDescriptor
{
    MethodInfo Method { get; }

    ManagedCallReceiverKind ReceiverKind { get; }

    Type? ExecutionScopedProviderType { get; }
}

public enum ManagedCallReceiverKind
{
    Static,
    ExecutionScopedProvider
}
