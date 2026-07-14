using System.Reflection;
using IntermediateRepresentationAbstractions;

namespace BasicCore.Core;

public sealed record CSharpCallDescriptor : IManagedCallDescriptor
{
    public CSharpCallDescriptor(MethodInfo method, CSharpCallReceiver receiver)
    {
        Method = method.ArgNotNull();
        Receiver = receiver.ArgNotNull();
    }

    public MethodInfo Method { get; }

    public CSharpCallReceiver Receiver { get; }

    public ManagedCallReceiverKind ReceiverKind => Receiver switch
    {
        CSharpCallReceiver.Static => ManagedCallReceiverKind.Static,
        CSharpCallReceiver.ExecutionScopedProvider => ManagedCallReceiverKind.ExecutionScopedProvider,
        _ => throw new InvalidOperationException($"Unknown managed-call receiver '{Receiver.GetType()}'.")
    };

    public Type? ExecutionScopedProviderType =>
        (Receiver as CSharpCallReceiver.ExecutionScopedProvider)?.ProviderType;
}

public abstract record CSharpCallReceiver
{
    public sealed record Static : CSharpCallReceiver;

    public sealed record ExecutionScopedProvider(Type ProviderType) : CSharpCallReceiver
    {
        public Type ProviderType { get; } = ProviderType.ArgNotNull();
    }
}
